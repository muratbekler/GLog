using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace GLog.Extensions.Logging
{
    internal class GLoggerOptionsSetup : ConfigureFromConfigurationOptions<GLoggerOptions>
    {
        public GLoggerOptionsSetup(ILoggerProviderConfiguration<GLoggerProvider> providerConfiguration)
            : base(providerConfiguration.Configuration)
        {
        }
    }
}
