using System.ComponentModel.DataAnnotations;

namespace Cadastro.Data
{
    public class PasswordResetToken
    {
        public int Id { get; set; }
        [Required]
        public string Token { get; set; }
        public int UsuarioId { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime ExpiraEm { get; set; }
        public virtual Usuario Usuario { get; set; }
    }
}
