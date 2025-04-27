    using Cadastro.Data;
    using Cadastro.DTO;
    using Cadastro.Servicos.Cupom;
    using Cadastro.Servicos.Email;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.EntityFrameworkCore;
    using System.Diagnostics;
    using System.Net.WebSockets;
    using System.Security.Claims;

    namespace Cadastro.Controllers
    {
        [Route("api/v1/[controller]")]
        [ApiController]
        [Authorize]
        [EnableCors("AllowAllOrigins")]
        public class CupomController : Controller
        {

            private readonly CupomServico _cupomServico;
            private readonly ILogger<CupomController> _logger;
            private readonly CadastroContexto _contexto;
            private readonly EnviarEmail _enviarEmail;

            public CupomController(
                CupomServico cupomServico,
                ILogger<CupomController> logger,
                CadastroContexto contexto,
                EnviarEmail enviarEmail)
            {
                _cupomServico = cupomServico;
                _logger = logger;
                _contexto = contexto;
                _enviarEmail = enviarEmail;
            }

            [HttpPost("cadastrar-cupom")]
            [EnableRateLimiting("CadastrarSlidingLimiter")]
            [ProducesResponseType(StatusCodes.Status200OK)]
            [ProducesResponseType(StatusCodes.Status400BadRequest)]
            [ProducesResponseType(StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(StatusCodes.Status409Conflict)]
            [ProducesResponseType(StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> CadastrarCupom([FromBody] CupomDto cupomDto)
            {
                Stopwatch sw = Stopwatch.StartNew();

                var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var usuario = _contexto.Usuarios.FirstOrDefault(u => u.Email == usuarioIdClaim);
                if (usuario == null)
                {
                    return Unauthorized(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status401Unauthorized,
                        Success = false,
                        Message = "Usuário não autorizado"
                    });
                }
                // Era bom esse aqui ter um próprio endpoint
                if (await _cupomServico.EhLimiteCuponsPorUsuario(usuario.Id))
                {
                    ModelState.AddModelError("LimiteCupons", "Limite de cupons atingido para este usuário.");
                }

                if (_cupomServico.EhQuantidadeMaximaProdutos(cupomDto.produtos))
                {
                    ModelState.AddModelError("quantidade", "Quantidade máxima de produtos por cupom é 5.");
                }

                if (_cupomServico.EhQuantidadeMinimaProdutos(cupomDto.produtos))
                {
                    ModelState.AddModelError("quantidade", "Quantidade mínima de produtos por cupom é 1.");
                }

                if (!_cupomServico.EhNumeroCupomFiscalValido(cupomDto.numeroCupom))
                {
                    ModelState.AddModelError("numeroCupom", "Número do cupom fiscal deve ter 8 dígitos.");
                }

                if (!await _cupomServico.EhNumeroCupomFiscalUnico(cupomDto.numeroCupom))
                {
                    ModelState.AddModelError("numeroCupom", "Número do cupom fiscal já cadastrado.");
                }

                if (!_cupomServico.ehCnpjValido(cupomDto.cnpj))
                {
                    ModelState.AddModelError("cnpj", "CNPJ do estabelecimento inválido.");
                }

                if (!await _cupomServico.EhProdutoValido(cupomDto.produtos))
                {
                    ModelState.AddModelError("produto", "Produto inválido.");
                }

                if (!_cupomServico.EhValorValido(cupomDto.produtos))
                {
                    ModelState.AddModelError("valor", "Valor do produto inválido.");
                }


                if (!ModelState.IsValid)
                {
                    var errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).FirstOrDefault()
                    );

                    _logger.LogWarning("Tentativa de cadastrar cupom falhou - validação inválida: {@Erros}", ModelState);
                    _contexto.LogsErro.Add(new LogErro
                    {
                        DataErro = DateTime.Now,
                        MensagemErro = "Tentativa de cadastrar falhou - validação inválida",
                        StackTrace = ModelState.ToString(),
                        Status = "400"
                    });

                    sw.Stop();
                    _contexto.LogsPerformance.Add(new LogPerformance
                    {
                        Data = DateTime.Now,
                        Endpoint = nameof(CadastrarCupom),
                        TempoExecucaoSegundos = sw.ElapsedMilliseconds / 1000.0,
                        UsuarioId = null
                    });
                    await _contexto.SaveChangesAsync();

                    await LogBadRequest("Erro de validação", sw, errors);

                    return BadRequest(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false,
                        Message = "Erro de validação",
                        Errors = errors
                    });
                }

                var quantidade = int.Parse(cupomDto.quantidadeTotal.ToString());

                var cupom = new Cupom
                {
                    NumeroCupomFiscal = cupomDto.numeroCupom,
                    CnpjEstabelecimento = cupomDto.cnpj,
                    DataCompra = _cupomServico.FormatarData(cupomDto.dataCompra),
                    DataCadastro = DateTime.Now,
                    UsuarioId = usuario.Id,
                    quantidadeTotal = quantidade,
                    ValorTotal = _cupomServico.CalcularValorTotal(cupomDto.produtos),
                    Produtos =  _cupomServico.ConverterParaProduto(cupomDto.produtos),
                };

                try
                {

                    await _contexto.Cupons.AddAsync(cupom);
                    await _contexto.SaveChangesAsync();
                    sw.Stop();
                    _contexto.LogsPerformance.Add(new LogPerformance
                    {
                        Data = DateTime.Now,
                        Endpoint = nameof(CadastrarCupom),
                        TempoExecucaoSegundos = sw.ElapsedMilliseconds / 1000.0,
                        UsuarioId = usuario.Id
                    });
                    await _contexto.SaveChangesAsync();

                try
                {
                    await _enviarEmail.EnviarEmailCupomCadastradolAsync(usuario.NomeCompleto, usuario.Email, cupomDto.numeroCupom);
                    _contexto.EmailLogs.Add(new EmailLog
                    {
                        DataEnvio = DateTime.Now,
                        Email = usuario.Email,
                        Status = "Enviado",
                        Mensagem = "Cadastro de cupom realizado com sucesso"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError("Erro ao enviar email: {@Excecao}", ex);
                    _contexto.EmailLogs.Add(new EmailLog
                    {
                        DataEnvio = DateTime.Now,
                        Email = usuario.Email,
                        Status = "Falha",
                        Mensagem = $"Erro ao enviar email: {ex.Message}"
                    });
                }

                return Ok(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status200OK,
                        Success = true,
                        Message = "Cupom cadastrado com sucesso",
                        Metadata = new Dictionary<string, object>
                            {
                                {"cupon" , cupom },
                            }


                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError("Erro ao cadastrar cupom: {@Excecao}", ex);
                    return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
                    {
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Success = false,
                        Message = "Erro ao cadastrar cupom",
                        Errors = new Dictionary<string, string>
                            {
                                { "Erro", ex.Message }
                            }
                    });

                }
            }

            [HttpGet("obterultimocadastrado")]
            [EnableRateLimiting("CadastrarSlidingLimiter")]
            [ProducesResponseType(StatusCodes.Status200OK)]
            [ProducesResponseType(StatusCodes.Status400BadRequest)]
            [ProducesResponseType(StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(StatusCodes.Status404NotFound)]
            [ProducesResponseType(StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> ObterUltimoCupom()
            {
                try
                {
                    var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var usuario = _contexto.Usuarios.FirstOrDefault(u => u.Email == usuarioIdClaim);
                    if (usuario == null)
                    {
                        return Unauthorized(new ApiResponse
                        {
                            StatusCode = StatusCodes.Status401Unauthorized,
                            Success = false,
                            Message = "Usuário não autorizado"
                        });
                    }


                    var cupom = await _cupomServico.ObterUltimoCupomPorUsuario(usuario.Id);
                    if (cupom == null)
                    {
                        return NotFound(new { Success = false, Message = "Nenhum cupom encontrado" });
                    }

                    return Ok(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status200OK,
                        Success = true,
                        Message = "Último cupom encontrado com sucesso",
                        Metadata = new Dictionary<string, object>
                        {
                            {"cupom" , cupom }
                        },
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao obter último cupom");
                    return StatusCode(500, new { Success = false, Message = "Erro interno ao obter cupom" });
                }
            }

            [HttpGet("obtertodos")]
            [EnableRateLimiting("CadastrarSlidingLimiter")]
            [ProducesResponseType(StatusCodes.Status200OK)]
            [ProducesResponseType(StatusCodes.Status400BadRequest)]
            [ProducesResponseType(StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(StatusCodes.Status404NotFound)]
            [ProducesResponseType(StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> ObterTodosCupons()
            {
                try
                {
                    var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var usuario = _contexto.Usuarios.FirstOrDefault(u => u.Email == usuarioIdClaim);
                    if (usuario == null)
                    {
                        return Unauthorized(new ApiResponse
                        {
                            StatusCode = StatusCodes.Status401Unauthorized,
                            Success = false,
                            Message = "Usuário não autorizado"
                        });
                    }

                    var cupons = await _cupomServico.ObterCuponsPorUsuario(usuario.Id);
                    if (cupons == null || !cupons.Any())
                    {
                        return NotFound(new ApiResponse { Success = false, Message = "Nenhum cupom encontrado" });
                    }
                    return Ok(new ApiResponse
                    {
                        StatusCode = StatusCodes.Status200OK,
                        Success = true,
                        Message = "Cupons encontrados com sucesso",
                        Metadata = new Dictionary<string, object>
                        {
                            {"cupons" , cupons }
                        },
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao obter cupons");
                    return StatusCode(500, new { Success = false, Message = "Erro interno ao obter cupons" });
                }
            }

            private async Task LogBadRequest(string mensagemErro, Stopwatch sw, object errors)
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
                    Endpoint = nameof(CadastrarCupom),
                    TempoExecucaoSegundos = sw.ElapsedMilliseconds / 1000.0,
                    UsuarioId = null
                });

                await _contexto.SaveChangesAsync();
            }

        }
    }