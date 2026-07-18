using FediverseHub.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FediverseHub.Maui;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services, ILocalizationService localizationService)
    {
        InitializeComponent();
        Title = localizationService.GetString("app.title");

        var tabs = new TabBar();
        tabs.Items.Add(CreateTab(localizationService.GetString("nav.timeline"), services.GetRequiredService<MainPage>()));
        tabs.Items.Add(CreateTab(localizationService.GetString("nav.compose"), services.GetRequiredService<ComposePage>()));
        tabs.Items.Add(CreateTab(localizationService.GetString("nav.settings"), services.GetRequiredService<SettingsPage>()));
        Items.Add(tabs);
    }

    private static Tab CreateTab(string title, Page page) =>
        new()
        {
            Title = title,
            Items =
            {
                new ShellContent
                {
                    Title = title,
                    Content = page
                }
            }
        };
}
