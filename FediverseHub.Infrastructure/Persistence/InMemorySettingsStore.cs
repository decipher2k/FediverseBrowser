using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Infrastructure.Persistence;

public sealed class InMemorySettingsStore(AppSettings? initialSettings = null) : ISettingsStore
{
    private AppSettings _settings = initialSettings ?? new AppSettings();

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_settings);

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        _settings = settings;
        return Task.CompletedTask;
    }
}
