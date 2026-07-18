using FediverseHub.Core.Domain;

namespace FediverseHub.Core.Services;

public sealed class InterestCatalog
{
    private static readonly IReadOnlyList<InterestCategory> Items =
    [
        Create("art-culture", "interest.artCulture", "#art", "#kunst", "#culture"),
        Create("gaming", "interest.gaming", "#gaming", "#games", "#videogames"),
        Create("technology", "interest.technology", "#technology", "#tech", "#opensource"),
        Create("programming", "interest.programming", "#programming", "#coding", "#dotnet"),
        Create("science", "interest.science", "#science", "#research", "#space"),
        Create("music", "interest.music", "#music", "#musicians", "#livemusic"),
        Create("movies-tv", "interest.moviesTv", "#movies", "#film", "#tv"),
        Create("books-literature", "interest.booksLiterature", "#books", "#reading", "#literature"),
        Create("politics-society", "interest.politicsSociety", "#politics", "#society", "#democracy"),
        Create("news", "interest.news", "#news", "#journalism", "#breakingnews"),
        Create("sports", "interest.sports", "#sports", "#football", "#running"),
        Create("photography", "interest.photography", "#photography", "#photo", "#streetphotography"),
        Create("travel", "interest.travel", "#travel", "#wanderlust", "#nature"),
        Create("food-cooking", "interest.foodCooking", "#food", "#cooking", "#recipes"),
        Create("sustainability", "interest.sustainability", "#climate", "#sustainability", "#environment"),
        Create("education", "interest.education", "#education", "#learning", "#edtech"),
        Create("health", "interest.health", "#health", "#fitness", "#mentalhealth"),
        Create("design", "interest.design", "#design", "#ux", "#ui"),
        Create("maker-diy", "interest.makerDiy", "#maker", "#diy", "#3dprinting"),
        Create("fediverse-openweb", "interest.fediverseOpenWeb", "#fediverse", "#activitypub", "#openweb")
    ];

    public IReadOnlyList<InterestCategory> GetAll() => Items;

    public IReadOnlyList<string> GetHashtagsForInterests(IEnumerable<string> interestIds)
    {
        var selected = new HashSet<string>(interestIds, StringComparer.OrdinalIgnoreCase);

        return HashtagNormalizer.NormalizeMany(
            Items
                .Where(item => selected.Contains(item.Id))
                .SelectMany(item => item.Hashtags));
    }

    private static InterestCategory Create(string id, string localizationKey, params string[] hashtags) =>
        new()
        {
            Id = id,
            LocalizationKey = localizationKey,
            Hashtags = HashtagNormalizer.NormalizeMany(hashtags)
        };
}
