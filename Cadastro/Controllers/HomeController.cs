using Microsoft.AspNetCore.Mvc;

namespace Cadastro.Controllers
{
    [Route("api/[controller]")]
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return PhysicalFile("wwwroot/html/index.html", "text/html");
        }
    }
}
