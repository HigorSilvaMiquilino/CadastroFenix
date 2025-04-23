using Cadastro.Data;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cadastro.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [EnableCors("AllowAllOrigins")]
    public class ProdutoController : Controller
    {
        private readonly CadastroContexto _contexto;

        public ProdutoController(CadastroContexto contexto)
        {
            _contexto = contexto;
        }

        [HttpGet("Produtos")]
        public async Task<ActionResult<List<Produto>>> GetProdutos()
        {
            var produtos = await _contexto.Produtos.Where(p => p.quantidade == 0).ToListAsync();
            return Ok(produtos);
        }
    }
}
