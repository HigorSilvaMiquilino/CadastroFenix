using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cadastro.Data
{
    public class Endereco
    {
        public int Id { get; set; }

        [Required, StringLength(8, MinimumLength = 8)]
        public string CEP { get; set; }

        [Required, StringLength(150), Column(name: "Endereco")]
        public string Logradouro { get; set; }

        [Required, StringLength(100)]
        public string Numero { get; set; }

        [Required, StringLength(100)]
        public string Bairro { get; set; }

        [Required, StringLength(2)]
        public string Estado { get; set; }

        [Required, StringLength(100)]
        public string Cidade { get; set; }
        public int UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; }
    }
}
