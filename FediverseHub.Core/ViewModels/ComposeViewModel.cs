using System.Collections.ObjectModel;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;
using FediverseHub.Core.Services;

namespace FediverseHub.Core.ViewModels;

public sealed class ComposeViewModel : ObservableObject
{
    private readonly IReadOnlyDictionary<FediverseSourceType, IPostPublisher> _publishers;
    private readonly IReadOnlyDictionary<FediverseSourceType, IFediverseSourceClient> _sourceClients;
    private readonly ComposePostValidator _validator;
    private readonly ILocalizationService _localizationService;
    private readonly IAppSession _appSession;
    private FediverseSourceType _selectedSource = FediverseSourceType.Mastodon;
    private string _title = string.Empty;
    private string _text = string.Empty;
    private string _communityName = string.Empty;
    private string? _statusMessage;

    public ComposeViewModel(
        IEnumerable<IPostPublisher> publishers,
        IEnumerable<IFediverseSourceClient> sourceClients,
        ComposePostValidator validator,
        ILocalizationService localizationService,
        IAppSession appSession)
    {
        _publishers = publishers.ToDictionary(static publisher => publisher.SourceType);
        _sourceClients = sourceClients.ToDictionary(static client => client.SourceType);
        _validator = validator;
        _localizationService = localizationService;
        _appSession = appSession;
        PublishCommand = new AsyncRelayCommand(PublishAsync);
    }

    public ObservableCollection<MediaAttachmentDraft> Media { get; } = [];

    public AsyncRelayCommand PublishCommand { get; }

    public string TitleLabel => _localizationService.GetString("compose.title");
    public string BodyLabel => _localizationService.GetString("compose.body");
    public string PublishLabel => _localizationService.GetString("action.publish");
    public string CommunityLabel => _localizationService.GetString("compose.community");
    public string SourceLabel => _localizationService.GetString("compose.source");
    public string MediaLabel => _localizationService.GetString("compose.media");
    public string ReadOnlyNotice => _localizationService.GetString("compose.readOnlyNotice");

    public FediverseSourceType SelectedSource
    {
        get => _selectedSource;
        set => SetProperty(ref _selectedSource, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public string CommunityName
    {
        get => _communityName;
        set => SetProperty(ref _communityName, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public async Task PublishAsync(CancellationToken cancellationToken)
    {
        if (_appSession.IsReadOnly)
        {
            StatusMessage = _localizationService.GetString("error.readOnlyMode");
            return;
        }

        if (!_publishers.TryGetValue(SelectedSource, out var publisher) ||
            !_sourceClients.TryGetValue(SelectedSource, out var client))
        {
            StatusMessage = _localizationService.GetString("error.publisherMissing");
            return;
        }

        var draft = new ComposePostDraft
        {
            TargetSource = SelectedSource,
            Title = Title,
            Text = Text,
            CommunityName = CommunityName,
            Media = Media.ToArray(),
            Tags = HashtagNormalizer.NormalizeMany(Text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        };

        var capabilities = await client.GetCapabilitiesAsync(cancellationToken);
        var localValidation = _validator.Validate(draft, capabilities);
        var remoteValidation = await publisher.ValidateAsync(draft, cancellationToken);

        if (!localValidation.IsValid || !remoteValidation.IsValid)
        {
            var errorKey = localValidation.Errors.Concat(remoteValidation.Errors).FirstOrDefault()
                ?? "error.validationFailed";
            StatusMessage = _localizationService.GetString(errorKey);
            return;
        }

        var result = await publisher.PublishAsync(draft, cancellationToken);
        StatusMessage = result.IsSuccess
            ? _localizationService.GetString("compose.published")
            : result.Error ?? _localizationService.GetString("error.publishFailed");
    }
}
