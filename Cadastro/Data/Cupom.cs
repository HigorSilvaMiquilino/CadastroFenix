using System.ComponentModel.DataAnnotations;

namespace Cadastro.Data
{
    public class Cupom
    {
        public int Id { get; set; }

        [Required]
        public string NumeroCupomFiscal { get; set; }
        [Required]
        public string CnpjEstabelecimento { get; set; }
        public DateTime DataCompra { get; set; }

        public DateTime DataCadastro { get; set; }
        public int UsuarioId { get; set; }

        public List<Produto> Produtos { get; set; }

        public int quantidadeTotal { get; set; }

        public decimal ValorTotal { get; set; }
    }
}
