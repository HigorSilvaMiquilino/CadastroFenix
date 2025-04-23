using Cadastro.Data;
using Cadastro.DTO;
using Cadastro.Servicos.Auth;
using Cadastro.Servicos.Cadastro;
using Cadastro.Servicos.Email;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cadastro.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [EnableCors("AllowAllOrigins")]
    public class AuthController : Controller
    {
        public readonly AuthServico _authServico;
        public readonly CadastroServico _cadastroServico;
        public readonly CadastroContexto _contexto;
        public readonly ILogger<AuthController> _logger;
        public readonly IDistributedCache _cache;
        public readonly IHttpClientFactory _httpClientFactory;
        private readonly EnviarEmail _enviarEmail;
        private readonly IConfiguration _configuration;
        public AuthController(
            AuthServico authServico,
            CadastroContexto contexto,
            ILogger<AuthController> logger,
            IDistributedCache cache,
            IHttpClientFactory httpClientFactory,
            EnviarEmail enviarEmail,
            CadastroServico cadastroServico,
            IConfiguration configuration)
        {
            _authServico = authServico;
            _contexto = contexto;
            _logger = logger;
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _enviarEmail = enviarEmail;
            _cadastroServico = cadastroServico;
            _configuration = configuration;
        }



        /// <summary>
        /// Autentica um usuário e retorna um token JWT.
        /// </summary>
        /// <param name="model">Dados de login (email e senha)</param>
        /// <returns>Token JWT se bem-sucedido</returns>
        /// <response code="200">Login bem-sucedido</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="401">Email ou senha inválidos</response>
        /// <response code="429">Muitas requisições</response>
        /// <response code="500">Erro no servidor</response>
        [HttpPost("login")]
        [EnableRateLimiting("CadastrarSlidingLimiter")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse))]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var ehValidoCaptcha = await _authServico.ValidarTurnstileToken(model.CfTurnstileResponse);
                if (!ehValidoCaptcha)
                {
                    await LogErroAsync("Falha na validação do CAPTCHA", new Exception("Token do CAPTCHA inválido"));
                    sw.Stop();
                    await LogPerformanceAsync(nameof(Login), sw.Elapsed.TotalSeconds, null);
                    _logger.LogWarning("Falha na validação do CAPTCHA para o usuário: " + model.Email);
                    return BadRequest(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false,
                        Message = "Falha na validação do CAPTCHA"
                    });
                }

                if (!ModelState.IsValid)
                {
                    await LogErroAsync("Dados inválidos", new Exception("Dados inválidos"));
                    sw.Stop();
                    await LogPerformanceAsync(nameof(Login), sw.Elapsed.TotalSeconds, null);

                    return BadRequest(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false,
                        Message = "Dados inválidos"
                    });
                }

                var authResult = await _authServico.AutenticaAsync(model.Email, model.Senha);
                if (!authResult.Success)
                {
                    await LogErroAsync(authResult.Message, new Exception("Email ou senha inválidos"));
                    sw.Stop();
                    await LogPerformanceAsync(nameof(Login), sw.Elapsed.TotalSeconds, null);

                    var metadata = new Dictionary<string, object>();
                    if (authResult.Message.Contains("Conta bloqueada"))
                    {
                        var usuario = await _contexto.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());
                        if (usuario?.LockoutEnd.HasValue == true)
                        {
                            var remainingSeconds = (int)(usuario.LockoutEnd.Value - DateTime.UtcNow).TotalSeconds;
                            metadata.Add("travadaPelosSegundosRestantes", remainingSeconds > 0 ? remainingSeconds : 0);
                        }
                    }

                    return Unauthorized(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status401Unauthorized,
                        Success = false,
                        Message = authResult.Message,
                        Metadata = metadata
                    });
                }

                Response.Cookies.Append("BearerToken", authResult.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/api",
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30)
                });

                Response.Cookies.Append("RefreshToken", authResult.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/api",
                    Expires = DateTimeOffset.UtcNow.AddDays(7) 
                });

                await LogSucessoAsync(authResult.UsuarioId, "Login realizado com sucesso");
                sw.Stop();
                await LogPerformanceAsync(nameof(Login), sw.Elapsed.TotalSeconds, authResult.UsuarioId);

                _logger.LogInformation("Login realizado com sucesso para o usuário: " + model.Email);

                return Ok(new ApiResponse
                {
                    StatusCode = StatusCodes.Status200OK,
                    Success = true,
                    Message = "Login realizado com sucesso",
                    Metadata = new Dictionary<string, Object>
                    {
                        { "nome", authResult.nomeCompleto },
                    }
                });
            }
            catch (Exception ex) 
            { 
                await LogErroAsync("Erro ao autenticar usuário", ex);
                sw.Stop();
                await LogPerformanceAsync(nameof(Login), sw.Elapsed.TotalSeconds, null);

                _logger.LogError(ex, "Erro ao autenticar usuário: " + model.Email);

                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Success = false,
                    Message = "Erro ao autenticar usuário",
                    Errors = new Dictionary<string, string>
                    {
                        { "Erro", ex.Message }
                    }
                });
            }
        }

        [HttpPost("esqueci-senha")]
        [EnableRateLimiting("VerificationSlidingLimiter")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse))]
        public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaRequestDto model)
        {
            var sw = Stopwatch.StartNew();
            if (!ModelState.IsValid)
            {
                await LogErroAsync("E-mail inválido", new Exception("E-mail inválidos"));
                sw.Stop();
                await LogPerformanceAsync(nameof(EsqueciSenha), sw.Elapsed.TotalSeconds, null);
                return BadRequest(new ApiResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "E-mail inválidos"
                });
            }
            try
            {
                var usuario = await _contexto.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());
                if (usuario == null)
                {
                    await LogErroAsync("Email não encontrado", new Exception("Email não encontrado"));
                    sw.Stop();
                    await LogPerformanceAsync(nameof(EsqueciSenha), sw.Elapsed.TotalSeconds, null);
                    return Ok(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status200OK,
                        Success = true,
                        Message = "Se o e-mail existir, um link de redefinição será enviado."
                    });
                }

                var token = await _authServico.GerarTokenRecuperacaoSenha(usuario);

                await _enviarEmail.EnviarRecuperacaoSenhaEmaillAsync(usuario.Email, usuario.NomeCompleto, "Redefinição de Senha", token);

                _logger.LogInformation("E-mail de redefinição de senha enviado para: " + usuario.Email);

                await LogSucessoAsync(usuario.Id, "E-mail de redefinição de senha enviado com sucesso");
                sw.Stop();
                await LogPerformanceAsync(nameof(EsqueciSenha), sw.Elapsed.TotalSeconds, usuario.Id);
                return Ok(new ApiResponse
                {
                    StatusCode = StatusCodes.Status200OK,
                    Success = true,
                    Message = "Se o e-mail existir, um link de redefinição será enviado."
                });
            }
            catch (Exception ex)
            {
                await LogErroAsync("Erro ao enviar e-mail de redefinição de senha", ex);
                sw.Stop();
                await LogPerformanceAsync(nameof(EsqueciSenha), sw.Elapsed.TotalSeconds, null);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Success = false,
                    Message = "Erro ao enviar e-mail de redefinição de senha",
                    Errors = new Dictionary<string, string>
                    {
                        { "Erro", ex.Message }
                    }
                });
            }
        }

        [HttpPost("redefinir-senha")]
        [EnableRateLimiting("VerificationSlidingLimiter")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse))]
        public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaRequestDto model)
        {
            var sw = Stopwatch.StartNew();

            if (!_cadastroServico.ehSenhaValida(model.senha, model.confirmacaoSenha))
            {
                await LogErroAsync("Senha inválida", new Exception("Senha inválida"));
                sw.Stop();
                await LogPerformanceAsync(nameof(RedefinirSenha), sw.Elapsed.TotalSeconds, null);
                _logger.LogWarning("Dados inválidos para redefinição de senha");
                return BadRequest(new ApiResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "Senha deve ter no mínimo 8 caracteres, com maiúscula, minúscula, 2 números e caracter especial, e deve corresponder à confirmação"
                });
            }

            if (model.senha != model.confirmacaoSenha)
            {
                await LogErroAsync("As senhas não coincidem", new Exception("As senhas não coincidem"));
                sw.Stop();
                await LogPerformanceAsync(nameof(RedefinirSenha), sw.Elapsed.TotalSeconds, null);
                _logger.LogWarning("As senhas não coincidem para o usuário");
                return BadRequest(new ApiResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "As senhas não coincidem"
                });
            }

            var resetToken = await _contexto.PasswordResetTokens
                    .FirstOrDefaultAsync(t => t.Token == model.Token && t.ExpiraEm > DateTime.Now);

            if (resetToken == null)
            {
                await LogErroAsync("Token inválido", new Exception("Token inválido"));
                sw.Stop();
                await LogPerformanceAsync(nameof(RedefinirSenha), sw.Elapsed.TotalSeconds, null);
                _logger.LogWarning("Invalid or expired token used for password reset: {Token}", model.Token);
                return BadRequest(new ApiResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "Token inválido ou expirado"
                });
            }



            try
            {
                var usuario = await _contexto.Usuarios.FirstOrDefaultAsync(u => u.Id == resetToken.UsuarioId);
                if (usuario == null)
                {
                    await LogErroAsync("Usuário não encontrado", new Exception("Usuário não encontrado"));
                    sw.Stop();
                    await LogPerformanceAsync(nameof(RedefinirSenha), sw.Elapsed.TotalSeconds, null);
                    _logger.LogWarning("Usuário não encontrado para redefinição de senha: " + resetToken.Id);
                    return NotFound(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status404NotFound,
                        Success = false,
                        Message = "Usuário não encontrado"
                    });
                }

                var ehEmailECPFExistente = await _authServico.VerificarEmailECPFexiste(model.email, model.cpf);
                if (!ehEmailECPFExistente)
                {
                    await LogErroAsync("Usuário com esse E-mail e CPF não encontrado", new Exception("Email ou CPF inválido"));
                    sw.Stop();
                    await LogPerformanceAsync(nameof(RedefinirSenha), sw.Elapsed.TotalSeconds, null);
                    _logger.LogWarning("Email ou CPF inválido para redefinição de senha: " + model.email);
                    return BadRequest(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false,
                        Message = "Usuário com esse E-mail e CPF não encontrado"
                    });
                }

                if (!ModelState.IsValid)
                {
                    await LogErroAsync("Dados inválidos", new Exception("Dados inválidos"));
                    sw.Stop();
                    await LogPerformanceAsync(nameof(RedefinirSenha), sw.Elapsed.TotalSeconds, null);
                    _logger.LogWarning("Dados inválidos para redefinição de senha");
                    return BadRequest(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false,
                        Message = "Dados inválidos"
                    });
                }

                usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(model.senha);
                _contexto.Usuarios.Update(usuario);
                _contexto.PasswordResetTokens.Remove(resetToken);

                await _contexto.SaveChangesAsync();
                _logger.LogInformation("Senha redefinida com sucesso para o usuário: " + usuario.Email);
                await LogSucessoAsync(usuario.Id, "Senha redefinida com sucesso");
                sw.Stop();
                await LogPerformanceAsync(nameof(RedefinirSenha), sw.Elapsed.TotalSeconds, usuario.Id);
                return Ok(new ApiResponse
                {
                    StatusCode = StatusCodes.Status200OK,
                    Success = true,
                    Message = "Senha redefinida com sucesso"
                });
            }
            catch (Exception ex)
            {
                await LogErroAsync("Erro ao redefinir senha", ex);
                sw.Stop();
                await LogPerformanceAsync(nameof(RedefinirSenha), sw.Elapsed.TotalSeconds, null);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Success = false,
                    Message = "Erro ao redefinir senha",
                    Errors = new Dictionary<string, string>
                    {
                        { "Erro", ex.Message }
                    }
                });
            }
        }
        
        [HttpPost("refresh-token")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse))]
        public async Task<IActionResult> RefreshToken()
        {
            var sw = Stopwatch.StartNew();

            var refreshToken = Request.Cookies["RefreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                await LogErroAsync("Refresh token não encontrado", new Exception("Refresh token não encontrado"));
                sw.Stop();
                await LogPerformanceAsync(nameof(RefreshToken), sw.Elapsed.TotalSeconds, null);
                return Unauthorized(new ApiResponse
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Success = false,
                    Message = "Refresh token não encontrado"
                });
            }

            try
            {
                var response = await _authServico.RefreshTokenAsync(refreshToken);

                Response.Cookies.Append("BearerToken", response.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/api",
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30)
                });

                Response.Cookies.Append("RefreshToken", response.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/api",
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

                _logger.LogInformation("Token atualizado com sucesso para o usuário associado ao refresh token");

                return Ok(new ApiResponse
                {
                    StatusCode = StatusCodes.Status200OK,
                    Success = true,
                    Message = "Token atualizado com sucesso",
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                await LogErroAsync("Falha ao atualizar token", ex);
                sw.Stop();
                await LogPerformanceAsync(nameof(RefreshToken), sw.Elapsed.TotalSeconds, null);

                _logger.LogWarning("Falha ao atualizar token: {Message}", ex.Message);
                return Unauthorized(new ApiResponse
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                await LogErroAsync("Erro ao atualizar token", ex);
                sw.Stop();
                await LogPerformanceAsync(nameof(RefreshToken), sw.Elapsed.TotalSeconds, null);

                _logger.LogError(ex, "Erro ao atualizar token");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Success = false,
                    Message = "Erro ao atualizar token",
                    Errors = new Dictionary<string, string>
            {
                { "Erro", ex.Message }
            }
                });
            }
        }
        

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse))]
        public async Task<IActionResult> Logout()
        {
            var sw = Stopwatch.StartNew();

            var bearerToken = Request.Cookies["BearerToken"];
            var refreshToken = Request.Cookies["RefreshToken"];
            if (string.IsNullOrEmpty(refreshToken) && string.IsNullOrEmpty(bearerToken))
            {
                await LogErroAsync("Refresh token ou Bearer Token não encontrado", new Exception("Refresh token ou Bearer Token não encontrado"));
                sw.Stop();
                await LogPerformanceAsync(nameof(Logout), sw.Elapsed.TotalSeconds, null);
                return Unauthorized(new ApiResponse
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Success = false,
                    Message = "Refresh token ou Bearer Token não encontrado"
                });
            }

            try
            {
                await _authServico.RevokeRefreshTokenAsync(refreshToken);

                Response.Cookies.Delete("BearerToken", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/api"
                });
                Response.Cookies.Delete("RefreshToken", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/api"
                });

                sw.Stop();
                await LogPerformanceAsync(nameof(Logout), sw.Elapsed.TotalSeconds, null);

                _logger.LogInformation("Logout realizado com sucesso");

                return Ok(new ApiResponse
                {
                    StatusCode = StatusCodes.Status200OK,
                    Success = true,
                    Message = "Logout realizado com sucesso"
                });
            }
            catch (Exception ex)
            {
                await LogErroAsync("Erro ao realizar logout", ex);
                sw.Stop();
                await LogPerformanceAsync(nameof(Logout), sw.Elapsed.TotalSeconds, null);
                _logger.LogError(ex, "Erro ao realizar logout");

                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Success = false,
                    Message = "Erro ao realizar logout",
                    Errors = new Dictionary<string, string>
                    {
                        { "Erro", ex.Message }
                    }
                });
            }
        }


        /// <summary>
        /// Log de erro 
        /// </summary>
        private async Task LogErroAsync(string mensagem, Exception ex)
        {
            try
            {
                var log = new LogErro
                {
                    DataErro = DateTime.UtcNow,
                    MensagemErro = mensagem,
                    StackTrace = "Email ou senha inválidos",
                    Status = ex != null ? "500" : "400"
                };
                _contexto.LogsErro.Add(log);
                await _contexto.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Erro ao registrar log de erro");
            }
        }

        /// <summary>
        /// Log de performance
        /// </summary>
        private async Task LogSucessoAsync(int usuarioId, string mensagem)
        {
            try
            {
                var log = new LogSucesso
                {
                    Data = DateTime.UtcNow,
                    Mensagem = mensagem,
                    Status = "200",
                    UsuarioId = usuarioId

                };
                _contexto.LogsSucesso.Add(log);
                await _contexto.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Erro ao registrar log de sucesso");
            }
        }

        private async Task LogPerformanceAsync(string endpoint, double tempo, int? usuarioId)
        {
            try
            {
                var log = new LogPerformance
                {
                    Data = DateTime.UtcNow,
                    Endpoint = endpoint,
                    TempoExecucaoSegundos = tempo,
                    UsuarioId = usuarioId,
                };
                _contexto.LogsPerformance.Add(log);
                await _contexto.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Erro ao registrar log de performance");
            }
        }
    }
}
