using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cadastro.Data
{
    public class Funcionarios
    {
        public int Id { get; set; }

        [Required, StringLength(11, MinimumLength = 11)]
        public string CPF { get; set; }

        [Required, StringLength(120)]
        public string NomeCompleto { get; set; }

        [Required, StringLength(120)]
        public string motivo { get; set; }
    }
}
