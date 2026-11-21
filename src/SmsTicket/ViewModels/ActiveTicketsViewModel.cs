using System.Collections.ObjectModel;
using System.Globalization;
using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.ActiveTickets;
using SmsTicket.Services.Confirmation;
using SmsTicket.Services.Localizer;

namespace SmsTicket.ViewModels;

public sealed class ActiveTicketsViewModel : ObservableObject
{
    private readonly ActiveTicketService _service;
    private readonly LocalizationService _localizer;
    private readonly RemainingTimeFormats _formats;

    private IReadOnlyList<PurchaseRecord> _records = [];
    private bool _hasItems;

    public ActiveTicketsViewModel(ActiveTicketService service, LocalizationService localizer)
    {
        _service = service;
        _localizer = localizer;
        _formats = new(localizer.RemainingMinutesFormat, localizer.RemainingHoursFormat, localizer.RemainingDaysFormat);
    }

    /// <summary>Items are kept and updated in place, so open flyouts survive a refresh.</summary>
    public ObservableCollection<ActiveTicketItem> Items { get; } = new();

    public bool HasItems => Items.Count > 0;

    public TimeZoneInfo Zone => _service.Zone;

    public async Task LoadAsync()
    {
        _records = await _service.History.GetAllAsync();
        Refresh();
    }

    /// <summary>Recomputes countdowns from the last loaded history without touching storage.</summary>
    public void Refresh()
    {
        var now = _service.Now;
        var active = ActiveTickets.Select(_records, now, _service.Zone);
        var activeIds = active.Select(t => t.Record.Id).ToHashSet();

        for (var i = Items.Count - 1; i >= 0; i--)
        {
            if (!activeIds.Contains(Items[i].Id))
            {
                Items.RemoveAt(i);
            }
        }

        for (var index = 0; index < active.Count; index++)
        {
            var ticket = active[index];
            var existing = Items.FirstOrDefault(i => i.Id == ticket.Record.Id);
            if (existing is null)
            {
                existing = new(ticket.Record.Id);
                existing.Update(ticket, now, this);
                Items.Insert(index, existing);
                continue;
            }

            if (Items.IndexOf(existing) is var current && current != index)
            {
                Items.Move(current, index);
            }

            existing.Update(ticket, now, this);
        }

        if (_hasItems != HasItems)
        {
            _hasItems = HasItems;
            OnPropertyChanged(nameof(HasItems));
        }
    }

    public Task AdjustStartAsync(ActiveTicketItem item, TimeSpan timeOfDay) =>
        _service.AdjustStartAsync(item.Id, ActiveTickets.ResolveStartTime(item.Ticket.Record.PreparedAt, timeOfDay, _service.Zone));

    public Task<ConfirmationResult> ApplyConfirmationAsync(ActiveTicketItem item, string smsText) =>
        _service.ApplyConfirmationAsync(item.Id, smsText);

    public Task MarkNotSentAsync(ActiveTicketItem item) => _service.MarkNotSentAsync(item.Id);

    public Task DismissAsync(ActiveTicketItem item) => _service.DismissAsync(item.Id);

    internal string FormatRemaining(TimeSpan remaining) =>
        ActiveTicketFormatter.FormatRemaining(remaining, _formats, CultureInfo.CurrentCulture);

    internal string FormatTime(DateTimeOffset time, DateTimeOffset now) =>
        ActiveTicketFormatter.FormatUntil(time, now, _service.Zone, CultureInfo.CurrentCulture);

    internal LocalizationService Localizer => _localizer;
}

public sealed class ActiveTicketItem : ObservableObject
{
    private ActiveTicket _ticket = null!;
    private string _remainingText = "";
    private string _timeText = "";
    private double _progress;
    private string _automationName = "";
    private string _estimateHint = "";

    public ActiveTicketItem(Guid id) => Id = id;

    public Guid Id { get; }

    public ActiveTicket Ticket
    {
        get => _ticket;
        private set
        {
            if (SetProperty(ref _ticket, value))
            {
                OnPropertyChanged(nameof(IsEstimate));
                OnPropertyChanged(nameof(HintText));
                OnPropertyChanged(nameof(ToolTipText));
                OnPropertyChanged(nameof(HasConfirmationSms));
            }
        }
    }

    public string Abbreviation { get; private set; } = "";

    public string CityName { get; private set; } = "";

    public string ValidityText { get; private set; } = "";

    public string OptionsLabel { get; private set; } = "";

    public string EstimateText { get; private set; } = "";

    /// <summary>Explains the estimate, which the card only marks with "≈".</summary>
    public string ToolTipText => IsEstimate ? $"{OptionsLabel}\n{_estimateHint}" : OptionsLabel;

    public bool IsEstimate => _ticket?.IsEstimate ?? false;

    public bool HasConfirmationSms => _ticket?.Record.ConfirmationSms is not null;

    /// <summary>Prague day tickets only start after the user replies to the operator.</summary>
    public string HintText { get; private set; } = "";

    public string RemainingText
    {
        get => _remainingText;
        private set => SetProperty(ref _remainingText, value);
    }

    /// <summary>"do 14:52", or "platí od 4:30" before the validity starts.</summary>
    public string TimeText
    {
        get => _timeText;
        private set => SetProperty(ref _timeText, value);
    }

    /// <summary>Remaining validity, 100 = full.</summary>
    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public string AutomationName
    {
        get => _automationName;
        private set => SetProperty(ref _automationName, value);
    }

    internal void Update(ActiveTicket ticket, DateTimeOffset now, ActiveTicketsViewModel owner)
    {
        var localizer = owner.Localizer;
        var isFirstUpdate = _ticket is null;
        var record = ticket.Record;
        var city = DataSource.Cities.FirstOrDefault(c => c.Id == record.CityId);
        HintText = ticket.RequiresConfirmation && ticket.IsEstimate ? localizer.ActiveTicketAwaitingConfirmation : "";
        Ticket = ticket;

        if (isFirstUpdate)
        {
            Abbreviation = city?.Abbreviation ?? record.CityId;
            CityName = city?.Name ?? record.CityName;
            var ticketType = city?.TicketTypes.FirstOrDefault(t => t.Id == record.TicketId);
            ValidityText = ticketType?.TimeText ?? record.TimeText;
            OptionsLabel = string.Format(CultureInfo.CurrentCulture, localizer.ActiveTicketOptionsFormat, $"{CityName} {ValidityText}");
            EstimateText = localizer.ActiveTicketEstimate;
            _estimateHint = localizer.ActiveTicketEstimateHint;
        }

        var prefix = ticket.IsEstimate ? "≈ " : "";
        RemainingText = prefix + owner.FormatRemaining(ticket.HasStarted(now) ? ticket.Remaining(now) : ticket.ValidUntil - ticket.ValidFrom);
        TimeText = ticket.HasStarted(now)
            ? string.Format(CultureInfo.CurrentCulture, localizer.ActiveTicketUntilFormat, owner.FormatTime(ticket.ValidUntil, now))
            : string.Format(CultureInfo.CurrentCulture, localizer.ActiveTicketStartsFormat, owner.FormatTime(ticket.ValidFrom, now));
        Progress = ticket.RemainingFraction(now) * 100;

        var name = string.Format(CultureInfo.CurrentCulture, localizer.ActiveTicketAutomationFormat, CityName, ValidityText, RemainingText, TimeText);
        AutomationName = ticket.IsEstimate ? $"{name}, {localizer.ActiveTicketEstimatedAutomation}" : name;
    }
}
