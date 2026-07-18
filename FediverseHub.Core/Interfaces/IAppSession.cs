namespace FediverseHub.Core.Interfaces;

public interface IAppSession
{
    bool IsAuthenticated { get; }
    bool IsReadOnly { get; }

    void StartAuthenticatedSession();

    void StartReadOnlySession();

    void SignOut();
}
