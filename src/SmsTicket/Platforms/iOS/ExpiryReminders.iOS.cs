using SmsTicket.Services.Reminders;
using UserNotifications;

namespace SmsTicket.iOS;

/// <summary>Schedules expiry reminders as local notifications with a time-interval trigger.</summary>
public sealed class IosReminderPermission : IReminderPermission
{
    public async Task<bool> RequestAsync()
    {
        var (granted, _) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound);
        return granted;
    }
}

public sealed class IosExpiryReminderScheduler : IExpiryReminderScheduler
{
    private const string IdentifierPrefix = "expiry-";

    public async Task ReplaceAllAsync(IReadOnlyList<ExpiryReminder> reminders)
    {
        var center = UNUserNotificationCenter.Current;
        var pending = await center.GetPendingNotificationRequestsAsync();
        var stale = pending.Select(r => r.Identifier).Where(id => id.StartsWith(IdentifierPrefix, StringComparison.Ordinal)).ToArray();
        if (stale.Length > 0)
        {
            center.RemovePendingNotificationRequests(stale);
        }

        if (reminders.Count == 0)
        {
            return;
        }

        // Shows the system prompt only the first time; afterwards it just reports the decision
        var (granted, _) = await center.RequestAuthorizationAsync(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound);
        if (!granted)
        {
            return;
        }

        foreach (var reminder in reminders)
        {
            var seconds = (reminder.NotifyAt - DateTimeOffset.UtcNow).TotalSeconds;
            if (seconds < 1)
            {
                continue;
            }

            UNMutableNotificationContent content = new()
            {
                Title = reminder.Title,
                Body = reminder.Body,
                Sound = UNNotificationSound.Default,
            };
            var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(seconds, false);
            await center.AddNotificationRequestAsync(UNNotificationRequest.FromIdentifier(IdentifierPrefix + reminder.Tag, content, trigger));
        }
    }
}
