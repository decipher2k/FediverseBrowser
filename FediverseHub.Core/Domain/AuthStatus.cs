namespace FediverseHub.Core.Domain;

public sealed record AuthStatus(
    FediverseSourceType SourceType,
    bool IsAuthenticated,
    string? AccountHandle = null,
    string? ReconnectUrl = null,
    string? Message = null)
{
    public static AuthStatus Demo(FediverseSourceType sourceType, string accountHandle) =>
        new(sourceType, true, accountHandle, Message: "Demo mode");

    public static AuthStatus Offline(FediverseSourceType sourceType, string message) =>
        new(sourceType, false, Message: message);
}
