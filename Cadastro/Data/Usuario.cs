
using System.ComponentModel.DataAnnotations;

namespace Cadastro.Data
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required, StringLength(11, MinimumLength = 11)]
        public string CPF { get; set; }
        [Required, StringLength(120)]
        public string NomeCompleto { get; set; }

        [Required, StringLength(10)] 
        public string DataNascimento { get; set; }

        [Required, StringLength(20)]
        public string Genero { get; set; }
        [Required, StringLength(15)]
        public string Telefone { get; set; }
        [Required, StringLength(200)]
        public string Email { get; set; }
        [Required]
        public string SenhaHash { get; set; }
        public int EnderecoId { get; set; }
        public virtual Endereco Endereco { get; set; }
        public bool AceiteTermos { get; set; } = false;

        [Required, StringLength(45)]
        public string UserIp { get; set; }

        [Required, StringLength(23)] 
        public string DataCreate { get; set; }

        [StringLength(23)] 
        public string? DataUpdate { get; set; }
    }
}
