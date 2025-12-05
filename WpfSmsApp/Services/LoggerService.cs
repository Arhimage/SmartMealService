using Serilog;
using Serilog.Core;

namespace WpfSmsApp.Services
{
    public class LoggerService
    {
        private static readonly Lazy<LoggerService> _instance = new(() => new LoggerService());
        public static LoggerService Instance => _instance.Value;

        private readonly Logger _logger;

        private LoggerService()
        {
            _logger = new LoggerConfiguration()
                .WriteTo.File(
                    path: "logs/test-sms-wpf-app-.log",
                    rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:dd-MM-yyyy HH:mm}] [{Level:u3}] {Message:lj}{NewLine}"
                )
                .CreateLogger();
        }

        public void LogCreateEvent<T>(T item)
        {
            _logger.Information("Был создан объект: {@CreatedItem}", item);
        }

        public void LogUpdateEvent<T>(T oldItem, T newItem)
        {
            _logger.Information("Был изменен объект: {OldItem} → {@NewItem}", oldItem, newItem);
        }

        public void LogRemoveEvent<T>(T item)
        {
            _logger.Information("Был удален объект: {@RemovedItem}", item);
        }

        public void LogErrorEvent<T>(string message, T item) where T : Exception
        {
            _logger.Error("Произошла ошибка: \"{ErrorMessage}\". Вызванное исключение: {@Exception} ", message, item);
        }
    }
}
