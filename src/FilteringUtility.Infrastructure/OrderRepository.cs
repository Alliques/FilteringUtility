using FilteringUtility.Domain;
using Serilog;
using System.Globalization;
using System.Text;


namespace FilteringUtility.Infrastructure
{
    public class OrderRepository : IOrderRepository
    {
        private string _filePath;
        private readonly ILogger _logger;

        public OrderRepository(ILogger logger, string filePath)
        {
            _logger = logger;
            _filePath = filePath;
        }

        public List<Order> GetAllOrders()
        {
            try
            {
                var orders = new List<Order>();

                foreach (var line in File.ReadAllLines(_filePath))
                {
                    var parts = line.Split(',');

                    if (parts.Length < 4) continue;

                    double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var weight);
                    DateTime.TryParseExact(parts[3], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var datetime);

                    orders.Add(new Order
                    {
                        OrderNumber = parts[0],
                        Weight = weight,
                        District = parts[2],
                        DeliveryDateTime = datetime
                    });
                }

                return orders;
            }
            catch (IOException ex)
            {
                _logger.Error("Ошибка чтения данных из файла.", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Неизвестная ошибка при обработке данных из источника.");
                throw;
            }
        }

        public List<Order> GetOrders(string cityDistrict, DateTime? firstDeliveryDateTimeStart = null, DateTime? firstDeliveryDateTimeEnd = null)
        {
            try
            {
                var orders = GetAllOrders();

                if (string.IsNullOrEmpty(cityDistrict) && !firstDeliveryDateTimeStart.HasValue && !firstDeliveryDateTimeEnd.HasValue)
                {
                    _logger.Warning("Нет параметров для фильтрации. Возвращаются все заказы.");
                    return orders;
                }

                return orders
                    .Where(order =>
                        (string.IsNullOrEmpty(cityDistrict) || order.District.Equals(cityDistrict, StringComparison.OrdinalIgnoreCase))
                            && (!firstDeliveryDateTimeStart.HasValue || order.DeliveryDateTime >= firstDeliveryDateTimeStart)
                            && (!firstDeliveryDateTimeEnd.HasValue || order.DeliveryDateTime <= firstDeliveryDateTimeEnd))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при фильтрации заказов.");
                throw;
            }
        }

        public List<Order> GetOrdersByPeriod(string district, TimeSpan timeSpan)
        {
            try
            {
                return GetAllOrders()
                    .Where(order => order.District.Equals(district, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(order => order.District)
                    .SelectMany(g =>
                    {
                        var earliestOrder = g.OrderBy(order => order.DeliveryDateTime).FirstOrDefault();
                        if (earliestOrder == null) return Enumerable.Empty<Order>();

                        // Точность 1 минута
                        DateTime endTime = earliestOrder.DeliveryDateTime.AddMinutes(timeSpan.TotalMinutes);

                        return g.Where(order => order.DeliveryDateTime >= earliestOrder.DeliveryDateTime &&
                                                order.DeliveryDateTime <= endTime);
                    }).ToList();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при фильтрации заказов.");
                throw;
            }
        }

        public async Task SaveOrdersAsync(IEnumerable<Order> filteredOrders, string outputPath)
        {
            var sb = new StringBuilder();
            foreach (var order in filteredOrders)
            {
                sb.AppendLine($"{order.OrderNumber},{order.Weight.ToString(CultureInfo.InvariantCulture)},{order.District},{order.DeliveryDateTime:yyyy-MM-dd HH:mm:ss}");
            }

            await File.WriteAllTextAsync(outputPath, sb.ToString());
        }
    }
}
