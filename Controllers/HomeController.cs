using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TheWalkco.Interfaces;
using TheWalkco.Models;

namespace TheWalkco.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductRepository _productRepository;

        public HomeController(ILogger<HomeController> logger, IProductRepository productRepository)
        {
            _logger = logger;
            _productRepository = productRepository;
        }



        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllProductsAsync(); // await async call
            return View(products.ToList()); 
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}
