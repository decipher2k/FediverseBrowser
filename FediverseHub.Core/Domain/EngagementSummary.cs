namespace FediverseHub.Core.Domain;

public sealed record EngagementSummary(
    int Replies = 0,
    int Likes = 0,
    int BoostsOrShares = 0,
    int Views = 0,
    int Upvotes = 0,
    int Downvotes = 0);
