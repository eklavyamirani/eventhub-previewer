using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace eventhub_previewer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var logger = CreateLogger();
            IConfiguration configuration = new ConfigurationBuilder()
                                                .AddJsonFile("appsettings.json")
                                                .AddJsonFile("appsettings.dev.json", optional: true)
                                                .Build();

            var eventHubConnectionString = configuration["eventHubConnectionString"];
            if (string.IsNullOrWhiteSpace(eventHubConnectionString))
            {
                throw new InvalidOperationException("EventHub Connection string is missing. Add a config eventHubConnectionString:<eventHubConnectionString> to appsetings.json");
            }

            // Create a channel to ensure we don't read more than max items.
            var brokerChannel = System.Threading.Channels.Channel.CreateBounded<string>(new BoundedChannelOptions(3)
            {
                FullMode = BoundedChannelFullMode.Wait,
            });

            var cancellationTokenSource = new CancellationTokenSource();

            // Create an eventhub listener
            var listener = new EventHubListener(eventHubConnectionString, brokerChannel);
            var listenerTask = listener.StartListeningAsync(maxBatchSize:3, waitTimeout: TimeSpan.FromSeconds(10), cancellationToken: cancellationTokenSource.Token);
            
            // Now create a brokerChannel reader to consume events
            var eventConsumer = new EventConsumer(brokerChannel, new ThrottlingOptions
            {
                LimitingPeriod = TimeSpan.FromMinutes(1),
                MaxItems = 2,
            });

            var consumingTask = eventConsumer.StartConsumingAsync(cancellationTokenSource.Token);
            Console.ReadLine();

            cancellationTokenSource.Cancel();
            try
            {
                await Task.WhenAll(listenerTask, consumingTask);
            }
            catch (TaskCanceledException)
            {
                // Expected.
            }
        }

        private static ILogger CreateLogger()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                    .AddConsole();
            });
            
            ILogger logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("Logging setup success.");
            return logger;
        }
    }
}
