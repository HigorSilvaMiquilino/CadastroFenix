using Cadastro.Data;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cadastro.DTO
{
    public class CupomDto
    {
        [Required, StringLength(8, MinimumLength = 8)]
        [JsonPropertyName("numeroCupom")]
        public string numeroCupom { get; set; }

        [Required, StringLength(18)]
        [JsonPropertyName("cnpj")]
        public string cnpj { get; set; }

        [Required]
        [JsonPropertyName("dataCompra")]
        public string dataCompra { get; set; }

        [Required]
        public DateTime DataCadastro { get; set; }

        [Required, MinLength(1), MaxLength(2)]
        [JsonPropertyName("produtos")]
        public List<ProdutoDto> produtos { get; set; }

        [Required]
        [JsonPropertyName("quantidadeTotal")]
        public string quantidadeTotal { get; set; }

        [Required]
        [JsonPropertyName("forcarErro")]

        public bool forcarErro { get; set; }

        [Required]
        [JsonPropertyName("valorTotal")]
        public decimal ValorTotal { get; set; }

    }
}
