using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cadastro.DTO
{
    public class ProdutoDto
    {
        [Required]
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [Required, StringLength(100)]
        [JsonPropertyName("descricao")]
        public string Descricao { get; set; }
        [Required]
        [JsonPropertyName("quantidade")]
        public string Quantidade { get; set; } 
        [Required]
        [JsonPropertyName("valor")]
        public string Valor { get; set; } 
    }
}
