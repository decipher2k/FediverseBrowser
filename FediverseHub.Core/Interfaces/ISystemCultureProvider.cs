using System.Globalization;

namespace FediverseHub.Core.Interfaces;

public interface ISystemCultureProvider
{
    CultureInfo GetSystemCulture();
}
