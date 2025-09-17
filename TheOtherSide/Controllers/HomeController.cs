using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TheOtherSide.Models;

namespace TheOtherSide.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Mujer()
        {
            return View("mujer/Mujer");
        }

        public IActionResult Comida()
        {
            return View();
        }

        public IActionResult Hombres()
        {
            return View();
        }

        public IActionResult Ninos()
        {
            return View();

        }

        public IActionResult Producto1Mujer()
        {
            return View("mujer/producto1mujer");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
