namespace FilteringUtility.Domain
{
    public class Order
    {
        public string OrderNumber { get; set; }
        public double Weight { get; set; }
        public string District { get; set; }
        public DateTime DeliveryDateTime { get; set; }
    }
}
