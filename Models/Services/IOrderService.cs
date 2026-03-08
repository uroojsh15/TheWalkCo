using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheWalkco.Interfaces;
using TheWalkco.Models;

namespace TheWalkco.Services
{
    public class OrderService : IOrderService
    {
        private readonly IProductRepository _productRepo;
        private readonly IOrderRepository _orderRepo;

        public OrderService(IProductRepository productRepo, IOrderRepository orderRepo)
        {
            _productRepo = productRepo;
            _orderRepo = orderRepo;
        }

        public async Task<int> PlaceOrderAsync(
            string userId,
            string name,
            string email,
            string phone,
            string address,
            List<CartItem> cartItems,
            string paymentMethod
        )
        {
            decimal totalAmount = 0;

            foreach (var item in cartItems)
            {
                var product = await _productRepo.GetProductByIdAsync(item.ProductId);

                if (product == null)
                    throw new Exception("Product not found");

                if (product.Stock < item.Quantity)
                    throw new Exception($"Not enough stock for {product.Name}");

                totalAmount += item.LineTotal;

                product.Stock -= item.Quantity;
                await _productRepo.UpdateProductAsync(product);
            }


           
            var order = new Order
            {
                UserId = userId,
                CustomerName = name,
                CustomerEmail = email,
                CustomerPhone = phone,
                ShippingAddress = address,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                Status = "Pending",
                PaymentMethod = paymentMethod,
                Items = new List<OrderItem>()
            };

            foreach (var item in cartItems)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,

                    Size = item.Size,
                    UnitPrice = item.UnitPrice
                });
            }

            await _orderRepo.CreateOrderAsync(order);

            return order.Id;
        }

        public async Task<Order> GetOrderByIdAsync(int id)
        {
            return await _orderRepo.GetOrderAsync(id);
        }
    }
}
