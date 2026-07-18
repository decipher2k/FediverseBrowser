using System.Collections.ObjectModel;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;
using FediverseHub.Core.Services;

namespace FediverseHub.Core.ViewModels;

public sealed class TimelineViewModel : ObservableObject
{
    private readonly ITimelineAggregator _timelineAggregator;
    private readonly IHashtagFollowService _hashtagFollowService;
    private readonly ILocalizationService _localizationService;
    private readonly HashSet<string> _loadedItemKeys = new(StringComparer.Ordinal);
    private FediverseSourceType? _selectedSource;
    private bool _isBusy;
    private bool _isLoadingMore;
    private bool _hasMoreItems = true;
    private int _pageIndex;
    private string? _errorMessage;

    public TimelineViewModel(
        ITimelineAggregator timelineAggregator,
        IHashtagFollowService hashtagFollowService,
        ILocalizationService localizationService)
    {
        _timelineAggregator = timelineAggregator;
        _hashtagFollowService = hashtagFollowService;
        _localizationService = localizationService;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync, () => HasMoreItems && !IsBusy);
    }

    public ObservableCollection<TimelineItemViewModel> Items { get; } = [];

    public AsyncRelayCommand RefreshCommand { get; }

    public AsyncRelayCommand LoadMoreCommand { get; }

    public FediverseSourceType? SelectedSource
    {
        get => _selectedSource;
        set => SetProperty(ref _selectedSource, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                LoadMoreCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsLoadingMore
    {
        get => _isLoadingMore;
        private set
        {
            if (SetProperty(ref _isLoadingMore, value))
            {
                LoadMoreCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasMoreItems
    {
        get => _hasMoreItems;
        private set
        {
            if (SetProperty(ref _hasMoreItems, value))
            {
                LoadMoreCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string AllTabTitle => _localizationService.GetString("tab.all");
    public string MastodonTabTitle => _localizationService.GetString("tab.mastodon");
    public string PixelfedTabTitle => _localizationService.GetString("tab.pixelfed");
    public string PeerTubeTabTitle => _localizationService.GetString("tab.peertube");
    public string LemmyTabTitle => _localizationService.GetString("tab.lemmy");
    public string RssTabTitle => _localizationService.GetString("tab.rss");
    public string RefreshLabel => _localizationService.GetString("action.refresh");

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        ErrorMessage = null;
        _pageIndex = 0;
        HasMoreItems = true;
        _loadedItemKeys.Clear();

        try
        {
            var request = await CreateRequestAsync(cancellationToken);
            var items = await _timelineAggregator.GetUnifiedTimelineAsync(request, cancellationToken);

            Items.Clear();
            AppendItems(items);
            HasMoreItems = items.Count >= request.Limit;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            ErrorMessage = _localizationService.GetString("error.timelineLoad");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LoadMoreAsync(CancellationToken cancellationToken)
    {
        if (IsBusy || IsLoadingMore || !HasMoreItems)
        {
            return;
        }

        IsLoadingMore = true;
        ErrorMessage = null;

        try
        {
            _pageIndex++;
            var request = await CreateRequestAsync(cancellationToken);
            var items = await _timelineAggregator.GetUnifiedTimelineAsync(request, cancellationToken);
            var added = AppendItems(items);
            HasMoreItems = items.Count >= request.Limit && added > 0;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            _pageIndex = Math.Max(0, _pageIndex - 1);
            ErrorMessage = _localizationService.GetString("error.timelineLoad");
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    private async Task<TimelineRequest> CreateRequestAsync(CancellationToken cancellationToken)
    {
        var followed = await _hashtagFollowService.GetFollowedHashtagsAsync(cancellationToken);

        return new TimelineRequest
        {
            Sources = SelectedSource is null
                ? null
                : new HashSet<FediverseSourceType> { SelectedSource.Value },
            Limit = 20,
            PageToken = _pageIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
            PreferInterestRelevance = followed.Count > 0,
            InterestHashtags = followed
        };
    }

    private int AppendItems(IEnumerable<UnifiedTimelineItem> items)
    {
        var added = 0;
        foreach (var item in items)
        {
            var key = $"{item.SourceType}:{item.Id}";
            if (!_loadedItemKeys.Add(key))
            {
                continue;
            }

            Items.Add(new TimelineItemViewModel(item));
            added++;
        }

        return added;
    }
}
