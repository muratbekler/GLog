using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace GLog.Extensions.Logging
{
    public static class LoggingBuilderExtensions
    {
        /// <summary>
        ///     Registers a <see cref="GLoggerProvider" /> with the service collection.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ILoggingBuilder AddGLog(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider, GLoggerProvider>());
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IConfigureOptions<GLoggerOptions>, GLoggerOptionsSetup>());
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<
                    IOptionsChangeTokenSource<GLoggerOptions>,
                    LoggerProviderOptionsChangeTokenSource<GLoggerOptions, GLoggerProvider>>());

            return builder;
        }

        /// <summary>
        ///     Registers a <see cref="GLoggerProvider" /> with the service collection allowing logger options
        ///     to be customised.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static ILoggingBuilder AddGLog(this ILoggingBuilder builder, Action<GLoggerOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddGLog();
            builder.Services.Configure(configure);
            return builder;
        }
    }
}
