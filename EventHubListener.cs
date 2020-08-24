using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;

namespace eventhub_previewer
{
    public class EventHubListener
    {
        private readonly string connectionString;
        private readonly Channel<string> channel;

        public EventHubListener(string connectionString, Channel<string> channel)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or whitespace", nameof(connectionString));
            }

            if (channel is null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            this.connectionString = connectionString;
            this.channel = channel;
        }

        public async Task StartListeningAsync(int maxBatchSize, TimeSpan waitTimeout, CancellationToken cancellationToken)
        {
            var readOptions = new ReadEventOptions
            {
                MaximumWaitTime = waitTimeout,
            };

            await using var client = new EventHubConsumerClient(consumerGroup: "$default", connectionString: connectionString);
            await foreach (var receivedEvent in client.ReadEventsAsync(startReadingAtEarliestEvent:false, readOptions, cancellationToken))
            {
                if(receivedEvent.Data == null)
                {
                    // timeout. Yield and try again.
                    await Task.Yield();
                    continue;
                }

                await ProcessAsync(Encoding.UTF8.GetString(receivedEvent.Data.Body.ToArray()), cancellationToken);
            }
        }

        private async Task ProcessAsync(string payload, CancellationToken cancellationToken)
        {
            while(!this.channel.Writer.TryWrite(payload))
            {
                await this.channel.Writer.WaitToWriteAsync(cancellationToken);
            }
        }
    }
}