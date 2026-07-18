using FediverseHub.Core.Domain;

namespace FediverseHub.Core.Interfaces;

public interface IInstanceConfigProvider
{
    Task<FediverseInstancesConfig> LoadAsync(CancellationToken cancellationToken);
}
