namespace SmsTicket.Services.Confirmation;

/// <param name="TicketId">Known when the SMS was captured for a specific ticket, null when shared into the app.</param>
public sealed record IncomingConfirmationSms(string Text, Guid? TicketId);

/// <summary>
/// Ticket SMS handed to the app from outside (share target, SMS User Consent). They may arrive before any UI
/// exists, so they wait here until the shell takes them.
/// </summary>
public static class ConfirmationSmsInbox
{
    private static readonly object _lock = new();
    private static readonly Queue<IncomingConfirmationSms> _pending = new();

    /// <summary>May fire on any thread.</summary>
    public static event EventHandler? Received;

    public static void Post(IncomingConfirmationSms sms)
    {
        if (string.IsNullOrWhiteSpace(sms.Text))
        {
            return;
        }

        lock (_lock)
        {
            _pending.Enqueue(sms);
        }

        Received?.Invoke(null, EventArgs.Empty);
    }

    public static bool TryTake(out IncomingConfirmationSms sms)
    {
        lock (_lock)
        {
            return _pending.TryDequeue(out sms!);
        }
    }
}
