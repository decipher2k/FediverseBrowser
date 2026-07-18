using FediverseHub.Core.Interfaces;
using FediverseHub.Infrastructure.DependencyInjection;
using FediverseHub.Infrastructure.Mock;
using Microsoft.Extensions.DependencyInjection;

namespace FediverseHub.Tests;

public sealed class InfrastructureRegistrationTests
{
    [Fact]
    public void Timeline_sources_are_mock_clients()
    {
        var services = new ServiceCollection();
        services.AddFediverseHubInfrastructure(
            dataDirectory: Path.Combine(Path.GetTempPath(), "fediversehub-tests", Guid.NewGuid().ToString("N")));

        using var provider = services.BuildServiceProvider();
        var timelineSources = provider.GetRequiredService<IEnumerable<IFediverseSourceClient>>().ToArray();

        Assert.Equal(5, timelineSources.Length);
        Assert.All(timelineSources, source => Assert.IsAssignableFrom<MockSourceClientBase>(source));
    }
}
