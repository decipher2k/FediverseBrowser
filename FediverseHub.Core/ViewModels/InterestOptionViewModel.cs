using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Core.ViewModels;

public sealed class InterestOptionViewModel(
    InterestCategory category,
    ILocalizationService localizationService) : ObservableObject
{
    private bool _isSelected;

    public string Id => category.Id;
    public string DisplayName => localizationService.GetString(category.LocalizationKey);
    public string HashtagPreview => string.Join(" ", category.Hashtags);

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
