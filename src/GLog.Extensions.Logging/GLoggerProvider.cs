using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLog.Extensions.Logging
{
    [ProviderAlias("GLog")]
    public class GLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly IOptionsMonitor<GLoggerOptions> _options;
        private readonly ConcurrentDictionary<string, GLogger> _loggers;
        private readonly IDisposable _optionsReloadToken;

        private IGLogClient? _GLogClient;
        private GMessageProcessor? _messageProcessor;
        private IExternalScopeProvider? _scopeProvider;

        public GLoggerProvider(IOptionsMonitor<GLoggerOptions> options)
        {
            _options = options;
            _loggers = new ConcurrentDictionary<string, GLogger>();

            LoadLoggerOptions(options.CurrentValue);

            var onOptionsChanged = Debouncer.Debounce<GLoggerOptions>(LoadLoggerOptions, TimeSpan.FromSeconds(1));
            _optionsReloadToken = options.OnChange(onOptionsChanged);
        }

        public ILogger CreateLogger(string name)
        {
            return _loggers.GetOrAdd(name, newName => new GLogger(
                newName, _messageProcessor!, _options.CurrentValue)
            {
                ScopeProvider = _scopeProvider
            });
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
            foreach (var logger in _loggers)
            {
                logger.Value.ScopeProvider = _scopeProvider;
            }
        }

        private void LoadLoggerOptions(GLoggerOptions options)
        {
            if (string.IsNullOrEmpty(options.Host))
            {
                throw new ArgumentException("GLog host is required.", nameof(options));
            }

            if (string.IsNullOrEmpty(options.LogSource))
            {
                throw new ArgumentException("GLog log source is required.", nameof(options));
            }

            var GLogClient = CreateGLogClient(options);

            if (_messageProcessor == null)
            {
                _messageProcessor = new GMessageProcessor(GLogClient);
                _messageProcessor.Start();
            }
            else
            {
                _messageProcessor.GLogClient = GLogClient;
                _GLogClient?.Dispose();
            }

            _GLogClient = GLogClient;

            foreach (var logger in _loggers)
            {
                logger.Value.Options = options;
            }
        }

        private static IGLogClient CreateGLogClient(GLoggerOptions options)
        {
            return options.Protocol switch
            {
                GProtocol.Udp => new UdpGLogClient(options),
                GProtocol.Tcp => new TcpGLogClient(options),
                GProtocol.Http => new HttpGLogClient(options),
                GProtocol.Https => new HttpGLogClient(options),
                _ => throw new ArgumentException("Unknown protocol.", nameof(options))
            };
        }

        public void Dispose()
        {
            _messageProcessor?.Stop();
            _GLogClient?.Dispose();
            _optionsReloadToken.Dispose();
        }
    }
}
