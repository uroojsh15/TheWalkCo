using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using TheWalkco.Interfaces;
using TheWalkco.Models;

namespace TheWalkco.Controllers
{
    [AllowAnonymous]
    public class CheckoutController : Controller
    {
        private readonly IOrderService _orderService;
        private const string CartCookieKey = "Cart";

        public CheckoutController(IOrderService orderService)
        {
            _orderService = orderService;
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

        private void ClearCart()
        {
            if (HttpContext.Request.Cookies.ContainsKey(CartCookieKey))
            {
                HttpContext.Response.Cookies.Delete(CartCookieKey);
            }
        }

        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCart();

            if (cart == null || !cart.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> Index(string customerName, string customerEmail, string customerPhone, string shippingAddress, string paymentMethod)
        {
            var cart = GetCart();

            if (cart == null || !cart.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(customerEmail))
                customerEmail = emailClaim;

            int orderId = await _orderService.PlaceOrderAsync(
                userId,
                customerName,
                customerEmail,
                customerPhone,
                shippingAddress,
                cart,
                paymentMethod);

            ClearCart();

            return RedirectToAction("Confirmation", new { id = orderId });
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            var model = new OrderConfirmation
            {
                OrderId = order.Id,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                CustomerName = order.CustomerName,
                ShippingAddress = order.ShippingAddress
            };

            return View(model);
        }
    }
}
