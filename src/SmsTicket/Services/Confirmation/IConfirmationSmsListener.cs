namespace SmsTicket.Services.Confirmation;

/// <summary>Waits for the operator's ticket SMS after the user prepared one.</summary>
public interface IConfirmationSmsListener
{
    void Start(Guid ticketId);
}

public static class ConfirmationSmsListeners
{
    // Only Android lets an app see an incoming SMS, and only with the user's consent per message
    public static IConfirmationSmsListener Create() =>
#if __ANDROID__
        SmsTicket.Droid.SmsUserConsentListener.Instance;
#else
        new NoOpConfirmationSmsListener();
#endif
}

public sealed class NoOpConfirmationSmsListener : IConfirmationSmsListener
{
    public void Start(Guid ticketId)
    {
    }
}
