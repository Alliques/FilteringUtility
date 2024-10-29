using FilteringUtility.Domain;
using FilteringUtility.Infrastructure;
using Moq;
using Serilog;
using System.Globalization;

namespace FilteringUtility.Tests
{
    public class OrderRepositoryTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly string _testFilePath;

        public OrderRepositoryTests()
        {
            _mockLogger = new Mock<ILogger>();
            _testFilePath = "test_orders.csv";
        }

        [Fact]
        public void GetAllOrders_ReturnsListOfOrders_WhenFileIsValid()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "Order1,12.5,DistrictA,2023-10-10 15:30:00\nOrder2,10.0,DistrictB,2023-10-11 16:45:00");
            var repository = new OrderRepository(_mockLogger.Object, _testFilePath);

            // Act
            var orders = repository.GetAllOrders();

            // Assert
            Assert.Equal(2, orders.Count);
            Assert.Equal("Order1", orders[0].OrderNumber);
            Assert.Equal(12.5, orders[0].Weight);
            Assert.Equal("DistrictA", orders[0].District);
            Assert.Equal(DateTime.ParseExact("2023-10-10 15:30:00", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), orders[0].DeliveryDateTime);
        }

        [Fact]
        public void GetAllOrders_LogsErrorAndThrowsIOException_WhenFileNotFound()
        {
            // Arrange
            var invalidFilePath = "nonexistent_file.csv";
            var repository = new OrderRepository(_mockLogger.Object, invalidFilePath);

            // Act, Assert
            var exception = Assert.Throws<FileNotFoundException>(() => repository.GetAllOrders());
            _mockLogger.Verify(logger => logger.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void GetOrders_FiltersByCityDistrictAndDateRange()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "Order1,12.5,DistrictA,2023-10-10 15:30:00\nOrder2,10.0,DistrictB,2023-10-11 16:45:00\nOrder3,8.0,DistrictA,2023-10-12 10:15:00");
            var repository = new OrderRepository(_mockLogger.Object, _testFilePath);

            // Act
            var filteredOrders = repository.GetOrders("DistrictA", new DateTime(2023, 10, 10), new DateTime(2023, 10, 11));

            // Assert
            Assert.Single(filteredOrders);
            Assert.Equal("Order1", filteredOrders[0].OrderNumber);
        }

        [Fact]
        public void GetOrdersByPeriod_FiltersOrdersWithinTimeSpan()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "Order1,12.5,DistrictA,2023-10-10 15:30:00\nOrder2,10.0,DistrictA,2023-10-10 16:00:00\nOrder3,8.0,DistrictA,2023-10-10 17:30:00");
            var repository = new OrderRepository(_mockLogger.Object, _testFilePath);

            // Act
            var filteredOrders = repository.GetOrdersByPeriod("DistrictA", TimeSpan.FromHours(1));

            // Assert
            Assert.Equal(2, filteredOrders.Count);
            Assert.Contains(filteredOrders, o => o.OrderNumber == "Order1");
            Assert.Contains(filteredOrders, o => o.OrderNumber == "Order2");
        }

        [Fact]
        public async Task SaveOrdersAsync_WritesOrdersToFile()
        {
            // Arrange
            var outputFilePath = "output_orders.csv";
            var orders = new List<Order>
            {
                new Order { OrderNumber = "Order1", Weight = 12.5, District = "DistrictA", DeliveryDateTime = DateTime.ParseExact("2023-10-10 15:30:00", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) }
            };
            var repository = new OrderRepository(_mockLogger.Object, _testFilePath);

            // Act
            await repository.SaveOrdersAsync(orders, outputFilePath);

            // Assert
            var savedContent = await File.ReadAllLinesAsync(outputFilePath);
            Assert.Single(savedContent);
            Assert.Equal("Order1,12.5,DistrictA,2023-10-10 15:30:00", savedContent[0]);

            // Очистка
            File.Delete(outputFilePath);
        }
    }
}