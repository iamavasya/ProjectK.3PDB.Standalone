using ProjectK._3PDB.Standalone.BL.Interfaces;

namespace ProjectK._3PDB.Standalone.API
{
    /// <summary>
    /// Creates a rotated automatic database snapshot shortly after startup and then
    /// once every 24 hours while the app runs.
    /// </summary>
    public class AutoBackupHostedService : BackgroundService
    {
        private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoBackupHostedService> _logger;

        public AutoBackupHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<AutoBackupHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(InitialDelay, stoppingToken);
                await RunBackupAsync(stoppingToken);

                using var timer = new PeriodicTimer(Interval);
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await RunBackupAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal on shutdown.
            }
        }

        private async Task RunBackupAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
                await backupService.CreateAutoBackupAsync();
                _logger.LogInformation("Automatic database backup created.");
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Automatic database backup failed.");
            }
        }
    }
}
