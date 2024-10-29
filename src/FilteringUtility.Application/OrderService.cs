using FilteringUtility.Domain;
using Microsoft.Extensions.Logging;


namespace FilteringUtility.Application
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IOrderRepository orderRepository, ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public List<Order> GetOrders(string cityDistrict, DateTime? firstDeliveryDateTimeStart = null, DateTime? firstDeliveryDateTimeEnd = null)
        {
            
            var orders = _orderRepository.GetOrders(cityDistrict, firstDeliveryDateTimeStart, firstDeliveryDateTimeEnd);

            _logger.LogInformation($"Фильтрация завершена. Найдено {orders.Count} заказов.");

            return orders;
        }

        public List<Order> GetOrdersByTileLimit(string cityDistrict, TimeSpan limit)
        {
            var orders = _orderRepository.GetOrdersByPeriod(cityDistrict, limit);

            _logger.LogInformation($"Фильтрация завершена. Найдено {orders.Count} заказов.");

            return orders;
        }

        public async Task SaveFilteredOrdersAsync(IEnumerable<Order> filteredOrders, string outputPath)
        {
            try
            {
                await _orderRepository.SaveOrdersAsync(filteredOrders, outputPath);
                _logger.LogInformation("Заказы успешно сохранены.");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError("Ошибка доступа при попытке сохранить заказы: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Критическая ошибка при сохранении заказов.");
                throw;
            }
        }
    }
}
