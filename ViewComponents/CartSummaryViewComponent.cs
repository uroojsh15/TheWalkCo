using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TheWalkco.Models;

namespace TheWalkco.ViewComponents
{
    public class CartSummaryViewComponent : ViewComponent
    {
        private const string CartCookieKey = "Cart";

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Read the cart cookie
            var cart = GetCartFromCookie();

            // Calculate total items and total price
            int itemCount = cart.Sum(i => i.Quantity);
            decimal totalPrice = cart.Sum(i => i.Quantity * i.UnitPrice);

            var model = new CartSummaryViewModel
            {
                ItemCount = itemCount,
                TotalPrice = totalPrice
            };

            return View(model);
        }

        private List<CartItem> GetCartFromCookie()
        {
            if (Request.Cookies.ContainsKey(CartCookieKey))
            {
                var json = Request.Cookies[CartCookieKey];
                try
                {
                    var cart = JsonSerializer.Deserialize<List<CartItem>>(json);
                    return cart ?? new List<CartItem>();
                }
                catch
                {
                    // If deserialization fails, return empty cart
                    return new List<CartItem>();
                }
            }

            return new List<CartItem>();
        }
    }

    public class CartSummaryViewModel
    {
        public int ItemCount { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
