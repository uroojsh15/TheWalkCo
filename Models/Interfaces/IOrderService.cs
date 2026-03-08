using System.Collections.Generic;
using TheWalkco.Models;

namespace TheWalkco.Interfaces
{
    public interface IOrderService
    {
        Task<int> PlaceOrderAsync(
           string userId,
           string name,
           string email,
           string phone,
           string address,
           List<CartItem> cartItems,
           string paymentMethod
       );

        Task<Order> GetOrderByIdAsync(int id);


    }
}