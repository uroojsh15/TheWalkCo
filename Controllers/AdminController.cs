using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheWalkco.Interfaces;

[Authorize(Policy = "AdminAccess")]
public class AdminController : Controller
{
    private readonly IProductRepository _productRepo;
    private readonly IOrderRepository _orderRepo;

    public AdminController(IProductRepository productRepo, IOrderRepository orderRepo)
    {
        _productRepo = productRepo;
        _orderRepo = orderRepo;
    }

    public async Task<IActionResult> Dashboard()
    {
        var products = await _productRepo.GetAllProductsAsync();
        var orders = await _orderRepo.GetAllOrdersAsync();

        ViewBag.TotalProducts = products.Count();
        ViewBag.TotalOrders = orders.Count();
        ViewBag.PendingOrders = orders.Count(o => o.Status == "Pending");

        ViewBag.TodayRevenue = orders
            .Where(o => o.OrderDate.Date == DateTime.Today)
            .Sum(o => o.TotalAmount);

        ViewBag.RecentOrders = orders
            .OrderByDescending(o => o.OrderDate)
            .Take(5)
            .Select(o => new
            {
                o.Id,
                o.CustomerName,
                o.OrderDate,
                o.Status,
                o.TotalAmount
            })
            .ToList();

        return View();
    }

    // Add this new action to list all orders for management
    public async Task<IActionResult> Orders()
    {
        var orders = await _orderRepo.GetAllOrdersAsync();

        // Convert to List here to match the view model type
        var ordersList = orders.ToList();

        return View(ordersList);

    }
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int orderId, string status)
    {
        var order = await _orderRepo.GetOrderAsync(orderId);
        if (order == null)
        {
            return NotFound();
        }

        await _orderRepo.UpdateOrderStatusAsync(orderId, status);

        // Optionally notify via SignalR here

        return Ok();
    }
}
