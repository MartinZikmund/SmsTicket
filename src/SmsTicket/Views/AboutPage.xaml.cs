using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.Localizer;
using Windows.ApplicationModel;

namespace SmsTicket.Views;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
    }

    public LocalizationService Localizer { get; } = (LocalizationService)Application.Current.Resources["Localizer"];

    public string VersionText
    {
        get
        {
            var version = Package.Current.Id.Version;
            return $"{Localizer.Version} {version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public IReadOnlyList<CityPhotoCredit> Credits { get; } = DataSource.Cities
        .Where(c => c.Photo is not null)
        .Select(c => new CityPhotoCredit(c.Name, c.Photo!, ((LocalizationService)Application.Current.Resources["Localizer"]).ViewSource))
        .ToList();
}

public record CityPhotoCredit(string CityName, PhotoCredit Photo, string ViewSourceText)
{
    public Uri LicenseUri => new(Photo.LicenseUrl);

    public Uri SourceUri => new(Photo.SourceUrl);
}
