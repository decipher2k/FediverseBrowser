namespace FediverseHub.Core.Domain;

public sealed record PostPublishResult(
    bool IsSuccess,
    string? RemoteId = null,
    string? OriginalUrl = null,
    string? Error = null)
{
    public static PostPublishResult Success(string remoteId, string originalUrl) =>
        new(true, remoteId, originalUrl);

    public static PostPublishResult Failure(string error) => new(false, Error: error);
}
