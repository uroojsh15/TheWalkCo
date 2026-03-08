using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TheWalkco.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; }


        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public List<OrderItem> Items { get; set; } = new();

    }
}
