using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLog.Extensions.Logging
{
    public static class LoggerFactoryExtensions
    {
        /// <summary>
        ///     Adds a <see cref="GLoggerProvider" /> to the logger factory with the supplied
        ///     <see cref="GLoggerOptions" />.
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ILoggerFactory AddGLog(
            this ILoggerFactory loggerFactory, IOptionsMonitor<GLoggerOptions> options)
        {
            loggerFactory.AddProvider(new GLoggerProvider(options));
            return loggerFactory;
        }

        /// <summary>
        ///     Adds a <see cref="GLoggerProvider" /> to the logger factory with the supplied
        ///     <see cref="GLoggerOptions" />.
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ILoggerFactory AddGLog(this ILoggerFactory loggerFactory, GLoggerOptions options)
        {
            return loggerFactory.AddGLog(new OptionsMonitorStub<GLoggerOptions>(options));
        }

        private class OptionsMonitorStub<T> : IOptionsMonitor<T>
        {
            public OptionsMonitorStub(T options)
            {
                CurrentValue = options;
            }

            public T CurrentValue { get; }

            public T Get(string name) => CurrentValue;

            public IDisposable OnChange(Action<T, string> listener) => new NullDisposable();

            private class NullDisposable : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}
