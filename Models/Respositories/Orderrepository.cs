using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TheWalkco.Interfaces;
using TheWalkco.Models;

namespace TheWalkco.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(IConfiguration config, ILogger<OrderRepository> logger)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        private async Task<SqlConnection> CreateOpenConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task CreateOrderAsync(Order order)
        {
            try
            {
                await using var db = await CreateOpenConnectionAsync();

                // Insert main order
                string orderSql = @"
            INSERT INTO Orders (UserId, CustomerName, CustomerEmail, CustomerPhone, ShippingAddress, PaymentMethod, TotalAmount, OrderDate, Status) 
            VALUES (@UserId, @CustomerName, @CustomerEmail, @CustomerPhone, @ShippingAddress, @PaymentMethod, @TotalAmount, @OrderDate, @Status);
            SELECT CAST(SCOPE_IDENTITY() as int);";

                int orderId = await db.QuerySingleAsync<int>(orderSql, new
                {
                    order.UserId,
                    order.CustomerName,
                    order.CustomerEmail,
                    order.CustomerPhone,
                    order.ShippingAddress,
                    order.PaymentMethod,
                    order.TotalAmount,
                    order.OrderDate,
                    Status = order.Status ?? "Pending"
                });

                order.Id = orderId;

                // Insert items
                if (order.Items != null)
                {
                    foreach (var item in order.Items)
                    {
                        string itemSql = @"
                    INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice, Size)
                    VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice, @Size);";

                        await db.ExecuteAsync(itemSql, new
                        {
                            OrderId = orderId,
                            item.ProductId,
                            item.Quantity,
                            item.UnitPrice,
                            item.Size
                        });
                    }
                }

                _logger.LogInformation("Order created successfully with ID: {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                throw;
            }
        }

        public async Task<Order> GetOrderAsync(int id)
        {
            await using var db = await CreateOpenConnectionAsync();

            string orderSql = "SELECT * FROM Orders WHERE Id = @Id";
            var order = await db.QueryFirstOrDefaultAsync<Order>(orderSql, new { Id = id });

            if (order == null) return null;

            string itemsSql = @"
                SELECT oi.*, p.*
                FROM OrderItems oi
                INNER JOIN Products p ON oi.ProductId = p.Id
                WHERE oi.OrderId = @OrderId";

            var items = await db.QueryAsync<OrderItem, Product, OrderItem>(
                itemsSql,
                (item, product) =>
                {
                    item.Product = product;
                    return item;
                },
                new { OrderId = id },
                splitOn: "ProductId"
            );

            order.Items = items.ToList();
            return order;
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            await using var db = await CreateOpenConnectionAsync();

            string sql = @"
                SELECT o.*, oi.*, p.*
                FROM Orders o
                LEFT JOIN OrderItems oi ON oi.OrderId = o.Id
                LEFT JOIN Products p ON p.Id = oi.ProductId
                ORDER BY o.OrderDate DESC, o.Id";

            var orderDict = new Dictionary<int, Order>();

            var results = await db.QueryAsync<Order, OrderItem, Product, Order>(
                sql,
                (order, item, product) =>
                {
                    if (!orderDict.TryGetValue(order.Id, out var current))
                    {
                        current = order;
                        current.Items = new List<OrderItem>();
                        orderDict.Add(order.Id, current);
                    }
                    if (item != null)
                    {
                        item.Product = product;
                        current.Items.Add(item);
                    }
                    return current;
                },
                splitOn: "Id,ProductId"
            );

            return orderDict.Values;
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status)
        {
            await using var db = await CreateOpenConnectionAsync();

            string sql = "UPDATE Orders SET Status = @Status WHERE Id = @Id";

            await db.ExecuteAsync(sql, new { Id = orderId, Status = status });
        }
    }
}
