
using Microsoft.Extensions.Caching.Distributed;

namespace Cadastro.Servicos.Cadastro
{
    public interface ICadastroServico
    {
        bool ehAceiteTermosValido(bool aceiteTermos);
        Task<bool> ehCepValido(string cep, string estado, string cidade, string bairro, string logradouro);
        Task<bool> ehCPFFuncionario(string cpf, IDistributedCache cache);
        Task<bool> ehCPFUnico(string cpf, IDistributedCache cache);
        bool ehCpfValido(string cpf);
        bool ehDataNascimentoValida(string dataNascimento);
        bool ehEmailValido(string email, string confirmacaoEmail);
        bool ehGeneroValido(string genero);
        bool ehNomeCompletoValido(string nomeCompleto);
        bool ehSenhaValida(string senha, string confirmacaoSenha);
        Task<bool> ehTelefoneUnico(string telefone, IDistributedCache cache);
        bool ehTelefoneValido(string telefone);

        Task<bool> ehCPFUnicoAsync(string cpf, IDistributedCache cache);
        Task<bool> ehEmailUnicoAsync(string email, IDistributedCache cache);
        Task<bool> ehEmailUnico(string email, IDistributedCache cache);
        Task<bool> ehTelefoneUnicoAsync(string telefone, IDistributedCache cache);
    }
}
