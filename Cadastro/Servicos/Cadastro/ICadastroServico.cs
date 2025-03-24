
namespace Cadastro.Servicos.Cadastro
{
    public interface ICadastroServico
    {
        bool ehAceiteTermosValido(bool aceiteTermos);
        Task<bool> ehCepValido(string cep, string estado, string cidade, string bairro, string logradouro);
        bool ehCPFFuncionario(string cpf);
        bool ehCPFUnico(string cpf);
        bool ehCpfValido(string cpf);
        bool ehDataNascimentoValida(string dataNascimento);
        bool ehEmailValido(string email, string confirmacaoEmail);
        bool ehGeneroValido(string genero);
        bool ehNomeCompletoValido(string nomeCompleto);
        bool ehSenhaValida(string senha, string confirmacaoSenha);
        bool ehTelefoneUnico(string telefone);
        bool ehTelefoneValido(string telefone);

        Task<bool> ehCPFUnicoAsync(string cpf);
        Task<bool> ehEmailUnicoAsync(string email);
    }
}
