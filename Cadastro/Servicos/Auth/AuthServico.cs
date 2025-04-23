using Cadastro.Data;
using Cadastro.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Cadastro.Servicos.Auth
{
    public class AuthServico : IAuthServico
    {

        private readonly IConfiguration _configuration;
        private readonly CadastroContexto _contexto;
        private readonly ILogger<AuthServico> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDistributedCache _cache;

        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 15;

        public AuthServico(
            IConfiguration configuration,
            CadastroContexto contexto,
            ILogger<AuthServico> logger,
            IHttpClientFactory httpClientFactory,
            IDistributedCache cache)
        {
            _configuration = configuration;
            _contexto = contexto;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        public string GegarJwtToken(Usuario usuario)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var expiracao = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes");
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(expiracao),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<AuthResult> AutenticaAsync(string email, string senha)
        {
            try
            {
                var usuario = await _contexto.Usuarios
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

                if (usuario == null)
                {
                    _logger.LogWarning("Usuário não encontrado");
                    await LogFailedAttempt(email);
                    await _contexto.LogsErro.AddAsync(new LogErro
                    {
                        MensagemErro = "Usuário não encontrado: " + email,
                        DataErro = DateTime.Now,
                        StackTrace = "Usuário não encontrado",
                        Status = "Erro"
                    });
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Email ou senha inválidos"
                    };
                }

                bool isLockedOut = await VerificarLockedAsync(email, usuario);
                if (isLockedOut)
                {
                    _logger.LogWarning("Conta bloqueada para o usuário: {Email}", email);
                    return new AuthResult
                    {
                        Success = false,
                        Message = $"Conta bloqueada. Tente novamente após {LockoutMinutes} minutos."
                    };
                }

                if (!BCrypt.Net.BCrypt.Verify(senha, usuario.SenhaHash))
                {
                    _logger.LogWarning("Senha incorreta");
                    await IncrementaTentativasFalhasAsync(email, usuario);
                    await _contexto.LogsErro.AddAsync(new LogErro
                    {
                        MensagemErro = "Senha incorreta para o usuário: " + email,
                        DataErro = DateTime.Now,
                        StackTrace = "Usuário não encontrado",
                        Status = "Erro"
                    });
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Email ou senha inválidos"
                    };
                }

                await ResetTestativasFalhasAsync(email, usuario);   
                var token = GegarJwtToken(usuario);
                var refreshToken = await GerarRefreshToken(usuario.Id);



                _logger.LogInformation("Token gerado com sucesso para o usuário: " + email);
                _logger.LogInformation("Usuário autenticado com sucesso: " + email);
                await _contexto.LogsSucesso.AddAsync(new LogSucesso
                {
                    Mensagem = "Usuário autenticado com sucesso: " + email,
                    Data = DateTime.Now,
                    Status = "Sucesso",
                    Usuario = usuario

                });

                await _contexto.SaveChangesAsync();
                return new AuthResult
                {
                    Success = true,
                    Token = token,
                    RefreshToken = refreshToken,
                    UsuarioId = usuario.Id,
                    nomeCompleto = usuario.NomeCompleto,
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao autenticar usuário");
                return new AuthResult
                {
                    Success = false,
                    Message = "Erro interno ao autenticar"
                };
            }
        }

        public async Task<PasswordResetToken> GerarTokenRecuperacaoSenha(Usuario usuario)
        {
            var token = Guid.NewGuid().ToString();
            var resetToken = new PasswordResetToken
            {
                Token = token,
                UsuarioId = usuario.Id,
                CriadoEm = DateTime.UtcNow,
                ExpiraEm = DateTime.UtcNow.AddHours(1)
            };

            _contexto.PasswordResetTokens.Add(resetToken);
            await _contexto.SaveChangesAsync();
            return resetToken;
        }


        public async Task<bool> ValidarTurnstileToken(string token)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var secretKey = _configuration["Cloudflare:Turnstile:SecretKey"];
                if (string.IsNullOrEmpty(secretKey))
                {
                    _logger.LogError("Chave secreta do Cloudflare Turnstile não configurada.");
                    return false;
                }

                var requestData = new Dictionary<string, string>
        {
            { "secret", secretKey },
            { "response", token }
        };

                var content = new FormUrlEncodedContent(requestData);
                var response = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Resposta do Turnstile: {ResponseBody}", responseBody);

                var result = System.Text.Json.JsonSerializer.Deserialize<TurnstileResponse>(responseBody);
                if (result == null)
                {
                    _logger.LogError("Erro ao desserializar a resposta do Turnstile");
                    return false;
                }

                if (!result.Success)
                {
                    _logger.LogWarning("Validação do Turnstile falhou. Códigos de erro: {ErrorCodes}", string.Join(", ", result.ErrorCodes ?? new string[0]));
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar o token do Turnstile");
                return false;
            }
        }

        public async Task<bool> VerificarEmailECPFexiste(string email, string cpf)
        {
            try
            {
                var usuario = await _contexto.Usuarios.FirstOrDefaultAsync(u =>
                u.Email.ToLower() == email.ToLower() || u.CPF == cpf);
                if (usuario == null)
                {
                    _logger.LogWarning("Usuário com esse E-mail e CPF não encontrado");
                    await _contexto.LogsErro.AddAsync(new LogErro
                    {
                        MensagemErro = "Usuário com esse E-mail e CPF não encontrado: " + email,
                        DataErro = DateTime.Now,
                        StackTrace = "Usuário com esse E-mail e CPF não encontrado",
                        Status = "Erro"
                    });
                    await _contexto.SaveChangesAsync();
                    return false;
                }
                else if (usuario.Email.ToLower() != email.ToLower() || usuario.CPF != cpf)
                {
                    _logger.LogWarning("E-mail e CPF não correspondem ao mesmo usuário");
                    await _contexto.LogsErro.AddAsync(new LogErro
                    {
                        MensagemErro = "E-mail e CPF não correspondem ao mesmo usuário: " + email,
                        DataErro = DateTime.Now,
                        StackTrace = "E-mail e CPF não correspondem ao mesmo usuário",
                        Status = "Erro"
                    });
                    await _contexto.SaveChangesAsync();
                    return false;
                }
                else
                {
                    _logger.LogInformation("E-mail e CPF correspondem ao mesmo usuário");
                    await _contexto.LogsSucesso.AddAsync(new LogSucesso
                    {
                        Mensagem = "E-mail e CPF correspondem ao mesmo usuário: " + email,
                        Data = DateTime.Now,
                        Status = "Sucesso",
                        Usuario = usuario
                    });
                    await _contexto.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar o e-mail e CPF do usuário");
                return false;
            }
        }

        private async Task<string> GerarRefreshToken(int usuarioId)
        {
            var token = Guid.NewGuid().ToString();
            var tokenHash = HashToken(token);

            var expiracao = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays");

            var refreshToken = new RefreshToken
            {
                TokenHash = tokenHash,
                UsuarioId = usuarioId,
                CriadoEm = DateTime.UtcNow,
                ExpiraEm = DateTime.UtcNow.AddDays(expiracao),
                Revoked = false
            };

            _contexto.RefreshTokens.Add(refreshToken);
            await _contexto.SaveChangesAsync();

            return token;
        }

        public async Task<RefreshTokenResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var tokenHash = HashToken(refreshToken);
            var storedToken = await _contexto.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && !rt.Revoked && rt.ExpiraEm > DateTime.UtcNow);
            if (storedToken == null)
            {
                throw new UnauthorizedAccessException("Refresh Token é inválido ou já expirou.");
            }
            var usuario = await _contexto.Usuarios.FindAsync(storedToken.UsuarioId);
            if (usuario == null)
            {
                throw new UnauthorizedAccessException("Usuário não encontrado.");
            }

            var novoJwtToken = GegarJwtToken(usuario);
            var novoRefreshToken = await GerarRefreshToken(usuario.Id);

            storedToken.Revoked = true;
            _contexto.RefreshTokens.Update(storedToken);
            await _contexto.SaveChangesAsync();

            return new RefreshTokenResponseDto
            {
                Token = novoJwtToken,
                RefreshToken = novoRefreshToken
            };
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var tokenHash = HashToken(refreshToken);
            var storedToken = await _contexto.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
            if (storedToken != null)
            {
                storedToken.Revoked = true;
                await _contexto.SaveChangesAsync();
            }
        }

        private string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hashedBytes);
        }


        private async Task<bool> VerificarLockedAsync(string email, Usuario usuario)
        {
            string lockoutKey = $"Lockout_{email}";
            var lockoutData = await _cache.GetStringAsync(lockoutKey);

            if (lockoutData != null)
            {
                return true; 
            }

            if (usuario.LockoutEnd.HasValue && usuario.LockoutEnd > DateTime.UtcNow)
            {
                var tempoRestante = usuario.LockoutEnd.Value - DateTime.UtcNow;
                await _cache.SetStringAsync(lockoutKey, "locked", new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = tempoRestante
                });
                return true;
            }

            return false;
        }

        private async Task IncrementaTentativasFalhasAsync(string email, Usuario usuario)
        {
            string failedAttemptsKey = $"FailedAttempts_{email}";
            int failedAttempts = 0;

            var cachedAttempts = await _cache.GetStringAsync(failedAttemptsKey);
            if (cachedAttempts != null)
            {
                failedAttempts = int.Parse(cachedAttempts);
            }
            else
            {
                failedAttempts = usuario.FailedLoginAttempts;
            }

            failedAttempts++;
            usuario.FailedLoginAttempts = failedAttempts;
            await _contexto.SaveChangesAsync();

           
            await _cache.SetStringAsync(failedAttemptsKey, failedAttempts.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(LockoutMinutes)
            });

            if (failedAttempts >= MaxFailedAttempts)
            {
                usuario.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                await _contexto.SaveChangesAsync();

                string lockoutKey = $"Lockout_{email}";
                await _cache.SetStringAsync(lockoutKey, "locked", new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(LockoutMinutes)
                });

                _logger.LogInformation("Conta bloqueada para o usuário: {Email} por {Minutes} minutos", email, LockoutMinutes);
            }
        }

        private async Task LogFailedAttempt(string email)
        {
            await _contexto.LogsErro.AddAsync(new LogErro
            {
                MensagemErro = "Usuário não encontrado: " + email,
                DataErro = DateTime.UtcNow,
                StackTrace = "Usuário não encontrado",
                Status = "Erro"
            });
            await _contexto.SaveChangesAsync();
        }

        private async Task ResetTestativasFalhasAsync(string email, Usuario usuario)
        {
            string failedAttemptsKey = $"FailedAttempts_{email}";
            string lockoutKey = $"Lockout_{email}";

            usuario.FailedLoginAttempts = 0;
            usuario.LockoutEnd = null;
            await _contexto.SaveChangesAsync();

            await _cache.RemoveAsync(failedAttemptsKey);
            await _cache.RemoveAsync(lockoutKey);
        }
    }
   
}
