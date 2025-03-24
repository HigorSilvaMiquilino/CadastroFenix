using Cadastro.Data;
using Cadastro.DTO;
using Cadastro.Servicos.Auth;
using Cadastro.Servicos.Cadastro;
using Cadastro.Servicos.Email;
using Cadastro.Servicos.Utilidade;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

namespace Cadastro.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CadastroController : Controller
    {
        private readonly CadastroServico _cadastroServico;
        private readonly CadastroContexto _contexto;
        private readonly ILogger<CadastroController> _logger;
        private readonly UtilServico _utilServico;
        private readonly EnviarEmail _enviarEmail;
        private readonly AuthServico _authServico;


        public CadastroController(
            CadastroServico cadastroServico,
            ILogger<CadastroController> logger,
            CadastroContexto contexto,
            UtilServico utilServico,
            EnviarEmail enviarEmail,
            AuthServico authServico)
        {
            _cadastroServico = cadastroServico;
            _logger = logger;
            _contexto = contexto;
            _utilServico = utilServico;
            _enviarEmail = enviarEmail;
            _authServico = authServico;

        }

        /// <summary>
        /// Registra um novo usuário com endereço.
        /// </summary>
        /// <response code="201">Usuário cadastrado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="404">Dados não incontrado</response>
        /// <response code="429">Muitas requisições</response>
        /// <response code="500">Erro no servidor</response>
        [HttpPost("Cadastrar")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Cadastrar([FromBody] SolicitacaoCadastroModel solicitacaoCadastro)
        {
            Stopwatch sw = Stopwatch.StartNew();


            if (!_cadastroServico.ehCpfValido(solicitacaoCadastro.CPF))
            {
                ModelState.AddModelError("CPF", "CPF inválido");
            }

            if (!_cadastroServico.ehCPFUnico(solicitacaoCadastro.CPF))
            {
                ModelState.AddModelError("CPF", "CPF já cadastrado");
            }

            if (!_cadastroServico.ehCPFFuncionario(solicitacaoCadastro.CPF))
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

            if (!_cadastroServico.ehTelefoneUnico(solicitacaoCadastro.Telefone))
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

            if (!_cadastroServico.ehEmailUnico(solicitacaoCadastro.Email))
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

                return BadRequest(new
                {
                    success = false,
                    message = "Erro de validação",
                    errors
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

                /*
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
                */

                _logger.LogInformation("Usuário cadastrado com sucesso: {Email}, CPF: {CPF}, Tempo: {tempo}", usuarioCompleto.Email, usuarioCompleto.CPF, sw.ElapsedMilliseconds / 1000.0 + " segundos");

                return CreatedAtAction(nameof(Cadastrar), new { id = usuario.Id }, new
                {
                    success = true,
                    message = "Cadastro realizado com sucesso",
                    bearerToken = token
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
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro no servidor",
                    errors = new Dictionary<string, string>()
                });
            }
        }

        /// <summary>
        /// Verifica se o CPF é válido, único e não pertence a um funcionário.
        /// </summary>
        /// <response code="200">CPF disponível</response>
        /// <response code="400">CPF inválido</response>
        /// <response code="400">CPF já está em uso</response>
        /// <response code="400">Funcionários não podem se cadastrar</response>
        ///         [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("verificar-cpf")]
        public async Task<IActionResult> VerificarCpf([FromQuery] string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "CPF é obrigatório"
                });
            }

            try
            {

                if (!_cadastroServico.ehCpfValido(cpf))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "CPF inválido"
                    });
                }

                if (!_cadastroServico.ehCPFFuncionario(cpf))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Funcionários não podem se cadastrar"
                    });
                }

                bool cpfUnico = await _cadastroServico.ehCPFUnicoAsync(cpf);
                if (!cpfUnico)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "CPF já está em uso"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "CPF disponível"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar CPF: {CPF}", cpf);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro no servidor ao verificar CPF"
                });
            }
        }

        /// <summary>
        /// Verifica se o email é válido e único.
        /// </summary>
        /// <response code="200">Email disponível</response>
        /// <response code="400">Email é obrigatório</response>
        /// <response code="400">Email inválido</response>
        /// <response code="400">Email já está em uso</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("verificar-email")]
        public async Task<IActionResult> VerificarEmail([FromQuery] string email, string confirmacao)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email é obrigatório"
                });
            }
            try
            {
                if (!_cadastroServico.ehEmailValido(email, confirmacao))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Email inválido"
                    });
                }
                bool emailUnico = await _cadastroServico.ehEmailUnicoAsync(email);
                if (!emailUnico)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Email já está em uso"
                    });
                }
                return Ok(new
                {
                    success = true,
                    message = "Email disponível"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar Email: {Email}", email);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro no servidor ao verificar Email"
                });
            }
        }

        /// <summary>
        /// Verifica se o telefone é válido e único.
        /// </summary>
        /// <response code="200">Telefone disponível</response>
        /// <response code="400">Telefone é obrigatório</response>
        /// <response code="400">Telefone inválido</response>
        /// <response code="400">Telefone já está em uso</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("verificar-telefone")]
        public async Task<IActionResult> VerificarTelefone([FromQuery] string telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Telefone é obrigatório"
                });
            }
            try
            {
                if (!_cadastroServico.ehTelefoneValido(telefone))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Telefone inválido"
                    });
                }
                bool telefoneUnico = await _cadastroServico.ehTelefoneUnicoAsync(telefone);
                if (!telefoneUnico)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Telefone já está em uso"
                    });
                }
                return Ok(new
                {
                    success = true,
                    message = "Telefone disponível"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar Telefone: {Telefone}", telefone);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro no servidor ao verificar Telefone"
                });
            }
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

            return BadRequest(new
            {
                success = false,
                message = "Erro de validação",
                errors
            });
        }

        private async Task<IActionResult> LogAndReturnBadRequest(string mensagemErro, Stopwatch sw, object errors)
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
            return BadRequest(new
            {
                success = false,
                message = "Erro de validação",
                errors
            });
        }
    }
}
