using FediverseHub.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FediverseHub.Maui;

public partial class OnboardingPage : ContentPage
{
    private readonly OnboardingViewModel _viewModel;
    private readonly IServiceProvider _services;

    public OnboardingPage(OnboardingViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _services = services;
        BindingContext = viewModel;
    }

    private async void OnCompleteClicked(object? sender, EventArgs e)
    {
        await _viewModel.CompleteAsync(CancellationToken.None);
        if (_viewModel.ValidationMessage is not null)
        {
            return;
        }

        if (Application.Current?.Windows.FirstOrDefault() is { } window)
        {
            window.Page = _services.GetRequiredService<AppShell>();
        }
    }
}
