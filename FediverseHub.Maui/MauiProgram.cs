using FediverseHub.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FediverseHub.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddFediverseHubInfrastructure();
        builder.Services.AddTransient<AppShell>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegistrationPage>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ComposePage>();
        builder.Services.AddTransient<OnboardingPage>();
        builder.Services.AddTransient<SettingsPage>();

        return builder.Build();
    }
}
