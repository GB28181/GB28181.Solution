using System;
using System.Threading;
using System.Threading.Tasks;
using GB28181.Server.Main;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GB28181.Service
{
    public class GBService : BackgroundService
    {

        private readonly CancellationTokenSource _processToken = new CancellationTokenSource();

        private readonly ILogger<GBService> _logger;
        private readonly IMainProcess _process;

        public GBService(ILogger<GBService> logger, IMainProcess process)
        {
            _logger = logger;
            _process = process;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _process.Stop();
            _processToken.Cancel();
            return base.StopAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GB28181 Main Service is Running");
            if (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("GB28181 MainProc Worker running at: {time}", DateTimeOffset.Now);

                await Task.Run(() => _process.Run(), _processToken.Token);
            }

            await Task.CompletedTask;
        }


    }
}
