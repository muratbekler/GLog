using Microsoft.Extensions.Logging;

namespace GLog.Extensions.Logging
{
    public class Logger
    {
        private readonly ILoggerFactory _loggerFactory;
        public Logger(GLoggerOptions options)
        {
            LoggerOptions = options;
            _loggerFactory = new LoggerFactory();
            _loggerFactory.AddGLog(LoggerOptions);
        }

        public GLoggerOptions LoggerOptions { get; }

        public ILoggerFactory CreateLoggerFactory(GLoggerOptions options)
        {
            var loggerFactory = LoggerFactory.Create(builder => builder
                .Configure(loggerOptions => loggerOptions
                    .ActivityTrackingOptions = ActivityTrackingOptions.None));

            loggerFactory.AddGLog(options);

            var logger = loggerFactory.CreateLogger<Logger>();
            return loggerFactory;
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }
    }
}
