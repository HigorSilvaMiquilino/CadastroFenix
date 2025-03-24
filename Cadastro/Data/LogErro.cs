namespace Cadastro.Data
{
    public class LogErro
    {
        public int Id { get; set; }
        public DateTime DataErro { get; set; }
        public string MensagemErro { get; set; }
        public string StackTrace { get; set; }
        public string Status { get; set; }
    }
}
