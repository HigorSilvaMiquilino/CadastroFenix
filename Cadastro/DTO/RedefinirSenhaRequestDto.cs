using System.ComponentModel.DataAnnotations;

namespace Cadastro.DTO
{
    public class RedefinirSenhaRequestDto
    {
        [Required(ErrorMessage = "O token é obrigatório")]
        public string Token { get; set; }

        [Required(ErrorMessage = "A nova senha é obrigatória")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 100 caracteres")]
        public string senha { get; set; }

        [Required(ErrorMessage = "A confirmação da senha é obrigatória")]
        [Compare("senha", ErrorMessage = "As senhas não coincidem")]
        public string confirmacaoSenha { get; set; }

        [Required, StringLength(11, MinimumLength = 11)]
        public string cpf { get; set; }

        [Required, StringLength(200)]
        public string email { get; set; }

    }
}
