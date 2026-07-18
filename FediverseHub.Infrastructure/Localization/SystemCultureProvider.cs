using System.Globalization;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Infrastructure.Localization;

public sealed class SystemCultureProvider : ISystemCultureProvider
{
    public CultureInfo GetSystemCulture() => CultureInfo.CurrentUICulture;
}
