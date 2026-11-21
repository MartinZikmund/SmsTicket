using Microsoft.UI.Xaml.Automation;
using SmsTicket.Services.ActiveTickets;
using SmsTicket.Services.Confirmation;
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

    private bool _isProcessingSms;

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
        var pageType = args.IsSettingsInvoked
            ? typeof(SettingsPage)
            : args.InvokedItemContainer?.Tag is string tag && _pages.TryGetValue(tag, out var tagged) ? tagged : null;

        if (pageType is not null && ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType, null, args.RecommendedNavigationTransitionInfo);
        }
    }
}
