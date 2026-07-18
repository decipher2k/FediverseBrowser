using FediverseHub.Core.Domain;
using FediverseHub.Core.ViewModels;

namespace FediverseHub.Maui;

public partial class ComposePage : ContentPage
{
    private readonly ComposeViewModel _viewModel;
    private readonly FediverseSourceType[] _sources =
    [
        FediverseSourceType.Mastodon,
        FediverseSourceType.Pixelfed,
        FediverseSourceType.PeerTube,
        FediverseSourceType.Lemmy
    ];

    public ComposePage(ComposeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        SourcePicker.ItemsSource = _sources;
        SourcePicker.SelectedIndex = 0;
    }

    private void OnSourceChanged(object? sender, EventArgs e)
    {
        if (SourcePicker.SelectedIndex >= 0)
        {
            _viewModel.SelectedSource = _sources[SourcePicker.SelectedIndex];
        }
    }
}
