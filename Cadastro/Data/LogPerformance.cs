using System.ComponentModel.DataAnnotations;

namespace Cadastro.Data
{
    public class LogPerformance
    {
        public int Id { get; set; }

        [Required]
        public DateTime Data { get; set; }

        [Required, StringLength(100)]
        public string Endpoint { get; set; }

        [Required]
        public double TempoExecucaoSegundos { get; set; }

        public int? UsuarioId { get; set; } 

        public virtual Usuario Usuario { get; set; }
    }
}
