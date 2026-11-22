using Microsoft.UI.Dispatching;
using SmsTicket.Services.ActiveTickets;
using SmsTicket.Services.Localizer;
using SmsTicket.ViewModels;

namespace SmsTicket.Views;

/// <summary>Horizontal strip of tickets that are currently valid, with a live countdown.</summary>
public sealed partial class ActiveTicketsStrip : UserControl
{
    private static readonly LocalizationService _localizer = new();
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(15);

    private readonly ActiveTicketService _service = ActiveTicketService.Default;
    private DispatcherQueueTimer? _timer;

    public ActiveTicketsStrip()
    {
        ViewModel = new ActiveTicketsViewModel(_service, _localizer);
        InitializeComponent();
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ActiveTicketsViewModel.HasItems))
            {
                // Set here rather than x:Bound on the root: Uno stops updating that binding once a cached page is unloaded and reloaded
                Visibility = ViewModel.HasItems ? Visibility.Visible : Visibility.Collapsed;

                // The timer only needs to run while something counts down
                UpdateTimer();
            }
        };
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public ActiveTicketsViewModel ViewModel { get; }

    public LocalizationService Localizer => _localizer;

    public static string PasteConfirmationText => _localizer.PasteConfirmationSms;

    public static string ShowTicketSmsText => _localizer.ShowTicketSms;

    public static string AdjustStartText => _localizer.AdjustStartTime;

    public static string DismissText => _localizer.DismissActiveTicket;

    public static string MarkNotSentText => _localizer.MarkNotSent;

    public static Visibility VisibleIfText(string? text) => string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _service.History.Changed += OnHistoryChanged;
        await ViewModel.LoadAsync();
        UpdateTimer();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _service.History.Changed -= OnHistoryChanged;
        _timer?.Stop();
    }

    private void OnHistoryChanged(object? sender, EventArgs e) =>
        DispatcherQueue.TryEnqueue(async () => await ViewModel.LoadAsync());

    private void UpdateTimer()
    {
        if (!IsLoaded || !ViewModel.HasItems)
        {
            _timer?.Stop();
            return;
        }

        if (_timer is null)
        {
            _timer = DispatcherQueue.CreateTimer();
            _timer.Interval = RefreshInterval;
            _timer.Tick += (_, _) => ViewModel.Refresh();
        }

        if (!_timer.IsRunning)
        {
            _timer.Start();
        }
    }

    private async void OnPasteConfirmationClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: ActiveTicketItem item } ||
            XamlRoot is not { } root ||
            await ConfirmationSmsDialogs.AskForSmsAsync(root, _service) is not { Length: > 0 } text)
        {
            return;
        }

        var result = await ViewModel.ApplyConfirmationAsync(item, text);
        await ConfirmationSmsDialogs.ShowResultAsync(root, result, item.Ticket, announceSuccess: false);
    }

    private async void OnShowTicketSmsClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ActiveTicketItem item } && XamlRoot is { } root)
        {
            await ConfirmationSmsDialogs.ShowStoredSmsAsync(root, item.Ticket, ViewModel.Zone);
        }
    }

    private async void OnAdjustStartClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: ActiveTicketItem item })
        {
            return;
        }

        var start = TimeZoneInfo.ConvertTime(item.Ticket.ValidFrom, ViewModel.Zone);
        TimePicker picker = new()
        {
            ClockIdentifier = "24HourClock",
            MinuteIncrement = 1,
            Time = new TimeSpan(start.Hour, start.Minute, 0),
            Header = _localizer.AdjustStartTime,
        };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(picker, "AdjustStartTimePicker");

        ContentDialog dialog = new()
        {
            Title = _localizer.AdjustStartTimeTitle,
            Content = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = _localizer.AdjustStartTimeText, TextWrapping = TextWrapping.Wrap },
                    picker,
                },
            },
            PrimaryButtonText = _localizer.Save,
            CloseButtonText = _localizer.Cancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.AdjustStartAsync(item, picker.Time);
        }
    }

    private async void OnDismissClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ActiveTicketItem item })
        {
            await ViewModel.DismissAsync(item);
        }
    }

    private async void OnMarkNotSentClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ActiveTicketItem item })
        {
            await ViewModel.MarkNotSentAsync(item);
        }
    }
}
