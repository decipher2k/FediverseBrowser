using FediverseHub.Core.Domain;

namespace FediverseHub.Core.Services;

public sealed class ComposePostValidator
{
    public PostValidationResult Validate(ComposePostDraft draft, SourceCapabilitySet capabilities)
    {
        var errors = new List<string>();

        if (!capabilities.SupportsPosting)
        {
            errors.Add("error.postingUnsupported");
        }

        if (string.IsNullOrWhiteSpace(draft.Text) && draft.Media.Count == 0)
        {
            errors.Add("error.composeEmpty");
        }

        if (draft.TargetSource == FediverseSourceType.PeerTube && draft.Media.Count == 0)
        {
            errors.Add("error.videoRequired");
        }

        if (draft.TargetSource == FediverseSourceType.Lemmy && string.IsNullOrWhiteSpace(draft.CommunityName))
        {
            errors.Add("error.communityRequired");
        }

        foreach (var media in draft.Media)
        {
            if (media.SizeBytes > capabilities.MaxMediaBytes)
            {
                errors.Add("error.mediaTooLarge");
            }

            if (capabilities.AllowedMediaTypes.Count > 0 &&
                !capabilities.AllowedMediaTypes.Contains(media.ContentType))
            {
                errors.Add("error.mediaTypeUnsupported");
            }
        }

        return errors.Count == 0
            ? PostValidationResult.Success
            : new PostValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
    }
}
