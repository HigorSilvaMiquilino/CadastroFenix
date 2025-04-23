namespace Cadastro.Data
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string TokenHash { get; set; }
        public int UsuarioId { get; set; }   
        public DateTime CriadoEm { get; set; } 
        public DateTime ExpiraEm { get; set; } 
        public bool Revoked { get; set; } = false; 
        public virtual Usuario Usuario { get; set; } 
    }
}
