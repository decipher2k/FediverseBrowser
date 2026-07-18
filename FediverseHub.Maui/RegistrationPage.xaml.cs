using FediverseHub.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FediverseHub.Maui;

public partial class RegistrationPage : ContentPage
{
    private readonly RegistrationViewModel _viewModel;
    private readonly IServiceProvider _services;

    public RegistrationPage(RegistrationViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _services = services;
        BindingContext = viewModel;
    }

    private void OnBackClicked(object? sender, EventArgs e)
    {
        if (Application.Current?.Windows.FirstOrDefault() is { } window)
        {
            window.Page = _services.GetRequiredService<LoginPage>();
        }
    }

    private void OnContinueClicked(object? sender, EventArgs e)
    {
        _viewModel.ContinueAsAuthenticated();
        if (Application.Current?.Windows.FirstOrDefault() is { } window)
        {
            window.Page = _services.GetRequiredService<OnboardingPage>();
        }
    }
}
