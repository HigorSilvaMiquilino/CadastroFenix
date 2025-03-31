using Cadastro.Servicos.Utilidade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace Cadastro.Tests
{
    public class UtilServicoTests
    {
        private readonly UtilServico _utilServico;

        public UtilServicoTests()
        {
            _utilServico = new UtilServico();
        }

        [Fact]
        public void FormatarNomeCompleto_DeveRetornarNomeCompletoEmMaiusculo()
        {
            var nomeCompleto = "Nome Completo";
            var result = _utilServico.FormatarNomeCompleto(nomeCompleto);
            result.Should().Be("NOME COMPLETO");
        }

        [Fact]
        public void FormatarCPF_DeveRetornarCPFSemPontuacao()
        {
            var cpf = "123.456.789-00";
            var result = _utilServico.FormatarCPF(cpf);
            result.Should().Be("12345678900");
        }

        [Fact]
        public void FormatarDataNascimento_DeveRetornarDataFormatada()
        {
            var dataNascimento = "01/01/2000";
            var result = _utilServico.FormatarDataNascimento(dataNascimento);
            result.Should().Be("2000-01-01");
        }

        [Fact]
        public void FormatarDataNascimento_DeveRetornarErroQuandoDataNascimentoVazia()
        {
            var dataNascimento = "";
            Action act = () => _utilServico.FormatarDataNascimento(dataNascimento);
            act.Should().Throw<ArgumentException>().WithMessage("Data de nascimento não pode ser vazia.");
        }

        [Fact]
        public void FormatarDataNascimento_DeveRetornarErroQuandoDataNascimentoInvalida()
        {
            var dataNascimento = "01/01/2000 00:00:00";
            Action act = () => _utilServico.FormatarDataNascimento(dataNascimento);
            act.Should().Throw<FormatException>().WithMessage("A data '01/01/2000 00:00:00' não está no formato esperado 'dd/MM/yyyy'.");
        }

        [Fact]
        public void FormatarTimestamp_DeveRetornarTimestampFormatado()
        {
            var dateTime = new DateTime(2021, 01, 01, 00, 00, 00);
            var result = _utilServico.FormatarTimestamp(dateTime);
            result.Should().Be("2021-01-01 00:00:00.000");
        }

        [Fact]
        public void FormatarCEP_DeveRetornarCEPSemPontuacao()
        {
            var cep = "12345-678";
            var result = _utilServico.FormatarCEP(cep);
            result.Should().Be("12345678");
        }

        [Fact]
        public void FormatarTelefone_DeveRetornarTelefoneSemPontuacao()
        {
            var telefone = "(11) 1234-5678";
            var result = _utilServico.FormatarTelefone(telefone);
            result.Should().Be("1112345678");
        }

        [Fact]
        public void FormatarGenero_DeveRetornarPrimeiraLetraDoGeneroEmMaiusculo()
        {
            var genero = "masculino";
            var result = _utilServico.FormatarGenero(genero);
            result.Should().Be("M");
        }

        [Fact]
        public void FormatarEmail_DeveRetornarEmailEmMinusculo()
        {
            string email = "TESTE@EXEMPLO.COM";
            var result = _utilServico.FormatarEmail(email);
            result.Should().Be("teste@exemplo.com");
        }
    }
}
