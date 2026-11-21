using System.Diagnostics;
using System.Globalization;
using SmsTicket.Data;
using SmsTicket.Services.ActiveTickets;
using SmsTicket.Services.Localizer;
#if WINDOWS10_0_17763_0_OR_GREATER && !HAS_UNO
using Windows.UI.Notifications;
#endif

namespace SmsTicket.Services.Reminders;

public static class ExpiryReminderSchedulers
{
    // Uno only implements badge notifications, so each platform uses its native API
    public static IExpiryReminderScheduler Create() =>
#if __ANDROID__
        new SmsTicket.Droid.AndroidExpiryReminderScheduler();
#elif __IOS__
        new SmsTicket.iOS.IosExpiryReminderScheduler();
#elif WINDOWS10_0_17763_0_OR_GREATER && !HAS_UNO
        new WindowsExpiryReminderScheduler();
#else
        new NoOpExpiryReminderScheduler();
#endif

    public static (string Title, string Body) CreateTexts(ActiveTicket ticket, DateTimeOffset now)
    {
        LocalizationService localizer = new();
        var culture = CultureInfo.CurrentCulture;
        var record = ticket.Record;
        var city = DataSource.Cities.FirstOrDefault(c => c.Id == record.CityId);
        var validity = city?.TicketTypes.FirstOrDefault(t => t.Id == record.TicketId)?.TimeText ?? record.TimeText;
        var until = ActiveTicketFormatter.FormatUntil(ticket.ValidUntil, now, PragueTime.Zone, culture);
        var body = string.Format(culture, localizer.ExpiryReminderBodyFormat, city?.Name ?? record.CityName, validity, until);
        return (localizer.ExpiryReminderTitle, body);
    }
}

/// <summary>Desktop and WebAssembly have no way to notify once the app is closed.</summary>
public sealed class NoOpExpiryReminderScheduler : IExpiryReminderScheduler
{
    public Task ReplaceAllAsync(IReadOnlyList<ExpiryReminder> reminders) => Task.CompletedTask;
}

#if WINDOWS10_0_17763_0_OR_GREATER && !HAS_UNO
/// <summary>Scheduled toasts. AppNotificationManager cannot schedule, so this uses the WinRT toast API.</summary>
public sealed class WindowsExpiryReminderScheduler : IExpiryReminderScheduler
{
    private const string Group = "expiry";

    public Task ReplaceAllAsync(IReadOnlyList<ExpiryReminder> reminders)
    {
        try
        {
            var notifier = ToastNotificationManager.CreateToastNotifier();
            foreach (var scheduled in notifier.GetScheduledToastNotifications().Where(n => n.Group == Group))
            {
                notifier.RemoveFromSchedule(scheduled);
            }

            foreach (var reminder in reminders.Where(r => r.NotifyAt > DateTimeOffset.Now))
            {
                var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
                var texts = xml.GetElementsByTagName("text");
                texts[0].AppendChild(xml.CreateTextNode(reminder.Title));
                texts[1].AppendChild(xml.CreateTextNode(reminder.Body));
                notifier.AddToSchedule(new ScheduledToastNotification(xml, reminder.NotifyAt)
                {
                    Group = Group,
                    Tag = reminder.Tag[..16],
                });
            }
        }
        catch (Exception ex)
        {
            // Throws without package identity (unpackaged runs)
            Trace.WriteLine($"Scheduled toasts unavailable: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
#endif
