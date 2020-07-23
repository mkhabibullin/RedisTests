using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisFTS.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisFTS
{
    internal class App : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly RedisClient redisClient;
        private Timer _timer;

        public App(ILogger<App> logger, RedisClient redisClient)
        {
            _logger = logger;
            this.redisClient = redisClient;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting");

            await redisClient.Connect();

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
              TimeSpan.FromSeconds(5));
        }

        private void DoWork(object state)
        {
            _logger.LogInformation($"Background work with text: 1");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping.");

            _timer?.Change(Timeout.Infinite, 0);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
