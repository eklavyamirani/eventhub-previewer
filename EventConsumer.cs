using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace eventhub_previewer
{
    public class EventConsumer
    {
        private readonly Channel<string> channel;
        private readonly SemaphoreSlim throttler;
        private readonly TimeSpan cooldownPeriod;
        private readonly int maxDegreeOfParallelism;

        public EventConsumer(Channel<string> channel, ThrottlingOptions throttlingOptions)
        {
            if (channel is null)
            {
                throw new System.ArgumentNullException(nameof(channel));
            }

            if (throttlingOptions is null)
            {
                throw new ArgumentNullException(nameof(throttlingOptions));
            }

            this.channel = channel;
            this.throttler = new SemaphoreSlim((int)throttlingOptions.MaxItems, (int)throttlingOptions.MaxItems);
            this.cooldownPeriod = throttlingOptions.LimitingPeriod;
            this.maxDegreeOfParallelism = (int)throttlingOptions.MaxItems;
        }

        public async Task StartConsumingAsync(CancellationToken cancellationToken)
        {
            await Task.WhenAll(Enumerable.Range(0, this.maxDegreeOfParallelism)
                      .Select((_) => ConsumeAsync(cancellationToken)));
        }

        private async Task ConsumeAsync(CancellationToken cancellationToken)
        {
            while (await this.channel.Reader.WaitToReadAsync(cancellationToken))
            {
                string payload;
                while (!this.channel.Reader.TryRead(out payload))
                {
                    // No data found.
                    // TODO: implement backoff.keep trying
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await ProcessAsync(payload, cancellationToken);
            }
        }

        private async Task ProcessAsync(string payload, CancellationToken cancellationToken)
        {
            await this.throttler.WaitAsync(cancellationToken);
            // Simulate processing.
            Console.WriteLine($"{DateTime.Now} - {payload}");
            await Task.Delay(this.cooldownPeriod, cancellationToken);
            this.throttler.Release();
        }
    }

    public class ThrottlingOptions
    {
        public uint MaxItems { get; set; }
        public TimeSpan LimitingPeriod { get; set; }
    }
}