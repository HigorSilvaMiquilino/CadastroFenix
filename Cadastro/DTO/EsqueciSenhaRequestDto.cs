using System.ComponentModel.DataAnnotations;

namespace Cadastro.DTO
{
    public class EsqueciSenhaRequestDto
    {
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; }
    }
}
