using System.ComponentModel.DataAnnotations;

namespace Cadastro.Data
{
    public class Produto
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string descricao { get; set; }

        [Required]
        public int quantidade { get; set; }

        [Required]
        public decimal valor { get; set; }
    }
}
