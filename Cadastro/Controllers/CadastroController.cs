using Cadastro.Data;
using Cadastro.DTO;
using Cadastro.Servicos.Auth;
using Cadastro.Servicos.Cadastro;
using Cadastro.Servicos.Email;
using Cadastro.Servicos.Utilidade;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

namespace Cadastro.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [EnableCors("AllowAllOrigins")]
    public class CadastroController : Controller
    {
        private readonly CadastroServico _cadastroServico;
        private readonly CadastroContexto _contexto;
        private readonly ILogger<CadastroController> _logger;
        private readonly UtilServico _utilServico;
        private readonly EnviarEmail _enviarEmail;
        private readonly AuthServico _authServico;
        private readonly IDistributedCache _cache;


        public CadastroController(
            CadastroServico cadastroServico,
            ILogger<CadastroController> logger,
            CadastroContexto contexto,
            UtilServico utilServico,
            EnviarEmail enviarEmail,
            AuthServico authServico,
            IDistributedCache cache)
        {
            _cadastroServico = cadastroServico;
            _logger = logger;
            _contexto = contexto;
            _utilServico = utilServico;
            _enviarEmail = enviarEmail;
            _authServico = authServico;
            _cache = cache;

        }

        /// <summary>
        /// Registra um novo usuário.
        /// </summary>
        /// <response code="201">Usuário cadastrado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="404">Dados não incontrado</response>
        /// <response code="429">Muitas requisições</response>
        /// <response code="500">Erro no servidor</response>
        [HttpPost("Cadastrar")]
        [EnableRateLimiting("CadastrarSlidingLimiter")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse))]
        public async Task<IActionResult> Cadastrar([FromBody] SolicitacaoCadastroModel solicitacaoCadastro)
        {
            Stopwatch sw = Stopwatch.StartNew();


            if (!_cadastroServico.ehCpfValido(solicitacaoCadastro.CPF))
            {
                ModelState.AddModelError("CPF", "CPF inválido");
            }

            if (!await _cadastroServico.ehCPFUnico(solicitacaoCadastro.CPF, _cache))
            {
                ModelState.AddModelError("CPF", "CPF já cadastrado");
            }

            if (!await _cadastroServico.ehCPFFuncionario(solicitacaoCadastro.CPF, _cache))
            {
                ModelState.AddModelError("CPF", "CPF já cadastrado como funcionário");
            }

            if (!_cadastroServico.ehNomeCompletoValido(solicitacaoCadastro.NomeCompleto))
            {
                ModelState.AddModelError("NomeCompleto", "Nome completo deve ter pelo menos duas partes com 2+ caracteres (ex: 'aa aa')");
            }

            if (!_cadastroServico.ehDataNascimentoValida(solicitacaoCadastro.DataNascimento))
            {
                ModelState.AddModelError("DataNascimento", "Data inválida ou menor de 18 anos");
            }

            if (!_cadastroServico.ehGeneroValido(solicitacaoCadastro.Genero))
            {
                ModelState.AddModelError("Genero", "Gênero deve ser 'Feminino', 'Masculino', 'Outro' ou 'Prefiro não responder'");
            }

            if (!_cadastroServico.ehTelefoneValido(solicitacaoCadastro.Telefone))
            {
                ModelState.AddModelError("Telefone", "Telefone inválido");
            }

            if (!await _cadastroServico.ehTelefoneUnico(solicitacaoCadastro.Telefone, _cache))
            {
                ModelState.AddModelError("Telefone", "Telefone já cadastrado");
            }

            if (!await _cadastroServico.ehCepValido(
                 solicitacaoCadastro.CEP,
                    solicitacaoCadastro.Estado,
                    solicitacaoCadastro.Cidade,
                    solicitacaoCadastro.Bairro,
                    solicitacaoCadastro.Logradouro))
            {
                ModelState.AddModelError("CEP", "CEP inválido ou dados de endereço não correspondem");
            }

            if (!_cadastroServico.ehEmailValido(solicitacaoCadastro.Email, solicitacaoCadastro.ConfirmacaoEmail))
            {
                ModelState.AddModelError("Email", "Email inválido ou não corresponde à confirmação (ex: aa@a.aa, sem múltiplos @)");
            }

            if (!await _cadastroServico.ehEmailUnico(solicitacaoCadastro.Email, _cache))
            {
                ModelState.AddModelError("Email", "Email já cadastrado");
            }

            if (!_cadastroServico.ehSenhaValida(solicitacaoCadastro.Senha, solicitacaoCadastro.ConfirmacaoSenha))
            {
                ModelState.AddModelError("Senha", "Senha deve ter no mínimo 8 caracteres, com maiúscula, minúscula, 2 números e caracter especial, e deve corresponder à confirmação");
            }

            if (!_cadastroServico.ehAceiteTermosValido(solicitacaoCadastro.AceiteTermos))
            {
                ModelState.AddModelError("AceiteTermos", "Você deve aceitar os termos para prosseguir");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key.ToLower(),
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).FirstOrDefault()
                );

                _logger.LogWarning("Tentativa de cadastro falhou - validação inválida: {@Erros}", ModelState);
                _contexto.LogsErro.Add(new LogErro
                {
                    DataErro = DateTime.Now,
                    MensagemErro = "Tentativa de cadastro falhou - validação inválida",
                    StackTrace = ModelState.ToString(),
                    Status = "400"
                });

                sw.Stop();
                _contexto.LogsPerformance.Add(new LogPerformance
                {
                    Data = DateTime.Now,
                    Endpoint = nameof(Cadastrar),
                    TempoExecucaoSegundos = sw.ElapsedMilliseconds / 1000.0,
                    UsuarioId = null
                });
                await _contexto.SaveChangesAsync();

                LogBadRequest("Erro de validação", sw, errors);

                return BadRequest(new ApiResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "Erro de validação",
                    Errors = errors
                });

                
            }

            var endereco = new Endereco
            {
                CEP = _utilServico.FormatarCEP(solicitacaoCadastro.CEP),
                Logradouro = _utilServico.FormatarLogradouro(solicitacaoCadastro.Logradouro),
                Numero = solicitacaoCadastro.Numero,
                Bairro = _utilServico.FormatarBairro(solicitacaoCadastro.Bairro),
                Estado = solicitacaoCadastro.Estado,
                Cidade = _utilServico.FormatarCidade(solicitacaoCadastro.Cidade)
            };

            var userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

            var usuario = new Usuario
            {
                CPF = _utilServico.FormatarCPF(solicitacaoCadastro.CPF),
                NomeCompleto = _utilServico.FormatarNomeCompleto(solicitacaoCadastro.NomeCompleto),
                DataNascimento = _utilServico.FormatarDataNascimento(solicitacaoCadastro.DataNascimento),
                Genero = _utilServico.FormatarGenero(solicitacaoCadastro.Genero),
                Telefone = _utilServico.FormatarTelefone(solicitacaoCadastro.Telefone),
                Email = _utilServico.FormatarEmail(solicitacaoCadastro.Email),
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(solicitacaoCadastro.Senha),
                EnderecoId = endereco.Id,
                Endereco = endereco,
                AceiteTermos = solicitacaoCadastro.AceiteTermos,
                UserIp = userIp,
                DataCreate = _utilServico.FormatarTimestamp(DateTime.Now),
                DataUpdate = null
            };

            try
            {
                _contexto.Usuarios.Add(usuario);
                await _contexto.SaveChangesAsync();

                _contexto.LogsSucesso.Add(new LogSucesso
                {
                    Data = DateTime.Now,
                    Mensagem = "Usuário cadastrado com sucesso",
                    Status = "201",
                    UsuarioId = usuario.Id
                });

                sw.Stop();
                _contexto.LogsPerformance.Add(new LogPerformance
                {
                    Data = DateTime.Now,
                    Endpoint = nameof(Cadastrar),
                    TempoExecucaoSegundos = sw.ElapsedMilliseconds / 1000.0,
                    UsuarioId = usuario.Id
                });

                await _contexto.SaveChangesAsync();


                var usuarioCompleto = await _contexto.Usuarios
                    .Include(u => u.Endereco)
                    .FirstOrDefaultAsync(u => u.Id == usuario.Id);

                var usuarioDto = new UsuarioResponseDto
                {
                    Id = usuarioCompleto.Id,
                    CPF = usuarioCompleto.CPF,
                    NomeCompleto = usuarioCompleto.NomeCompleto,
                    DataNascimento = usuarioCompleto.DataNascimento,
                    Genero = usuarioCompleto.Genero,
                    Telefone = usuarioCompleto.Telefone,
                    Email = usuarioCompleto.Email,
                    SenhaHash = usuarioCompleto.SenhaHash,
                    AceiteTermos = usuarioCompleto.AceiteTermos,
                    Endereco = new EnderecoResponseDto
                    {
                        Id = usuarioCompleto.Endereco.Id,
                        CEP = usuarioCompleto.Endereco.CEP,
                        Logradouro = usuarioCompleto.Endereco.Logradouro,
                        Numero = usuarioCompleto.Endereco.Numero,
                        Bairro = usuarioCompleto.Endereco.Bairro,
                        Estado = usuarioCompleto.Endereco.Estado,
                        Cidade = usuarioCompleto.Endereco.Cidade,
                        UsuarioId = usuarioCompleto.Id
                    }
                };

                var token = _authServico.GegarJwtToken(usuario);

                var baseUrl = $"{this.Request.Scheme}://{this.Request.Host}";
                var imagemUrl = $"{baseUrl}/wwwroot/img/fenix.jpg";



                
                try
                {
                    await _enviarEmail.EnviarEmailslAsync(usuarioDto.Email, usuarioDto.NomeCompleto, "Cadastro realizado com sucesso", "Seja bem-vindo ao nosso sistema Fenix!", imagemUrl);
                    _contexto.EmailLogs.Add(new EmailLog
                    {
                        DataEnvio = DateTime.Now,
                        Email = usuarioDto.Email,
                        Status = "Enviado",
                        Mensagem = "Cadastro realizado com sucesso"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao enviar email de boas-vindas para: {Email}", usuarioDto.Email);
                    _contexto.LogsErro.Add(new LogErro
                    {
                        DataErro = DateTime.Now,
                        MensagemErro = "Erro ao enviar email de boas-vindas",
                        StackTrace = ex.StackTrace,
                        Status = "500"
                    });
                    await _contexto.SaveChangesAsync();
                }
                

                Response.Cookies.Append("BearerToken", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.Now.AddHours(1),
                    Path = "/api",
                });
                _logger.LogInformation("BearerToken cookie set for user: {Email}, CPF: {CPF}", usuarioCompleto.Email, usuarioCompleto.CPF);

                await _cache.RemoveAsync($"email_unico_{usuarioCompleto.Email.ToLower()}");
                await _cache.RemoveAsync($"cpf_unico_{new string(usuarioCompleto.CPF.Where(char.IsDigit).ToArray())}");
                await _cache.RemoveAsync($"funcionario_unico_{new string(usuarioCompleto.CPF.Where(char.IsDigit).ToArray())}");
                await _cache.RemoveAsync($"telefone_unico_{new string(usuarioCompleto.Telefone.Where(char.IsDigit).ToArray())}");

                _logger.LogInformation("Usuário cadastrado com sucesso: {Email}, CPF: {CPF}, Tempo: {tempo}", usuarioCompleto.Email, usuarioCompleto.CPF, sw.ElapsedMilliseconds / 1000.0 + " segundos");
                return StatusCode(StatusCodes.Status201Created, new ApiResponse
                {
                    StatusCode = StatusCodes.Status201Created,
                    Success = true,
                    Message = "Cadastro realizado com sucesso"
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cadastrar usuário: {Email}, CPF: {CPF}", solicitacaoCadastro.Email, solicitacaoCadastro.CPF);
                _contexto.LogsErro.Add(new LogErro
                {
                    DataErro = DateTime.Now,
                    MensagemErro = "Erro interno ao salvar cadastro",
                    StackTrace = ex.StackTrace,
                    Status = "500"
                });

                sw.Stop();
                _contexto.LogsPerformance.Add(new LogPerformance
                {
                    Data = DateTime.Now,
                    Endpoint = nameof(Cadastrar),
                    TempoExecucaoSegundos = sw.ElapsedMilliseconds / 1000.0,
                    UsuarioId = usuario.Id > 0 ? usuario.Id : null
                });

                await _contexto.SaveChangesAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Success = false,
                    Message = "Erro no servidor",
                    Errors = new Dictionary<string, string>()
                });
            }
        }

        /// <summary>
        /// Verifica se o CPF é válido, único e não pertence a um funcionário.
        /// </summary>
        /// <param name="cpf">CPF a ser verificado</param>
        /// <response code="200">CPF disponível</response>
        /// <response code="400">CPF inválido</response>
        /// <response code="400">CPF já está em uso</response>
        /// <response code="400">Funcionários não podem se cadastrar</response>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse))]
        [HttpGet("verificar-cpf")]
        [EnableRateLimiting("VerificationSlidingLimiter")]
        public async Task<IActionResult> VerificarCpf([FromQuery] string cpf)
        {

            if (string.IsNullOrWhiteSpace(cpf))
            {
                return BadRequest(new ApiResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "CPF é obrigatório"
                });
            }

            try
            {

                if (!_cadastroServico.ehCpfValido(cpf))
                {
                    return BadRequest(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false,
                        Message = "CPF inválido"
                    });
                }

                if (!await _cadastroServico.ehCPFFuncionario(cpf, _cache))
                {
                    return BadRequest(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false,
                        Message = "Funcionários não podem se cadastrar"
                    });
                }

                bool cpfUnico = await _cadastroServico.ehCPFUnicoAsync(cpf, _cache);
                if (!cpfUnico)
                {
                    return BadRequest(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false,
                        Message = "CPF já está em uso"
                    });
                }

                return Ok(new ApiResponse
                {
                    StatusCode = StatusCodes.Status200OK,
                    Success = true,
                    Message = "CPF disponível"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar CPF: {CPF}", cpf);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Success = false,
                    Message = "Erro no servidor ao verificar CPF"
                });
            }
        }

        /// <summary>
        /// Verifica se o email é válido e único.
        /// </summary>
        /// <param name="email">Email a ser verificado</param>
        /// <param name="confirmacao">Confirmação do email</param>
        /// <response code="200">Email disponível</response>
        /// <response code="400">Email é obrigatório</response>
        /// <response code="400">Email inválido</response>
        /// <response code="400">Email já está em uso</response>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse))]
        [HttpGet("verificar-email")]
        [EnableRateLimiting("VerificationSlidingLimiter")]
        public async Task<IActionResult> VerificarEmail([FromQuery] string email, string confirmacao)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new ApiResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "Email é obrigatório"
                });
            }
            try
            {
                if (!_cadastroServico.ehEmailValido(email, confirmacao))
                {
                    return BadRequest(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false,
                        Message = "Email inválido"
                    });
                }
                bool emailUnico = await _cadastroServico.ehEmailUnicoAsync(email, _cache);
                if (!emailUnico)
                {
                    return BadRequest(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false,
                        Message = "Email já está em uso"
                    });
                }

                return Ok(new ApiResponse
                {
                    StatusCode = StatusCodes.Status200OK,
                    Success = true,
                    Message = "Email disponível"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar Email: {Email}", email);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Success = false,
                    Message = "Erro no servidor ao verificar Email"
                });
            }
        }

        /// <summary>
        /// Verifica se o telefone é válido e único.
        /// </summary>
        /// <param name="telefone">Telefone a ser verificado</param>
        /// <response code="200">Telefone disponível</response>
        /// <response code="400">Telefone é obrigatório</response>
        /// <response code="400">Telefone inválido</response>
        /// <response code="400">Telefone já está em uso</response>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse))]
        [HttpGet("verificar-telefone")]
        [EnableRateLimiting("VerificationSlidingLimiter")]
        public async Task<IActionResult> VerificarTelefone([FromQuery] string telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone))
            {
                return BadRequest(new ApiResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "Telefone é obrigatório"
                });
            }
            try
            {
                if (!_cadastroServico.ehTelefoneValido(telefone))
                {
                    return BadRequest(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false,
                        Message = "Telefone inválido"
                    });
                }
                bool telefoneUnico = await _cadastroServico.ehTelefoneUnicoAsync(telefone, _cache);
                if (!telefoneUnico)
                {
                    return BadRequest(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false,
                        Message = "Telefone já está em uso"
                    });
                }

                return Ok(new ApiResponse
                {
                    StatusCode = StatusCodes.Status200OK,
                    Success = true,
                    Message = "Telefone disponível"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar Telefone: {Telefone}", telefone);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Success = false,
                    Message = "Erro no servidor ao verificar Telefone"
                });
            }
        }


        [HttpPost("test-sliding-limiter")]
        [EnableRateLimiting("CadastrarSlidingLimiter")]
        public IActionResult TestSlidingLimiter()
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                success = false,
                message = "Você fez muitas requisições. Por favor, aguarde 60 segundos antes de tentar novamente.",
                retryAfter = 60,
                limit = 5,
                window = "1 minuto",
            });
        }


        [HttpGet("test-bad-request")]
        public IActionResult TestBadRequest()
        {
            var errors = new Dictionary<string, string>
        {
            { "cpf", "CPF inválido" },
            { "nomecompleto", "Nome completo deve ter pelo menos duas partes com 2+ caracteres (ex: 'aa aa')" },
            { "datanascimento", "Data inválida ou menor de 18 anos" },
            { "genero", "Gênero deve ser 'Feminino', 'Masculino', 'Outro' ou 'Prefiro não responder'" },
            { "telefone", "Telefone já está em uso" },
            { "cep", "CEP inválido ou dados de endereço não correspondem" },
            { "email", "Email inválido ou não corresponde à confirmação (ex: aa@a.aa, sem múltiplos @)" },
            { "senha", "Senha deve ter no mínimo 8 caracteres, com maiúscula, minúscula, 2 números e caracter especial, e deve corresponder à confirmação" },
            { "aceitetermos", "Você deve aceitar os termos para prosseguir" }
        };

            return BadRequest(new ApiResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "Erro de validação",
                Errors = errors
            });
        }

        private async void LogBadRequest(string mensagemErro, Stopwatch sw, object errors)
        {
            _logger.LogWarning("Erro: {MensagemErro}, {@Erros}", mensagemErro, errors);
            _contexto.LogsErro.Add(new LogErro
            {
                DataErro = DateTime.Now,
                MensagemErro = mensagemErro,
                StackTrace = ModelState.ToString(),
                Status = "400"
            });

            sw.Stop();
            _contexto.LogsPerformance.Add(new LogPerformance
            {
                Data = DateTime.Now,
                Endpoint = nameof(Cadastrar),
                TempoExecucaoSegundos = sw.ElapsedMilliseconds / 1000.0,
                UsuarioId = null
            });

            await _contexto.SaveChangesAsync();
        }
    }
}
