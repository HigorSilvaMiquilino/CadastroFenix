using System.ComponentModel.DataAnnotations;

namespace Cadastro.DTO
{
    public class SolicitacaoCadastroModel
    {
        /// <summary>
        /// CPF do usuário 
        /// </summary>
        public string CPF { get; set; }

        /// <summary>
        /// Nome completo do usuário 
        /// </summary>
        public string NomeCompleto { get; set; }

        /// <summary>
        /// Data de nascimento do usuário no formato dd/MM/yyyy
        /// </summary>
        public string DataNascimento { get; set; }

        /// <summary>
        /// Gênero do usuário
        /// </summary>
        public string Genero { get; set; }

        /// <summary>
        /// Telefone do usuário 
        /// </summary>
        public string Telefone { get; set; }

        /// <summary>
        /// CEP do usuário
        /// </summary>
        public string CEP { get; set; }

        /// <summary>
        /// Logradouro do usuário
        /// </summary>
        public string Logradouro { get; set; }


        /// <summary>
        ///  Número do endereço do usuário
        /// </summary>
        public string Numero { get; set; }

        /// <summary>
        /// Bairro do usuário
        /// </summary>
        public string Bairro { get; set; }

        /// <summary>
        /// Estado do usuário
        /// </summary>
        public string Estado { get; set; }

        /// <summary>
        /// Estado do usuário
        /// </summary>
        public string Cidade { get; set; }

        /// <summary>
        /// Email do usuário 
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Confirmação do email do usuário
        /// </summary>
        public string ConfirmacaoEmail { get; set; }


        /// <summary>
        /// Senha do usuário
        /// </summary>
        public string Senha { get; set; }

        /// <summary>
        /// Confirmação da senha do usuário
        /// </summary>
        public string ConfirmacaoSenha { get; set; }

        /// <summary>
        /// Aceite dos termos de uso
        /// </summary>
        public bool AceiteTermos { get; set; }

        [Required(ErrorMessage = "Token do CAPTCHA é obrigatório")]
        public string CfTurnstileResponse { get; set; }
    }
}
