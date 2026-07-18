using FediverseHub.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FediverseHub.Maui;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;
    private readonly IServiceProvider _services;

    public LoginPage(LoginViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _services = services;
        BindingContext = viewModel;
    }

    private void OnLoginClicked(object? sender, EventArgs e)
    {
        _viewModel.StartAuthenticatedSession();
        OpenAppShell();
    }

    private void OnRegisterClicked(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Application.Current?.Windows.FirstOrDefault() is { } window)
            {
                window.Page = _services.GetRequiredService<RegistrationPage>();
            }
        });
    }

    private void OnSkipClicked(object? sender, EventArgs e)
    {
        _viewModel.StartReadOnlySession();
        OpenAppShell();
    }

    private void OpenAppShell()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Application.Current?.Windows.FirstOrDefault() is { } window)
            {
                window.Page = _services.GetRequiredService<AppShell>();
            }
        });
    }
}
