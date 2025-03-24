using Cadastro.Data;

namespace Cadastro.Servicos.Auth
{
    public interface IAuthServico
    {
        string GegarJwtToken(Usuario usuario);

    }
}
