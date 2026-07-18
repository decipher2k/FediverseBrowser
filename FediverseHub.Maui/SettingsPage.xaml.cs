using FediverseHub.Core.Domain;
using FediverseHub.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FediverseHub.Maui;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;
    private readonly IServiceProvider _services;

    public SettingsPage(SettingsViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _services = services;
        BindingContext = viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.LoadAsync(CancellationToken.None);
    }

    private async void OnLanguageSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is SupportedLanguage language)
        {
            await _viewModel.SetLanguageAsync(language.Code, CancellationToken.None);
        }
    }

    private void OnUseSystemLanguageToggled(object? sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            _viewModel.UseSystemLanguageCommand.Execute(null);
        }
    }

    private void OnLogoutClicked(object? sender, EventArgs e)
    {
        _viewModel.SignOut();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Application.Current?.Windows.FirstOrDefault() is { } window)
            {
                window.Page = _services.GetRequiredService<LoginPage>();
            }
        });
    }
}
