
namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly HttpClient _httpClient;

        public Worker(ILogger<Worker> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker starting at {time}", DateTimeOffset.Now);
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker stopping at {time}", DateTimeOffset.Now);
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Small delay so your API (started together) can come up
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var res = await _httpClient.GetAsync("http://localhost:5012", stoppingToken);

                    if (res.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("API is UP. Status: {StatusCode}", (int)res.StatusCode);
                    }
                    else
                    {
                        _logger.LogWarning("API responded but not healthy. Status: {StatusCode}", (int)res.StatusCode);
                    }
                }
                catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning("HTTP request timed out.");
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "API unreachable (network/SSL).");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error while pinging API.");
                }
            }
        }
    }
}