namespace SmsTicket.Services.Ads;

/// <summary>Lets the user change or withdraw their ad consent after the first consent form.</summary>
public interface IAdPrivacyOptions
{
    bool IsRequired { get; }

    event EventHandler? Changed;

    void Show();
}
