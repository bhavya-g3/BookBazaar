using WorkerService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System.IO;

var logFile = "logs/worker.log";

// Ensure directory exists
Directory.CreateDirectory("logs");

// Delete old file
if (File.Exists(logFile))
{
    File.Delete(logFile);
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Error)
    .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Error)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/worker.log",
        rollingInterval: RollingInterval.Infinite
    )
    .CreateLogger();

try
{
    Log.Information("Starting Worker host...");

    var builder = Host.CreateApplicationBuilder(args);

    // Enable Serilog for logging
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(dispose: true);

    // Graceful shutdown for log flushing
    builder.Services.Configure<HostOptions>(o =>
    {
        o.ShutdownTimeout = TimeSpan.FromSeconds(10);
    });

    // DI: HttpClient for Worker + Worker itself
    builder.Services.AddHttpClient<Worker>(c =>
    {
        c.Timeout = TimeSpan.FromSeconds(10);
    });

    builder.Services.AddHostedService<Worker>();

    await builder.Build().RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush(); // Ensures logs are written before exit
}