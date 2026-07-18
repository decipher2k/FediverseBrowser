using Avalonia;
using FediverseHub.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace FediverseHub.Linux;

internal static class Program
{
    public static IServiceProvider Services { get; private set; } = default!;

    [STAThread]
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddFediverseHubInfrastructure();
        services.AddSingleton<MainWindow>();
        Services = services.BuildServiceProvider();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
