using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using System.Threading.Tasks;
using TheWalkco.Hubs;
using TheWalkco.Interfaces;
using TheWalkco.Models;

namespace TheWalkco.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepo;
        private readonly IHubContext<ProductHub> _productHubContext; // Add this


        public ProductController(IProductRepository productRepo, IHubContext<ProductHub> productHubContext)
        {
            _productRepo = productRepo;
            _productHubContext = productHubContext;
        }


        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return PartialView("_ProductCards", Enumerable.Empty<Product>());
            }

            var products = await _productRepo.SearchProductsAsync(term.Trim());

            return PartialView("_ProductCards", products);
        }



        [AllowAnonymous]
        public async Task<IActionResult> Index(string? category)
        {
            var products = string.IsNullOrEmpty(category) || category.ToLower() == "all"
                ? await _productRepo.GetAllProductsAsync()
                : await _productRepo.GetProductsByCategoryAsync(category);

            ViewBag.SelectedCategory = category;
            return View(products);
        }


        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepo.GetProductByIdAsync(id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> AdminIndex()
        {
            var products = await _productRepo.GetAllProductsAsync();
            return View(products);
        }

        [Authorize(Policy = "AdminAccess")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Policy = "AdminAccess")]
        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            await _productRepo.AddProductAsync(product);
            await _productHubContext.Clients.All.SendAsync("ReceiveNewProduct", product.Name, product.Id);

            return RedirectToAction(nameof(AdminIndex));
        }

        [Authorize(Policy = "AdminAccess")]

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepo.GetProductByIdAsync(id);

            if (product == null)
                return NotFound();

            return View(product);
        }
        [Authorize(Policy = "AdminAccess")]



        [HttpPost]
        public async Task<IActionResult> Edit(Product product)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            await _productRepo.UpdateProductAsync(product);

            return RedirectToAction(nameof(AdminIndex));
        }

        [Authorize(Policy = "AdminAccess")]

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepo.GetProductByIdAsync(id);

            if (product == null)
                return NotFound();

            return View(product);
        }
        [Authorize(Policy = "AdminAccess")]

        [HttpPost, ActionName("Delete")]

        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productRepo.DeleteProductAsync(id);
            return RedirectToAction(nameof(AdminIndex));
        }
    }
}
