using SmsTicket.Services.Ads;
using SmsTicket.Services.AppSettings;
using SmsTicket.Services.Localizer;
using SmsTicket.Services.Settings;
using SmsTicket.ViewModels;

namespace SmsTicket.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    public SettingsViewModel ViewModel { get; } = new(
        new AppSettings(new SettingsService()),
        App.StartupLanguage,
        App.ApplyTheme,
        App.ApplyLanguage,
        AdPrivacyOptions.TryCreate());

    public LocalizationService Localizer { get; } = (LocalizationService)Application.Current.Resources["Localizer"];

    private void OnAboutClick(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(AboutPage));
}
