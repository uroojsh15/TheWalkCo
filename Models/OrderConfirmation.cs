namespace TheWalkco.Models
{
    public class OrderConfirmation
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string CustomerName { get; set; }
        public string ShippingAddress { get; set; }
    }
}
