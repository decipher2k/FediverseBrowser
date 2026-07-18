using FediverseHub.Core.Domain;

namespace FediverseHub.Core.ViewModels;

public sealed class TimelineItemViewModel
{
    public TimelineItemViewModel(UnifiedTimelineItem item)
    {
        Item = item;
    }

    public UnifiedTimelineItem Item { get; }
    public string Source => Item.SourceType.ToString();
    public string Author => string.IsNullOrWhiteSpace(Item.AuthorHandle)
        ? Item.AuthorName
        : $"{Item.AuthorName} ({Item.AuthorHandle})";
    public string Title => Item.Title ?? string.Empty;
    public string Text => Item.Text ?? string.Empty;
    public IReadOnlyList<string> ImageUrls => Item.MediaUrls
        .Where(static url => !string.IsNullOrWhiteSpace(url))
        .Distinct(StringComparer.Ordinal)
        .Take(4)
        .ToArray();
    public string? MediaPreviewUrl => Item.ThumbnailUrl ?? ImageUrls.FirstOrDefault();
    public string? VideoEmbedUrl => Item.VideoUrl;
    public string? OpenUrl => Item.ExternalUrl ?? Item.VideoUrl ?? ImageUrls.FirstOrDefault();
    public string PublishedAt => Item.PublishedAt.ToLocalTime().ToString("g");
    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
    public bool HasText => !string.IsNullOrWhiteSpace(Text);
    public bool HasImages => ImageUrls.Count > 0;
    public bool HasVideo => !string.IsNullOrWhiteSpace(VideoEmbedUrl);
    public bool CanOpenOriginal => !string.IsNullOrWhiteSpace(OpenUrl) && Item.CanOpenOriginal;
}
