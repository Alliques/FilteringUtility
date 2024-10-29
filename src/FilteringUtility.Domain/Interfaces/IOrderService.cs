namespace FilteringUtility.Domain
{
    public interface IOrderService
    {
        List<Order> GetOrders(string cityDistrict, DateTime? firstDeliveryDateTimeStart = null, DateTime? firstDeliveryDateTimeEnd = null);
        Task SaveFilteredOrdersAsync(IEnumerable<Order> filteredOrders, string outputPath);

        List<Order> GetOrdersByTileLimit(string cityDistrict, TimeSpan limit);
    }
}
