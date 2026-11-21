namespace SmsTicket.Services.Reminders;

public interface IExpiryReminderScheduler
{
    /// <summary>Makes <paramref name="reminders"/> the only scheduled expiry reminders.</summary>
    Task ReplaceAllAsync(IReadOnlyList<ExpiryReminder> reminders);
}
