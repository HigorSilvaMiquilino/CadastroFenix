using System.ComponentModel.DataAnnotations;

namespace Cadastro.DTO
{
    /// <summary>
    /// Modelo para solicitação de login / Login request model
    /// </summary>
    public class LoginRequestDto
    {
        /// <summary>
        /// Email do usuário 
        /// </summary>
        [Required(ErrorMessage = "Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; }

        /// <summary>
        /// Senha do usuário 
        /// </summary>
        [Required(ErrorMessage = "Senha é obrigatória")]
        [StringLength(80, MinimumLength = 8, ErrorMessage = "Senha deve ter entre 8 e 80 caracteres")]
        public string Senha { get; set; }

        [Required(ErrorMessage = "Token do CAPTCHA é obrigatório")]
       public string CfTurnstileResponse { get; set; } 
    }
}
