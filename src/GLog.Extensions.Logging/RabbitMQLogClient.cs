using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GLog.Extensions.Logging
{
    public class RabbitMQLogClient : IGLogClient
    {
        private const int MaxChunks = 128;
        private const int MessageHeaderSize = 12;
        private const int MessageIdSize = 8;

        private readonly int _maxMessageBodySize;

        private readonly GLoggerOptions _options;
        private readonly Random _random;

        // RabbitMQ bağlantı nesneleri
        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMQLogClient(GLoggerOptions options)
        {
            _options = options;
            _maxMessageBodySize = options.UdpMaxChunkSize - MessageHeaderSize;
            _random = new Random();
            _ = Init();
        }

        private async Task<bool> Init()
        {
            try
            {
                var factory = new ConnectionFactory() { HostName = _options.Host, Port = _options.Port };
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.ExchangeDeclare(exchange: _options.Exchange, type: _options.ExchangeType); // Exchange ve tipini ayarlayın

                await TempMessageSendAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task SendMessageAsync(GMessage message)
        {

            if (_channel == null || _channel?.IsClosed == true)
            {
                if (!File.Exists(_options.LogPathSource))
                {
                    File.Create(_options.LogPathSource).Dispose();
                }
                File.AppendAllText(_options.LogPathSource, message.ToJson() + Environment.NewLine);

                await Init();
                return;
            }
            else
            {
                await TempMessageSendAsync();
                await SendRabbitMQMessageAsync(message);
            }
        }

        private async Task SendRabbitMQMessageAsync(GMessage message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message.ToJson());

            if (_options.CompressUdp && messageBytes.Length > _options.UdpCompressionThreshold)
            {
                messageBytes = await CompressMessageAsync(messageBytes);
            }

            foreach (var messageChunk in ChunkMessage(messageBytes))
            {
                _channel.BasicPublish(exchange: _options.Exchange,
                                      routingKey: _options.RoutingKey,
                                      basicProperties: null,
                                      body: messageChunk);
            }
        }

        private static async Task<byte[]> CompressMessageAsync(byte[] messageBytes)
        {
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
            {
                await gzipStream.WriteAsync(messageBytes, 0, messageBytes.Length);
            }
            return outputStream.ToArray();
        }

        private IEnumerable<byte[]> ChunkMessage(byte[] messageBytes)
        {
            if (messageBytes.Length < _options.UdpMaxChunkSize)
            {
                yield return messageBytes;
                yield break;
            }

            var sequenceCount = (int)Math.Ceiling(messageBytes.Length / (double)_maxMessageBodySize);
            if (sequenceCount > MaxChunks)
            {
                Debug.Fail($"GLog message contains {sequenceCount} chunks, exceeding the maximum of {MaxChunks}.");
                yield break;
            }

            var messageId = GetMessageId();
            for (var sequenceNumber = 0; sequenceNumber < sequenceCount; sequenceNumber++)
            {
                var messageHeader = GetMessageHeader(sequenceNumber, sequenceCount, messageId);
                var chunkStartIndex = sequenceNumber * _maxMessageBodySize;
                var messageBodySize = Math.Min(messageBytes.Length - chunkStartIndex, _maxMessageBodySize);
                var chunk = new byte[messageBodySize + MessageHeaderSize];

                Array.Copy(messageHeader, chunk, MessageHeaderSize);
                Array.ConstrainedCopy(messageBytes, chunkStartIndex, chunk, MessageHeaderSize, messageBodySize);

                yield return chunk;
            }
        }

        private byte[] GetMessageId()
        {
            var messageId = new byte[8];
            _random.NextBytes(messageId);
            return messageId;
        }

        private static byte[] GetMessageHeader(int sequenceNumber, int sequenceCount, byte[] messageId)
        {
            var header = new byte[MessageHeaderSize];
            header[0] = 0x1e;
            header[1] = 0x0f;

            Array.ConstrainedCopy(messageId, 0, header, 2, MessageIdSize);

            header[10] = Convert.ToByte(sequenceNumber);
            header[11] = Convert.ToByte(sequenceCount);

            return header;
        }

        private async Task TempMessageSendAsync()
        {
            List<GMessage> logs = ReadLogs();
            for (int i = logs.Count - 1; i >= 0; i--)
            {
                var log = logs[i];
                await SendRabbitMQMessageAsync(log);
                DeleteLog(i);
            }
        }

        public List<GMessage> ReadLogs()
        {
            var logs = new List<GMessage>();

            if (File.Exists(_options.LogPathSource))
            {
                var lines = File.ReadAllLines(_options.LogPathSource);
                foreach (var line in lines)
                {
                    var logEntry = JsonSerializer.Deserialize<GMessage>(line);
                    logs.Add(logEntry);
                }
            }
            return logs;
        }

        public void DeleteLog(int index)
        {
            var logs = ReadLogs();

            if (index >= 0 && index < logs.Count)
            {
                logs.RemoveAt(index);
                File.WriteAllText(_options.LogPathSource, "");

                foreach (var log in logs)
                {
                    string jsonLog = JsonSerializer.Serialize(log);
                    File.AppendAllText(_options.LogPathSource, jsonLog + Environment.NewLine);
                }
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
