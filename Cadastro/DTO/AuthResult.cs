namespace Cadastro.DTO
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string Message { get; set; }
        public int UsuarioId { get; set; }
        public string nomeCompleto { get; set; }    
    }
}
