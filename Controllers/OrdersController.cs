using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TheWalkco.Hubs;
using TheWalkco.Interfaces;
using TheWalkco.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;



namespace TheWalkco.Controllers
{

    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IOrderRepository _orderRepo;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly ILogger<OrdersController> _logger;
        public OrdersController(IOrderService orderService, IOrderRepository orderRepo,
                                IHubContext<OrderHub> hubContext, ILogger<OrdersController> logger  )
        {
            _orderService = orderService;
            _orderRepo = orderRepo;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var orders = (await _orderRepo.GetAllOrdersAsync())
                         .Where(o => o.UserId == userId)
                         .ToList();

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderRepo.GetOrderAsync(id);

            if (order == null)
                return NotFound();

            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (order.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            return View(order);
        }

        private List<CartItem> GetCart()
        {
            const string CartCookieKey = "Cart";

            if (HttpContext.Request.Cookies.ContainsKey(CartCookieKey))
            {
                var json = HttpContext.Request.Cookies[CartCookieKey];
                return JsonSerializer.Deserialize<List<CartItem>>(json);
            }

            return new List<CartItem>();
        }
        private void ClearCart()
        {
            const string CartCookieKey = "Cart";

            if (HttpContext.Request.Cookies.ContainsKey(CartCookieKey))
            {
                HttpContext.Response.Cookies.Delete(CartCookieKey);
            }
        }
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(
     string name,
     string email,
     string phone,
     string address,
     string paymentMethod)
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var cartItems = GetCart();
            if (cartItems == null || !cartItems.Any())
                return BadRequest("Your cart is empty.");

            int orderId = await _orderService.PlaceOrderAsync(
                userId,
                name,
                email,
                phone,
                address,
                cartItems,
                paymentMethod
            );

            ClearCart();

            var payload = new
            {
                orderId = orderId,
                customerName = name,
                timestamp = DateTime.Now.ToString("HH:mm")
            };

            _logger.LogInformation("SignalR notification about to send for order {OrderId}", orderId);

            try
            {
                // Send only to admin group instead of all clients
                await _hubContext.Clients.Group("admins").SendAsync("NewOrder", payload);
                _logger.LogInformation("SignalR notification sent to admins for order {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending SignalR notification for order {OrderId}", orderId);
            }

            return RedirectToAction("Details", new { id = orderId });
        }


    }
}