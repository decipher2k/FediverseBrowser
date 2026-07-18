namespace FediverseHub.Core.Interfaces;

public interface IMastodonClient : IFediverseSourceClient, IPostPublisher, IHashtagRemoteFollowClient
{
}

public interface IPixelfedClient : IFediverseSourceClient, IPostPublisher, IHashtagRemoteFollowClient
{
}

public interface IPeerTubeClient : IFediverseSourceClient, IPostPublisher
{
}

public interface ILemmyClient : IFediverseSourceClient, IPostPublisher
{
}

public interface IRssSourceClient : IFediverseSourceClient
{
}
