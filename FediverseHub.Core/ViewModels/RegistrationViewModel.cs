using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Core.ViewModels;

public sealed partial class RegistrationViewModel : ObservableObject
{
    private readonly IReadOnlyDictionary<FediverseSourceType, IRegistrationClient> _registrationClients;
    private readonly IInstanceConfigProvider _instanceConfigProvider;
    private readonly ILocalizationService _localizationService;
    private readonly IAppSession _appSession;
    private string _accountName = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _peerAccountName = string.Empty;
    private string? _message;
    private bool _isBusy;
    private bool _canContinue;

    public RegistrationViewModel(
        IEnumerable<IRegistrationClient> registrationClients,
        IInstanceConfigProvider instanceConfigProvider,
        ILocalizationService localizationService,
        IAppSession appSession)
    {
        _registrationClients = registrationClients.ToDictionary(static client => client.SourceType);
        _instanceConfigProvider = instanceConfigProvider;
        _localizationService = localizationService;
        _appSession = appSession;
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, () => !IsBusy);
    }

    public ObservableCollection<RegistrationProviderResultViewModel> Results { get; } = [];

    public AsyncRelayCommand RegisterCommand { get; }

    public string Title => _localizationService.GetString("register.title");
    public string AccountNameLabel => _localizationService.GetString("register.accountName");
    public string EmailLabel => _localizationService.GetString("register.email");
    public string PasswordLabel => _localizationService.GetString("register.password");
    public string ConfirmPasswordLabel => _localizationService.GetString("register.confirmPassword");
    public string PeerAccountNameLabel => _localizationService.GetString("register.peerAccountName");
    public string RegisterLabel => _localizationService.GetString("login.register");
    public string BackLabel => _localizationService.GetString("action.back");
    public string ContinueLabel => _localizationService.GetString("action.continue");
    public string Explanation => _localizationService.GetString("register.explanation");

    public string AccountName
    {
        get => _accountName;
        set
        {
            if (SetProperty(ref _accountName, value))
            {
                PeerAccountName = BuildPeerAccountName(value);
            }
        }
    }

    public string PeerAccountName
    {
        get => _peerAccountName;
        private set => SetProperty(ref _peerAccountName, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public string? Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RegisterCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanContinue
    {
        get => _canContinue;
        private set => SetProperty(ref _canContinue, value);
    }

    public void ContinueAsAuthenticated() => _appSession.StartAuthenticatedSession();

    public async Task RegisterAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        CanContinue = false;
        Results.Clear();
        Message = null;

        try
        {
            var peerName = BuildPeerAccountName(AccountName);
            if (peerName.Length == 0)
            {
                Message = _localizationService.GetString("error.invalidAccountName");
                return;
            }

            if (!IsValidEmail(Email))
            {
                Message = _localizationService.GetString("error.invalidEmail");
                return;
            }

            if (Password.Length < 12)
            {
                Message = _localizationService.GetString("error.passwordTooShort");
                return;
            }

            if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
            {
                Message = _localizationService.GetString("error.passwordMismatch");
                return;
            }

            var config = await _instanceConfigProvider.LoadAsync(cancellationToken);
            var providers = GetConfiguredProviders(config).ToArray();

            if (providers.Length == 0)
            {
                Message = _localizationService.GetString("error.noProvidersConfigured");
                return;
            }

            foreach (var provider in providers)
            {
                if (!_registrationClients.TryGetValue(provider.SourceType, out var client))
                {
                    Results.Add(new RegistrationProviderResultViewModel(
                        new RegistrationResult(
                            provider.SourceType,
                            RegistrationState.Failed,
                            Message: _localizationService.GetString("error.registrationClientMissing"))));
                    continue;
                }

                var host = new Uri(provider.BaseUrl).Host;
                var result = await client.TryRegisterAsync(
                    new RegistrationAttempt(
                        provider.SourceType,
                        peerName,
                        Email.Trim(),
                        Password),
                    cancellationToken);

                Results.Add(new RegistrationProviderResultViewModel(result));
            }

            CanContinue = Results.Any(static result => result.State == RegistrationState.Created.ToString());
            Message = CanContinue
                ? _localizationService.GetString("register.created")
                : _localizationService.GetString("register.externalFallback");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            Message = _localizationService.GetString("error.registrationFailed");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public static string BuildPeerAccountName(string accountName)
    {
        var normalized = accountName.Trim().TrimStart('@').ToLowerInvariant();
        if (normalized.EndsWith(".peer", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^5];
        }

        if (!AccountNameRegex().IsMatch(normalized))
        {
            return string.Empty;
        }

        return $"{normalized}.peer";
    }

    private static IEnumerable<InstanceConfig> GetConfiguredProviders(FediverseInstancesConfig config)
    {
        InstanceConfig[] providers = [config.Mastodon, config.Pixelfed, config.PeerTube, config.Lemmy];
        return providers.Where(static provider => !string.IsNullOrWhiteSpace(provider.BaseUrl));
    }

    private static bool IsValidEmail(string email)
    {
        var trimmed = email.Trim();
        return trimmed.Length <= 254 && EmailRegex().IsMatch(trimmed);
    }

    [GeneratedRegex("^[a-z0-9_][a-z0-9_-]{0,30}$", RegexOptions.CultureInvariant)]
    private static partial Regex AccountNameRegex();

    [GeneratedRegex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
