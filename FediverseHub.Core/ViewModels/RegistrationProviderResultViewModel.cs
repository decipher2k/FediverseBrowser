using FediverseHub.Core.Domain;

namespace FediverseHub.Core.ViewModels;

public sealed class RegistrationProviderResultViewModel
{
    public RegistrationProviderResultViewModel(RegistrationResult result)
    {
        SourceType = result.SourceType;
        AccountHandle = result.AccountHandle ?? string.Empty;
        State = result.State.ToString();
        Message = result.Message ?? string.Empty;
        ExternalRegistrationUrl = result.ExternalRegistrationUrl ?? string.Empty;
    }

    public FediverseSourceType SourceType { get; }

    public string Provider => SourceType.ToString();

    public string AccountHandle { get; }

    public string State { get; }

    public string Message { get; }

    public string ExternalRegistrationUrl { get; }
}
