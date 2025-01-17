﻿using System;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GLog.Extensions.Logging.Tests.Fixtures
{
    public class HttpClientWrapper : IDisposable
    {
        private readonly HttpClient _httpClient;

        public HttpClientWrapper(string baseAddress, string username, string password, string clientName)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseAddress),
                DefaultRequestHeaders =
                {
                    Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"))),
                    Accept = {new MediaTypeWithQualityHeaderValue("application/json")}
                }
            };

            _httpClient.DefaultRequestHeaders.Add("X-Requested-By", clientName);
        }

        public async Task<dynamic> GetAsync(string url, CancellationToken cancellation = default)
        {
            var response = await _httpClient.GetAsync(url, cancellation);
            return await DeserialiseResponseAsync(response);
        }

        public async Task<dynamic> PostAsync(object value, string url, CancellationToken cancellation = default)
        {
            var requestJson = JsonConvert.SerializeObject(value);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, requestContent, cancellation);
            return await DeserialiseResponseAsync(response);
        }

        private static async Task<dynamic> DeserialiseResponseAsync(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ExpandoObject>(responseString);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
