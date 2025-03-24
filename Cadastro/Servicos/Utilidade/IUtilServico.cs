

namespace Cadastro.Servicos.Utilidade
{
    public interface IUtilServico
    {
        string FormatarBairro(string bairro);
        string FormatarCEP(string cep);
        string FormatarCidade(string cidade);
        string FormatarCPF(string cpf);
        string FormatarDataNascimento(string dataNascimento);
        string FormatarEmail(string email);
        string FormatarGenero(string genero);
        string FormatarLogradouro(string logradouro);
        string FormatarNomeCompleto(string nomeCompleto);
        string FormatarTimestamp(DateTime dateTime);
    }
}
