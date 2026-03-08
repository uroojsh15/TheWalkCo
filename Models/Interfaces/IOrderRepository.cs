using System.Collections.Generic;
using TheWalkco.Models;

namespace TheWalkco.Interfaces
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<Order> GetOrderAsync(int id);
        Task CreateOrderAsync(Order order);
        Task UpdateOrderStatusAsync(int orderId, string status);

    }
}
