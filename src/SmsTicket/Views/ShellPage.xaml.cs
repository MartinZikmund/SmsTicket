using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using SmsTicket.Services.ActiveTickets;
using SmsTicket.Services.Confirmation;
using SmsTicket.Services.Localizer;
using Uno.AdMob;
using Uno.Toolkit.UI;

namespace SmsTicket.Views;

public sealed partial class ShellPage : Page
{
    private bool _isProcessingSms;

    public ShellPage()
    {
        InitializeComponent();
        ContentFrame.Navigate(typeof(MainPage));

        // Switching between the top and left pane resets the built-in settings label
        NavView.RegisterPropertyChangedCallback(
            NavigationView.PaneDisplayModeProperty,
            (_, _) => DispatcherQueue.TryEnqueue(LocalizeSettingsItem));

        // Full window width, so the adaptive banner's size matches the space it gets
        if (App.AreAdsEnabled)
        {
            AdHost.Child = new BannerAd { HorizontalAlignment = HorizontalAlignment.Center };
            AdHost.Visibility = Visibility.Visible;
        }

        Loaded += (_, _) =>
        {
            ConfirmationSmsInbox.Received += OnConfirmationSmsReceived;
            _ = ProcessConfirmationSmsAsync();
        };
        Unloaded += (_, _) => ConfirmationSmsInbox.Received -= OnConfirmationSmsReceived;
    }

    public LocalizationService Localizer { get; } = (LocalizationService)Application.Current.Resources["Localizer"];

    private void OnNavViewLoaded(object sender, RoutedEventArgs e)
    {
        LocalizeSettingsItem();
        SyncSelection(ContentFrame.CurrentSourcePageType);
    }

    // NavigationView only lays out a display mode change correctly while its pane is visible
    private void OnLayoutStateChanged(object sender, VisualStateChangedEventArgs e)
    {
        if (e.NewState == WideState)
        {
            NavView.IsPaneVisible = true;
            NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;

            // After the pane items are laid out, so the selection indicator lands on the right one
            DispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.Low,
                () => SyncSelection(ContentFrame.CurrentSourcePageType));
        }
        else
        {
            // A selection changed while the pane is hidden leaves the indicator on the old item
            NavView.SelectedItem = null;
            NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
            NavView.IsPaneVisible = false;
        }
    }

    private void LocalizeSettingsItem()
    {
        // The built-in label is only localized on Windows and ignores our language override
        if (NavView.SettingsItem is NavigationViewItem settingsItem)
        {
            settingsItem.Content = Localizer.Settings;
            AutomationProperties.SetAutomationId(settingsItem, "NavSettings");
        }
    }

    private void OnConfirmationSmsReceived(object? sender, EventArgs e) =>
        DispatcherQueue.TryEnqueue(async () => await ProcessConfirmationSmsAsync());

    private async Task ProcessConfirmationSmsAsync()
    {
        if (_isProcessingSms || XamlRoot is null)
        {
            return;
        }

        _isProcessingSms = true;
        try
        {
            var service = ActiveTicketService.Default;
            while (ConfirmationSmsInbox.TryTake(out var sms))
            {
                // Captured for a known ticket (SMS User Consent), or shared in and matched here
                var ticketId = sms.TicketId;
                var isShared = ticketId is null;
                if (ticketId is null)
                {
                    var match = await service.MatchConfirmationAsync(sms.Text);
                    var ticket = match.Ticket ?? (match.Candidates.Count > 0
                        ? await ConfirmationSmsDialogs.ChooseTicketAsync(XamlRoot, match.Candidates, service.Zone)
                        : null);
                    if (ticket is null)
                    {
                        if (match.Candidates.Count == 0)
                        {
                            await ConfirmationSmsDialogs.ShowNoTicketAsync(XamlRoot);
                        }

                        continue;
                    }

                    ticketId = ticket.Record.Id;
                }

                var result = await service.ApplyConfirmationAsync(ticketId.Value, sms.Text);
                var updated = (await service.GetActiveAsync()).FirstOrDefault(t => t.Record.Id == ticketId);
                await ConfirmationSmsDialogs.ShowResultAsync(XamlRoot, result, updated, announceSuccess: isShared);
            }
        }
        finally
        {
            _isProcessingSms = false;
        }
    }

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var tag = args.IsSettingsInvoked ? ShellDestinations.Settings : args.InvokedItemContainer?.Tag as string;
        NavigateTo(tag, args.RecommendedNavigationTransitionInfo);
    }

    private void OnTabSelectionChanged(TabBar sender, TabBarSelectionChangedEventArgs args) =>
        NavigateTo((args.NewItem as FrameworkElement)?.Tag as string, null);

    // Tapping the current tab again returns to its root page, e.g. from About back to Settings
    private void OnTabClick(object sender, RoutedEventArgs e) =>
        NavigateTo((sender as FrameworkElement)?.Tag as string, null);

    private void NavigateTo(string? tag, NavigationTransitionInfo? transition)
    {
        var pageType = ShellDestinations.PageFor(tag);
        if (pageType is not null && ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType, null, transition);
        }
    }

    private void OnContentNavigated(object sender, NavigationEventArgs e) => SyncSelection(e.SourcePageType);

    // The pane and the tabs both follow the page, so they stay in sync across a resize
    private void SyncSelection(Type? pageType)
    {
        var tag = ShellDestinations.TagFor(pageType);
        if (tag is null)
        {
            return;
        }

        object? navItem = tag switch
        {
            ShellDestinations.Tickets => TicketsItem,
            ShellDestinations.History => HistoryItem,
            ShellDestinations.About => AboutItem,
            _ => NavView.SettingsItem,
        };
        if (navItem is not null && NavView.IsPaneVisible)
        {
            NavView.SelectedItem = navItem;
        }

        var tab = ShellDestinations.TabFor(tag);
        BottomTabs.SelectedItem = BottomTabs.Items.OfType<TabBarItem>().FirstOrDefault(t => Equals(t.Tag, tab));
    }
}
