using Cadastro.Data;
using Cadastro.DTO;
using Cadastro.Migrations;
using Cadastro.Servicos.Cadastro;
using Microsoft.EntityFrameworkCore;

namespace Cadastro.Servicos.Cupom
{
    public class CupomServico : ICupomServico
    {
        private readonly CadastroContexto _contexto;
        private readonly ILogger<CadastroServico> _logger;
        private const int MAX_CUPONS_POR_CLIENTE = 100;
        private const int MAX_PRODUTOS_POR_CUPOM = 5;

        public CupomServico(CadastroContexto contexto, ILogger<CadastroServico> logger)
        {
            _contexto = contexto;
            _logger = logger;
        }

        public async Task<bool> EhLimiteCuponsPorUsuario(int usuarioId)
        {
            var cuponsUsuario = await _contexto.Cupons
                .CountAsync(c => c.UsuarioId == usuarioId);
            if (cuponsUsuario > MAX_CUPONS_POR_CLIENTE)
            {
                return true;
            }
            return false;
        }

        public bool EhQuantidadeMaximaProdutos(int quantidadeProdutos)
        {
            if (quantidadeProdutos > MAX_PRODUTOS_POR_CUPOM)
            {
                return true;
            }
            return false;
        }

        public bool EhQuantidadeMinimaProdutos(int quantidadeProdutos)
        {
            if (quantidadeProdutos < 1)
            {
                return true;
            }
            return false;
        }

        public bool EhNumeroCupomFiscalValido(string numeroCupomFiscal)
        {
            return numeroCupomFiscal.Length == 8 && numeroCupomFiscal.All(char.IsDigit);
        }

        public async Task<bool> EhNumeroCupomFiscalUnico(string numeroCupomFiscal)
        {
            var cupom = await _contexto.Cupons
                .FirstOrDefaultAsync(c => c.NumeroCupomFiscal == numeroCupomFiscal);
            if (cupom != null)
            {
                return false;
            }
            return true;
        }

        public bool ehCnpjValido(string cnpj)
        {
            int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma;
            int resto;
            string digito;
            string tempCnpj;
            cnpj = cnpj.Trim();
            cnpj = cnpj.Replace(".", "").Replace("-", "").Replace("/", "");
            if (cnpj.Length != 14)
                return false;
            tempCnpj = cnpj.Substring(0, 12);
            soma = 0;
            for (int i = 0; i < 12; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];
            resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = resto.ToString();
            tempCnpj = tempCnpj + digito;
            soma = 0;
            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];
            resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = digito + resto.ToString();
            return cnpj.EndsWith(digito);
        }

        public async Task<bool> EhProdutoValido(List<ProdutoDto> produtos)
        {
            foreach (var produto in produtos)
            {
                var produtoId = int.Parse(produto.Id.ToString());
                var produtoExistente = await _contexto.Produtos
                    .FirstOrDefaultAsync(p => p.Id == produtoId);
                if (produtoExistente == null)
                {
                    return false;
                }
            }
            return true;
        }

        public bool EhValorValido(decimal valor)
        {
            if (valor < 0)
            {
                return false;
            }
            return true;
        }

        public async Task<Data.Cupom> ObterUltimoCupomPorUsuario(int usuarioId)
        {
            var cupom = await _contexto.Cupons
                .Where(c => c.UsuarioId == usuarioId)
                .OrderByDescending(c => c.DataCadastro)
                .FirstOrDefaultAsync();
            return cupom;
        }

        public async Task<List<Data.Cupom>> ObterCuponsPorUsuario(int usuarioId)
        {
            var cupons = await _contexto.Cupons
                .Where(c => c.UsuarioId == usuarioId)
                .ToListAsync();
            return cupons;
        }

        public DateTime FormatarData(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                throw new ArgumentException("Data não pode ser vazia.");

            try
            {
                return DateTime.ParseExact(data, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                throw new FormatException($"A data '{data}' não está no formato esperado 'dd/MM/yyyy'.");
            }
        }

        public decimal CalcularValorTotal(List<ProdutoDto> produtos)
        {
            decimal valorTotal = 0;
            foreach (var produto in produtos)
            {
                var quantidade =  int.Parse(produto.Quantidade.ToString());
                var valor = decimal.Parse(produto.Valor.ToString());
                valorTotal += quantidade * valor;
            }
            return valorTotal;
        }

        public List<Produto> ConverterParaProduto(List<ProdutoDto> produtoDto)
        {
            var list = new List<Produto>();
            foreach (var produto in produtoDto)
            {
                var produtoConvertido = new Produto
                {
                    descricao = produto.Descricao,
                    quantidade = int.Parse(produto.Quantidade.ToString()),
                    valor = decimal.Parse(produto.Valor.ToString())
                };
                list.Add(produtoConvertido);
            }
            return list;

        }
    }
}
