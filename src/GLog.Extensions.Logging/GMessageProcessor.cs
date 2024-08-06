using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GLog.Extensions.Logging
{
    public class GMessageProcessor
    {
        private readonly BufferBlock<GMessage> _messageBuffer;

        private Task _processorTask = Task.CompletedTask;

        public GMessageProcessor(IGLogClient gLogClient)
        {
            GLogClient = gLogClient;
            _messageBuffer = new BufferBlock<GMessage>(new DataflowBlockOptions
            {
                BoundedCapacity = 10000
            });
        }

        internal IGLogClient GLogClient { get; set; }

        public void Start()
        {
            _processorTask = Task.Run(StartAsync);
        }

        private async Task StartAsync()
        {
            while (!_messageBuffer.Completion.IsCompleted)
            {
                try
                {
                    var message = await _messageBuffer.ReceiveAsync();
                    await GLogClient.SendMessageAsync(message);
                }
                catch (InvalidOperationException)
                {
                    // The source completed without providing data to receive.
                }
                catch (Exception ex)
                {
                    Debug.Fail("Unhandled exception while sending GLog message.", ex.ToString());
                }
            }
        }

        public void Stop()
        {
            _messageBuffer.Complete();
            _processorTask.Wait();
        }

        public void SendMessage(GMessage message)
        {
            if (!_messageBuffer.Post(message))
            {
                Debug.Fail("Failed to add GLog message to buffer.");
            }
        }
    }
}
