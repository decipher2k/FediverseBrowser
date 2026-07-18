using FediverseHub.Core.Domain;
using FediverseHub.Core.ViewModels;

namespace FediverseHub.Maui;

public partial class MainPage : ContentPage
{
    private readonly TimelineViewModel _viewModel;
    private bool _hasAutoRefreshed;
    private readonly FediverseSourceType?[] _sourceOrder =
    [
        null,
        FediverseSourceType.Mastodon,
        FediverseSourceType.Pixelfed,
        FediverseSourceType.PeerTube,
        FediverseSourceType.Lemmy,
        FediverseSourceType.Rss
    ];

    public MainPage(TimelineViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_hasAutoRefreshed)
        {
            return;
        }

        _hasAutoRefreshed = true;
        await _viewModel.RefreshAsync(CancellationToken.None);
    }

    private async void OnAllClicked(object? sender, EventArgs e) => await SelectSourceAsync(null);

    private async void OnMastodonClicked(object? sender, EventArgs e) =>
        await SelectSourceAsync(FediverseSourceType.Mastodon);

    private async void OnPixelfedClicked(object? sender, EventArgs e) =>
        await SelectSourceAsync(FediverseSourceType.Pixelfed);

    private async void OnPeerTubeClicked(object? sender, EventArgs e) =>
        await SelectSourceAsync(FediverseSourceType.PeerTube);

    private async void OnLemmyClicked(object? sender, EventArgs e) =>
        await SelectSourceAsync(FediverseSourceType.Lemmy);

    private async void OnRssClicked(object? sender, EventArgs e) =>
        await SelectSourceAsync(FediverseSourceType.Rss);

    private async void OnSwipeLeft(object? sender, SwipedEventArgs e) => await MoveSourceAsync(1);

    private async void OnSwipeRight(object? sender, SwipedEventArgs e) => await MoveSourceAsync(-1);

    private async Task MoveSourceAsync(int delta)
    {
        var current = Array.IndexOf(_sourceOrder, _viewModel.SelectedSource);
        var next = Math.Clamp(current + delta, 0, _sourceOrder.Length - 1);
        await SelectSourceAsync(_sourceOrder[next]);
    }

    private async Task SelectSourceAsync(FediverseSourceType? sourceType)
    {
        _viewModel.SelectedSource = sourceType;
        await _viewModel.RefreshAsync(CancellationToken.None);
    }

    private async void OnPostTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Element { BindingContext: TimelineItemViewModel item } ||
            !item.CanOpenOriginal ||
            string.IsNullOrWhiteSpace(item.OpenUrl))
        {
            return;
        }

        await Browser.OpenAsync(item.OpenUrl, BrowserLaunchMode.SystemPreferred);
    }
}
