using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GLog.Extensions.Logging
{
    public class HttpGLogClient : IGLogClient
    {
        private readonly HttpClient _httpClient;

        public HttpGLogClient(GLoggerOptions options)
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = options.Protocol.ToString().ToLower(),
                Host = options.Host,
                Port = options.Port
            };

            _httpClient = new HttpClient
            {
                BaseAddress = uriBuilder.Uri,
                Timeout = options.HttpTimeout
            };

            foreach (var header in options.HttpHeaders)
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        public async Task SendMessageAsync(GMessage message)
        {
            var content = new StringContent(message.ToJson(), Encoding.UTF8, "application/json");
            var result = await _httpClient.PostAsync("GLog", content);
            result.EnsureSuccessStatusCode();
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
