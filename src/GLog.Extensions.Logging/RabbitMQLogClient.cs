using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace GLog.Extensions.Logging
{
    public class RabbitMQLogClient : IGLogClient
    {
        private const string LogSecret = "bG9nLmtvZG1hdGlrLmNvbQ==";
        private const string LogSecretKey = "MTIyMDE=";
        private const int MaxChunks = 128;
        private const int MessageHeaderSize = 12;
        private const int MessageIdSize = 8;

        private readonly int _maxMessageBodySize;

        private readonly GLoggerOptions _options;
        private readonly Random _random;

        // RabbitMQ bağlantı nesneleri
        private readonly IConnection? _connection;
        private readonly IModel? _channel;

        public RabbitMQLogClient(GLoggerOptions options)
        {
            _options = options;
            _maxMessageBodySize = options.UdpMaxChunkSize - MessageHeaderSize;

            var factory = new ConnectionFactory() { HostName = _options.Host, Port  = _options.Port };

            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.ExchangeDeclare(exchange: _options.Exchange, type: _options.ExchangeType); // Exchange ve tipini ayarlayın

            }
            catch
            {
            }
            _random = new Random();
        }

        public async Task SendMessageAsync(GMessage message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message.ToJson());

            if (_options.CompressUdp && messageBytes.Length > _options.UdpCompressionThreshold)
            {
                messageBytes = await CompressMessageAsync(messageBytes);
            }

            foreach (var messageChunk in ChunkMessage(messageBytes))
            {
                // RabbitMQ'ya mesaj gönderme
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

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
