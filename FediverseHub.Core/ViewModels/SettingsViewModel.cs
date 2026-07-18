using System.Collections.ObjectModel;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Core.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly ILocalizationService _localizationService;
    private readonly IAppSession _appSession;
    private AppSettings _settings = new();

    public SettingsViewModel(
        ISettingsStore settingsStore,
        ILocalizationService localizationService,
        IAppSession appSession)
    {
        _settingsStore = settingsStore;
        _localizationService = localizationService;
        _appSession = appSession;
        Languages = new ObservableCollection<SupportedLanguage>(localizationService.SupportedLanguages);
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        UseSystemLanguageCommand = new AsyncRelayCommand(UseSystemLanguageAsync);
    }

    public ObservableCollection<SupportedLanguage> Languages { get; }

    public AsyncRelayCommand LoadCommand { get; }
    public AsyncRelayCommand UseSystemLanguageCommand { get; }

    public string Title => _localizationService.GetString("settings.title");
    public string LanguageSectionTitle => _localizationService.GetString("settings.language");
    public string UseSystemLanguageLabel => _localizationService.GetString("settings.useSystemLanguage");
    public string ThemeSectionTitle => _localizationService.GetString("settings.theme");
    public string CacheSectionTitle => _localizationService.GetString("settings.cache");
    public string LogoutLabel => _localizationService.GetString("action.logout");

    public AppSettings Settings
    {
        get => _settings;
        private set => SetProperty(ref _settings, value);
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        Settings = await _settingsStore.LoadAsync(cancellationToken);
    }

    public async Task SetLanguageAsync(string languageCode, CancellationToken cancellationToken)
    {
        await _localizationService.SetLanguageAsync(languageCode, cancellationToken);
        Settings = Settings with { LanguageCode = languageCode, UseSystemLanguage = false };
        await _settingsStore.SaveAsync(Settings, cancellationToken);
    }

    private async Task UseSystemLanguageAsync(CancellationToken cancellationToken)
    {
        await _localizationService.UseSystemLanguageAsync(cancellationToken);
        Settings = Settings with { LanguageCode = null, UseSystemLanguage = true };
        await _settingsStore.SaveAsync(Settings, cancellationToken);
    }

    public void SignOut() => _appSession.SignOut();
}
