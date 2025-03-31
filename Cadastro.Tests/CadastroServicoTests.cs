using Cadastro.Data;
using Cadastro.Servicos.Cadastro;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Text.Json;
using System.Threading.Tasks;
using Funcionarios = Cadastro.Data.Funcionarios;

namespace Cadastro.Tests
{
    public class CadastroServicoTests : IDisposable
    {
        private readonly CadastroContexto _context; 
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<ILogger<CadastroServico>> _mockLogger;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly CadastroServico _cadastroServico;

        public CadastroServicoTests()
        {
            var options = new DbContextOptionsBuilder<CadastroContexto>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
                .Options;
            _context = new CadastroContexto(options);

            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockLogger = new Mock<ILogger<CadastroServico>>();
            _mockCache = new Mock<IDistributedCache>();

            _cadastroServico = new CadastroServico(_context, _mockHttpClientFactory.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public void EhCpfValido_ValidaCPF_RetornaVerdadeiro()
        {
            
            string cpf = "529.982.247-25";

            
            var result = _cadastroServico.ehCpfValido(cpf);

            
            result.Should().BeTrue();
        }

        [Fact]
        public void EhCpfValido_InvalidoCPF_RetornaFalso()
        {

            string cpf = "123.456.789-00";

            var result = _cadastroServico.ehCpfValido(cpf);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task EhCPFUnico_CpfNotInDatabase_ReturnsTrue()
        {

            string cpf = "12345678909";

            _mockCache.Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null);

            _mockCache.Setup(m => m.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _cadastroServico.ehCPFUnico(cpf, _mockCache.Object);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task EhCPFUnico_CpfInDatabase_ReturnsFalse()
        {
            string cpf = "12345678909";
            _context.Usuarios.Add(new Usuario
            {
                CPF = "12345678909",
                NomeCompleto = "John Doe",
                Telefone = "11912345678",
                DataNascimento = "01/01/1990",
                Genero = "Masculino",
                Email = "john.doe@example.com",
                SenhaHash = "hashedpassword",
                DataCreate = DateTime.UtcNow.ToString(),
                UserIp = "127.0.0.1"
            });
            await _context.SaveChangesAsync();
            _mockCache.Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null);
            _mockCache.Setup(m => m.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _cadastroServico.ehCPFUnico(cpf, _mockCache.Object);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task EhCPFFuncionario_CpfNotInFuncionarios_ReturnsTrue()
        {
            string cpf = "12345678909";
            _mockCache.Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null);
            _mockCache.Setup(m => m.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var result = await _cadastroServico.ehCPFFuncionario(cpf, _mockCache.Object);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task EhCPFFuncionario_CpfInFuncionarios_ReturnsFalse()
        {
            string cpf = "12345678909";
            _context.Funcionarios.Add(new Funcionarios
            {
                CPF = "12345678909",
                NomeCompleto = "John Doe",
                motivo = "Funcionário"
            });
            await _context.SaveChangesAsync();
            _mockCache.Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null);
            _mockCache.Setup(m => m.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
           
            var result = await _cadastroServico.ehCPFFuncionario(cpf, _mockCache.Object);

            result.Should().BeFalse();
        }

        [Fact]
        public void EhEmailValido_ValidEmail_ReturnsTrue()
        {
            string email = "test@example.com";
            string confirmacaoEmail = "test@example.com";

            var result = _cadastroServico.ehEmailValido(email, confirmacaoEmail);

            result.Should().BeTrue();
        }

        [Fact]
        public void EhEmailValido_InvalidEmailFormat_ReturnsFalse()
        {
            string email = "invalid-email";
            string confirmacaoEmail = "invalid-email";

            var result = _cadastroServico.ehEmailValido(email, confirmacaoEmail);

            result.Should().BeFalse();
        }

        [Fact]
        public void EhEmailValido_EmailsDoNotMatch_ReturnsFalse()
        {
            string email = "test@example.com";
            string confirmacaoEmail = "different@example.com";

         
            var result = _cadastroServico.ehEmailValido(email, confirmacaoEmail);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task EhTelefoneUnico_TelefoneNaoNoBancoDeDados_RetornaVerdadeiro()
        {
            string telefone = "11912345678";
            _mockCache.Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null);
            _mockCache.Setup(m => m.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _cadastroServico.ehTelefoneUnico(telefone, _mockCache.Object);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task EhTelefoneUnico_TelefoneNoBancoDeDados_RetornaFalso()
        {
            string telefone = "11912345678";
            _context.Usuarios.Add(new Usuario
            {
                CPF = "12345678909",
                NomeCompleto = "John Doe",
                DataNascimento = "01/01/1990",
                Genero = "Masculino",
                Email = "john.doe@example.com",
                Telefone = "11912345678",
                SenhaHash = "hashedpassword",
                DataCreate = DateTime.UtcNow.ToString(),
                UserIp = "127.0.0.1"
            });
            await _context.SaveChangesAsync();
            _mockCache.Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null);
            _mockCache.Setup(m => m.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _cadastroServico.ehTelefoneUnico(telefone, _mockCache.Object);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task EhCepValido_InvalidCep_ReturnsFalse()
        {
            string cep = "invalid-cep";
            string estado = "SP";
            string cidade = "São Paulo";
            string bairro = "Centro";
            string logradouro = "Rua Exemplo";

            var result = await _cadastroServico.ehCepValido(cep, estado, cidade, bairro, logradouro);

            result.Should().BeFalse();
        }
    }


    public class ViaCepResponse
    {
        public string Cep { get; set; }
        public string Uf { get; set; }
        public string Localidade { get; set; }
        public string Bairro { get; set; }
        public string Logradouro { get; set; }
        public bool? Erro { get; set; }
    }
}