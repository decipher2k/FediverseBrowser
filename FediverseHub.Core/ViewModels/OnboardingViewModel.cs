using System.Collections.ObjectModel;
using FediverseHub.Core.Interfaces;
using FediverseHub.Core.Services;

namespace FediverseHub.Core.ViewModels;

public sealed class OnboardingViewModel : ObservableObject
{
    private readonly InterestCatalog _interestCatalog;
    private readonly IHashtagFollowService _hashtagFollowService;
    private readonly ILocalizationService _localizationService;
    private string _customHashtags = string.Empty;
    private string? _validationMessage;

    public OnboardingViewModel(
        InterestCatalog interestCatalog,
        IHashtagFollowService hashtagFollowService,
        ILocalizationService localizationService)
    {
        _interestCatalog = interestCatalog;
        _hashtagFollowService = hashtagFollowService;
        _localizationService = localizationService;
        Interests = new ObservableCollection<InterestOptionViewModel>(
            interestCatalog.GetAll().Select(category => new InterestOptionViewModel(category, localizationService)));
        CompleteCommand = new AsyncRelayCommand(CompleteAsync);
    }

    public ObservableCollection<InterestOptionViewModel> Interests { get; }

    public AsyncRelayCommand CompleteCommand { get; }

    public string Title => _localizationService.GetString("onboarding.title");
    public string Subtitle => _localizationService.GetString("onboarding.subtitle");
    public string CustomHashtagsLabel => _localizationService.GetString("onboarding.customHashtags");
    public string CompleteLabel => _localizationService.GetString("action.complete");

    public string CustomHashtags
    {
        get => _customHashtags;
        set => SetProperty(ref _customHashtags, value);
    }

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public async Task CompleteAsync(CancellationToken cancellationToken)
    {
        var selectedIds = Interests.Where(static item => item.IsSelected).Select(static item => item.Id);
        var hashtags = _interestCatalog.GetHashtagsForInterests(selectedIds)
            .Concat(CustomHashtags.Split([' ', ',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
        var normalized = HashtagNormalizer.NormalizeMany(hashtags);

        if (normalized.Count == 0)
        {
            ValidationMessage = _localizationService.GetString("error.selectInterest");
            return;
        }

        ValidationMessage = null;
        await _hashtagFollowService.FollowHashtagsAsync(normalized, cancellationToken);
    }
}
