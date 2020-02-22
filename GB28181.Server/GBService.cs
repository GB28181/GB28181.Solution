using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using GB28181.Server.Main;

namespace GB28181.Service
{
    public class GBService : BackgroundService
    {
        private readonly ILogger<GBService> _logger;
        public GBService(ILogger<GBService> logger)
        {
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service is Running");
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("GB28181 MainProc Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }


    }
}
