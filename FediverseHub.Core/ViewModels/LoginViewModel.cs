using FediverseHub.Core.Interfaces;

namespace FediverseHub.Core.ViewModels;

public sealed class LoginViewModel(ILocalizationService localizationService, IAppSession appSession) : ObservableObject
{
    public string Title => localizationService.GetString("login.title");
    public string Subtitle => localizationService.GetString("app.subtitle");
    public string LoginLabel => localizationService.GetString("login.login");
    public string RegisterLabel => localizationService.GetString("login.register");
    public string DemoModeLabel => localizationService.GetString("login.demoMode");
    public string SkipNotice => localizationService.GetString("login.skipNotice");

    public void StartAuthenticatedSession() => appSession.StartAuthenticatedSession();

    public void StartReadOnlySession() => appSession.StartReadOnlySession();

    public void SignOut() => appSession.SignOut();
}
