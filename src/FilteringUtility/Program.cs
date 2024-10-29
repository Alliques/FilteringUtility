using FilteringUtility.Application;
using FilteringUtility.Domain;
using FilteringUtility.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Serilog;

namespace FilteringUtility
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();
            IServiceProvider serviceProvider;

            var rootCommand = new RootCommand();
            
            var firstDeliveryDateTimeStartOption = new Option<DateTime?>("--firstDeliveryDateTimeStart", "Дата доставки (от)");
            firstDeliveryDateTimeStartOption.AddAlias("_firstDeliveryDateTimeStart");

            var firstDeliveryDateTimeEndOption = new Option<DateTime?>("--firstDeliveryDateTimeEnd", "Дата доставки (по)");
            firstDeliveryDateTimeEndOption.AddAlias("_firstDeliveryDateTimeEnd");

            var dataPathOption = new Option<string>("--dataPath", "Файл с данными");
            dataPathOption.AddAlias("_dataPath");

            var cityDistrictOption = new Option<string>("--cityDistrict", "Район");
            cityDistrictOption.AddAlias("_cityDistrict");

            var deliveryLogOption = new Option<string>("--deliveryLog", "Логи");
            deliveryLogOption.AddAlias("_deliveryLog"); 

            var deliveryOrder = new Option<string>("--deliveryOrder", "Выходной файл");
            deliveryOrder.AddAlias("_deliveryOrder");

            rootCommand.AddOption(dataPathOption);
            rootCommand.AddOption(cityDistrictOption);
            rootCommand.AddOption(deliveryLogOption);
            rootCommand.AddOption(firstDeliveryDateTimeStartOption);
            rootCommand.AddOption(firstDeliveryDateTimeEndOption);
            rootCommand.AddOption(deliveryOrder);

            rootCommand.SetHandler(
            async (string dataPath, DateTime? firstDeliveryDateTimeStart, DateTime? firstDeliveryDateTimeEnd, string cityDistrict, string logsPath, string deliveryOrder) =>
            {
                if (!File.Exists(dataPath))
                {
                    Console.WriteLine("Файл с данными не найден");
                    return;
                }

                var deliveryOrderPath = string.IsNullOrEmpty(deliveryOrder) 
                    ? Path.Combine(AppContext.BaseDirectory, "DeliveryOrder.csv")
                    : deliveryOrder;

                ConfigureServices(services, ref logsPath, dataPath);
                serviceProvider = services.BuildServiceProvider();

                var _orderService = serviceProvider.GetRequiredService<IOrderService>();

                Console.WriteLine("Параметры получены:");
                Console.WriteLine($"Район города: {cityDistrict}");
                Console.WriteLine($"Путь к файлу с данными: {dataPath}");
                Console.WriteLine($"Путь к файлу с результатами второй фильтрации: {deliveryOrderPath}");
                Console.WriteLine($"Путь к файлу с логами: {logsPath}\n");

                Log.Information($"Фильтрация заказов для района {cityDistrict} за период времени c {firstDeliveryDateTimeStart} по {firstDeliveryDateTimeEnd}");

                var orders = _orderService.GetOrders(cityDistrict, firstDeliveryDateTimeStart, firstDeliveryDateTimeEnd);
                
                PrintResults(orders);

                // По условию 30 минут
                var timeLimit = TimeSpan.FromMinutes(30);

                Log.Information($"Фильтрация заказов для района {cityDistrict} с первого заказа и последующие {timeLimit.Minutes} минут");

                var ordersByTimeLimit = _orderService.GetOrdersByTileLimit(cityDistrict, timeLimit);

                await _orderService.SaveFilteredOrdersAsync(ordersByTimeLimit, deliveryOrderPath);

                Log.Information($"Результаты фильтрации записаны в файл: {deliveryOrderPath}");
            },
            dataPathOption, firstDeliveryDateTimeStartOption, firstDeliveryDateTimeEndOption, cityDistrictOption, deliveryLogOption, deliveryOrder
        );

            await rootCommand.InvokeAsync(args);
            Console.ReadKey();
        }

        /// <summary>
        /// Вывод результатов в консоль первой выборки, т.к. по условию задачи в выходной файл она не попадает
        /// </summary>
        private static void PrintResults(List<Order> filteredOrders)
        {
            if (filteredOrders.Any())
            {
                const int orderNumberWidth = 15;
                const int weightWidth = 15;
                const int districtWidth = 20;
                const int dateTimeWidth = 20;

                Console.WriteLine(
                    "OrderNumber".PadRight(orderNumberWidth) +
                    "Weight(kg)".PadRight(weightWidth) +
                    "District".PadRight(districtWidth) +
                    "DeliveryDateTime".PadRight(dateTimeWidth)
                );

                Console.WriteLine(new string('-', orderNumberWidth + weightWidth + districtWidth + dateTimeWidth));

                foreach (var order in filteredOrders)
                {
                    Console.WriteLine(
                        $"{order.OrderNumber}".PadRight(orderNumberWidth) +
                        $"{order.Weight:F2}".PadRight(weightWidth) +
                        $"{order.District}".PadRight(districtWidth) +
                        $"{order.DeliveryDateTime:yyyy-MM-dd HH:mm:ss}".PadRight(dateTimeWidth)
                    );
                }
            }
            else
            {
                Console.WriteLine("Нет заказов, соответствующих заданным критериям.");
            }
        }

        private static void ConfigureServices(IServiceCollection services, ref string logsPath, string dataFilePath)
        {
            if (string.IsNullOrEmpty(logsPath) | !Directory.Exists(logsPath))
            {
                logsPath = Path.Combine(AppContext.BaseDirectory, "Logs.txt");
            }
            else
            {
                logsPath = Path.Combine(logsPath, "Logs.txt");
            }

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(logsPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(Log.Logger));

            services.AddTransient<IOrderRepository>(r => new OrderRepository(Log.Logger, dataFilePath))
                    .AddTransient<IOrderService, OrderService>();
        }
    }
}
