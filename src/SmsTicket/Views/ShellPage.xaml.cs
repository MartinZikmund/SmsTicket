using Microsoft.UI.Xaml.Automation;
using SmsTicket.Services.Localizer;
using Uno.AdMob;

namespace SmsTicket.Views;

public sealed partial class ShellPage : Page
{
    private static readonly Dictionary<string, Type> _pages = new()
    {
        ["Tickets"] = typeof(MainPage),
        ["History"] = typeof(HistoryPage),
        ["About"] = typeof(AboutPage),
    };

    public ShellPage()
    {
        InitializeComponent();
        NavView.SelectedItem = TicketsItem;
        ContentFrame.Navigate(typeof(MainPage));

        // Full window width, so the adaptive banner's size matches the space it gets
        if (App.AreAdsEnabled)
        {
            BannerAd banner = new() { HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetRow(banner, 1);
            ShellRoot.Children.Add(banner);
        }
    }

    public LocalizationService Localizer { get; } = (LocalizationService)Application.Current.Resources["Localizer"];

    private void OnNavViewLoaded(object sender, RoutedEventArgs e)
    {
        // The built-in label is only localized on Windows and ignores our language override
        if (NavView.SettingsItem is NavigationViewItem settingsItem)
        {
            settingsItem.Content = Localizer.Settings;
            AutomationProperties.SetAutomationId(settingsItem, "NavSettings");
        }
    }

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var pageType = args.IsSettingsInvoked
            ? typeof(SettingsPage)
            : args.InvokedItemContainer?.Tag is string tag && _pages.TryGetValue(tag, out var tagged) ? tagged : null;

        if (pageType is not null && ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType, null, args.RecommendedNavigationTransitionInfo);
        }
    }
}
