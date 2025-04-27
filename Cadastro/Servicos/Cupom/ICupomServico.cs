
using Cadastro.Data;
using Cadastro.DTO;

namespace Cadastro.Servicos.Cupom
{
    public interface ICupomServico
    {
        decimal CalcularValorTotal(List<ProdutoDto> produtos);
        bool ehCnpjValido(string cnpj);
        Task<bool> EhLimiteCuponsPorUsuario(int usuarioId);
        Task<bool> EhNumeroCupomFiscalUnico(string numeroCupomFiscal);
        bool EhNumeroCupomFiscalValido(string numeroCupomFiscal);
        bool EhQuantidadeMaximaProdutos(List<ProdutoDto> produto);
        bool EhQuantidadeMinimaProdutos(List<ProdutoDto> produto);
        bool EhValorValido(List<ProdutoDto> produto);
        DateTime FormatarData(string data);
        Task<List<Data.Cupom>> ObterCuponsPorUsuario(int usuarioId);
        Task<Data.Cupom> ObterUltimoCupomPorUsuario(int usuarioId);
    }
}
