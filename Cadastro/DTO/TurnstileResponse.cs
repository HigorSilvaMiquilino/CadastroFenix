using System.Text.Json.Serialization;

namespace Cadastro.DTO
{
    public class TurnstileResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonPropertyName("error-codes")]
        public string[] ErrorCodes { get; set; }
    }
}
