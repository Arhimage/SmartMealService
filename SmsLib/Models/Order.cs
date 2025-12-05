namespace SmsLib.Models
{
    public class Order
    {
        public Guid OrderId { get; set; }
        public List<OrderItem> MenuItems { get; set; }
    }
}
