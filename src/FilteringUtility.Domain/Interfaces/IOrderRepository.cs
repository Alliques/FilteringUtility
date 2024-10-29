namespace FilteringUtility.Domain
{
    /// <summary>
    /// Репозиторий заказов
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// Вернуть все заказы (здесь вместо dbset)
        /// </summary>
        /// <returns>Список заказов</returns>
        List<Order> GetAllOrders();

        /// <summary>
        /// Сохранить заказы в файл
        /// </summary>
        /// <param name="filteredOrders">Список заказов</param>
        /// <param name="outputPath">Путь к файлу</param>
        Task SaveOrdersAsync(IEnumerable<Order> filteredOrders, string outputPath);

        /// <summary>
        /// Вернуть заказы с фильтрацией по параметрам
        /// </summary>
        /// <param name="cityDistrict">Район доставки</param>
        /// <param name="firstDeliveryDateTimeStart">Стартовая временная точка</param>
        /// <param name="firstDeliveryDateTimeEnd">Конечная временная точка </param>
        /// <returns>Список отфильтрованных заказов</returns>
        List<Order> GetOrders(string cityDistrict, DateTime? firstDeliveryDateTimeStart = null, DateTime? firstDeliveryDateTimeEnd = null);

        /// <summary>
        /// Вернуть заказы для доставки в конкретный район города в ближайшие N - отрезок времени после времени первого заказа
        /// </summary>
        /// <param name="district">Район доставки</param>
        /// <param name="timeSpan">Период времени</param>
        /// <returns>Список отфильтрованных заказов</returns>
        List<Order> GetOrdersByPeriod(string district, TimeSpan timeSpan);
    }
}
