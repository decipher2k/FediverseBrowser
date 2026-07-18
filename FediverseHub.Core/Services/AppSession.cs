using FediverseHub.Core.Interfaces;

namespace FediverseHub.Core.Services;

public sealed class AppSession : IAppSession
{
    public bool IsAuthenticated { get; private set; }

    public bool IsReadOnly { get; private set; } = true;

    public void StartAuthenticatedSession()
    {
        IsAuthenticated = true;
        IsReadOnly = false;
    }

    public void StartReadOnlySession()
    {
        IsAuthenticated = false;
        IsReadOnly = true;
    }

    public void SignOut()
    {
        IsAuthenticated = false;
        IsReadOnly = true;
    }
}
