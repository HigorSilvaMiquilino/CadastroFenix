namespace Cadastro.Data
{
    public class LogSucesso
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public string Status { get; set; }

        public string Mensagem { get; set; }

        public int UsuarioId { get; set; }

        public virtual Usuario Usuario { get; set; }


    }
}
