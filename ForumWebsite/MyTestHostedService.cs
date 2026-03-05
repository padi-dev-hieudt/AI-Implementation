
using ForumWebsite.Data.Context;

namespace ForumWebsite
{
    /// <summary>
    /// Temporary development background service — logs a heartbeat every 5 seconds.
    /// NOTE: Do NOT inject scoped services (e.g. ApplicationDbContext) here — IHostedService
    /// is registered as Singleton, which would create a captive dependency violation.
    /// Use IServiceScopeFactory if you ever need to resolve scoped services from a background task.
    /// </summary>
    public class MyTestHostedService : IHostedService
    {
        private readonly ILogger<MyTestHostedService> _logger;

        public MyTestHostedService(ILogger<MyTestHostedService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;

            using (var scope = scopeFactory.CreateScope())
            using (var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var data = string.Join("-> \n", db.Posts.Select(x => x.Title).ToList());
                _logger.LogInformation(data);
            }   
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("MyTestHostedService is running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("MyTestHostedService start was cancelled.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MyTestHostedService is stopped.");
            return Task.CompletedTask;
        }
    }
}
