using System.ComponentModel.DataAnnotations;

namespace Cadastro.DTO
{
    public class SolicitacaoCadastroModel
    {
        public string CPF { get; set; }

        public string NomeCompleto { get; set; }

        public string DataNascimento { get; set; }

        public string Genero { get; set; }
        public string Telefone { get; set; }

        public string CEP { get; set; }

        public string Logradouro { get; set; }

        public string Numero { get; set; }

        public string Bairro { get; set; }

        public string Estado { get; set; }

        public string Cidade { get; set; }
        public string Email { get; set; }
        public string ConfirmacaoEmail { get; set; }

        public string Senha { get; set; }

        public string ConfirmacaoSenha { get; set; }

        public bool AceiteTermos { get; set; }
    }
}
