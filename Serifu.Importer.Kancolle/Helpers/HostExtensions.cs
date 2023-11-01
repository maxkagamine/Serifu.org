using Microsoft.Extensions.Hosting;
using Serilog;

namespace Microsoft.Extensions.DependencyInjection;
internal static class HostExtensions
{
    /// <summary>
    /// Adds <typeparamref name="T"/> as a scoped service and registers an <see cref="IHostedService"/> which will
    /// execute it using the entrypoint defined by <paramref name="main"/> and exit upon completion.
    /// </summary>
    /// <typeparam name="T">The entrypoint class.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="main">
    /// A function that calls the entrypoint method on <typeparamref name="T"/> and returns a <see cref="Task"/>. The
    /// function is passed a <see cref="CancellationToken"/> that will be triggered if the application is stopped e.g.
    /// due to a Ctrl+C interrupt.
    /// </param>
    public static IServiceCollection AddEntryPoint<T>(
        this IServiceCollection services,
        Func<T, CancellationToken, Task<int>> main) where T : class
    {
        services.AddScoped<T>();
        return services.AddHostedService<DelegateService>(provider => new(async stoppingToken =>
        {
            using var scope = provider.CreateScope();
            var entrypoint = scope.ServiceProvider.GetRequiredService<T>();
            var lifetime = scope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger>();

            try
            {
                Environment.ExitCode = await main(entrypoint, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Microsoft.Extensions.Hosting prints exceptions twice and exits with 0... we'll do it ourselves.
                logger.Fatal(ex, "Unhandled exception.");
                Environment.ExitCode = 255;
                Console.Write("\a"); // Flashes the taskbar if the terminal's not in the foreground
            }

            lifetime.StopApplication();
        }));
    }

    /// <inheritdoc cref="AddEntryPoint{T}(IServiceCollection, Func{T, CancellationToken, Task{Int32}})"/>
    public static IServiceCollection AddEntryPoint<T>(
        this IServiceCollection services,
        Func<T, CancellationToken, Task> main) where T : class
        => services.AddEntryPoint<T>(async (entry, stoppingToken) =>
        {
            await main(entry, stoppingToken);
            return 0;
        });

    private class DelegateService : BackgroundService
    {
        private readonly Func<CancellationToken, Task> executeAsync;

        public DelegateService(Func<CancellationToken, Task> executeAsync)
        {
            this.executeAsync = executeAsync;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => executeAsync(stoppingToken);
    }
}
