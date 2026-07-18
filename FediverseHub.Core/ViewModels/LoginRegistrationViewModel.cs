using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Core.ViewModels;

public sealed class LoginRegistrationViewModel : ObservableObject
{
    private readonly IReadOnlyList<IRegistrationClient> _registrationClients;
    private readonly ILocalizationService _localizationService;
    private string _userName = string.Empty;
    private string _email = string.Empty;
    private string? _message;

    public LoginRegistrationViewModel(
        IEnumerable<IRegistrationClient> registrationClients,
        ILocalizationService localizationService)
    {
        _registrationClients = registrationClients.ToArray();
        _localizationService = localizationService;
        RegisterCommand = new AsyncRelayCommand(RegisterAsync);
    }

    public AsyncRelayCommand RegisterCommand { get; }

    public string Title => _localizationService.GetString("login.title");
    public string LoginLabel => _localizationService.GetString("login.login");
    public string RegisterLabel => _localizationService.GetString("login.register");
    public string DemoModeLabel => _localizationService.GetString("login.demoMode");

    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string? Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    private async Task RegisterAsync(CancellationToken cancellationToken)
    {
        var attempts = _registrationClients.Select(client =>
            client.TryRegisterAsync(
                new RegistrationAttempt(client.SourceType, UserName, Email, Password: null),
                cancellationToken));

        var results = await Task.WhenAll(attempts);
        var needsExternal = results.Where(static result => result.State != RegistrationState.Created).ToArray();
        Message = needsExternal.Length == 0
            ? _localizationService.GetString("register.created")
            : _localizationService.GetString("register.externalFallback");
    }
}
