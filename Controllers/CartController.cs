using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TheWalkco.Interfaces;
using TheWalkco.Models;

namespace TheWalkco.Controllers
{
    [Authorize(Policy = "UserAccess")]

    public class CartController : Controller
    {
        private const string CartCookieKey = "Cart";

        private readonly IProductRepository _productRepository;

        public CartController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        private List<CartItem> GetCart()
        {
            if (HttpContext.Request.Cookies.ContainsKey(CartCookieKey))
            {
                var json = HttpContext.Request.Cookies[CartCookieKey];
                return JsonSerializer.Deserialize<List<CartItem>>(json);
            }

            return new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            var options = new CookieOptions
            {
                Expires = System.DateTime.Now.AddDays(7),
                HttpOnly = true
            };

            var json = JsonSerializer.Serialize(cart);

            HttpContext.Response.Cookies.Append(CartCookieKey, json, options);
        }

        private void ClearCart()
        {
            if (HttpContext.Request.Cookies.ContainsKey(CartCookieKey))
            {
                HttpContext.Response.Cookies.Delete(CartCookieKey);
            }
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, string size, int quantity = 1)
        {
            var product = await _productRepository.GetProductByIdAsync(productId);
            if (product == null)
                return NotFound();

            var cart = GetCart();

            var existing = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                string categoryFolder = "others";

                if (!string.IsNullOrEmpty(product.Category))
                {
                    var cat = product.Category.ToLower().Replace(" ", "");
                    if (cat.StartsWith("women"))
                        categoryFolder = "womenshoes";
                    else if (cat.StartsWith("men"))
                        categoryFolder = "menshoes";
                }
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ImageUrl = $"/images/{categoryFolder}/{product.ImageUrl}",
                      // <-- Set Category = product.Category, this here
                    Size = size,
                    Quantity = quantity,
                    UnitPrice = product.Price
                });
            }

            SaveCart(cart);

            return RedirectToAction("Index");
        }

        public IActionResult Remove(int productId, string size)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, string size, int quantity)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }

                SaveCart(cart);
            }

            return RedirectToAction("Index");
        }

        public IActionResult Clear()
        {
            ClearCart();
            return RedirectToAction("Index");
        }
    }
}
