using FediverseHub.Core.Domain;

namespace FediverseHub.Core.Interfaces;

public interface ISettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}
