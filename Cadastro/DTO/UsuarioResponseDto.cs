namespace Cadastro.DTO
{
    public class UsuarioResponseDto
    {
        public int Id { get; set; }
        public string CPF { get; set; }
        public string NomeCompleto { get; set; }
        public string DataNascimento { get; set; }
        public string Genero { get; set; }
        public string Telefone { get; set; }
        public string Email { get; set; }
        public string SenhaHash { get; set; }
        public EnderecoResponseDto Endereco { get; set; }
        public bool AceiteTermos { get; set; }
    }
}
