using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Uno.AdMob;
using Uno.AdMob.Services;

namespace SmsTicket.Services.Ads;

public sealed class AdPrivacyOptions : IAdPrivacyOptions
{
    private readonly IAdConsentService _consent;
    private readonly DispatcherQueue? _dispatcher = DispatcherQueue.GetForCurrentThread();

    private AdPrivacyOptions(IAdConsentService consent)
    {
        _consent = consent;
        _consent.OnConsentInfoUpdated += (_, _) => RaiseChanged();
        _consent.OnConsentFormDismissed += (_, _) => RaiseChanged();
    }

    public event EventHandler? Changed;

    public bool IsRequired => _consent.IsPrivacyOptionsRequired();

    // Null when ads are off or unsupported (desktop, Windows, WASM)
    public static IAdPrivacyOptions? TryCreate()
    {
        if (!App.AreAdsEnabled || !AdMobPlatform.IsSupported)
        {
            return null;
        }

        var consent = AdMobPlatform.Services?.GetService<IAdConsentService>();
        return consent is null ? null : new AdPrivacyOptions(consent);
    }

    public void Show()
    {
        try
        {
            _consent.ShowPrivacyOptionsForm();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Showing the ad privacy options failed: {ex}");
        }
    }

    private void RaiseChanged()
    {
        if (_dispatcher is null || _dispatcher.HasThreadAccess)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            _dispatcher.TryEnqueue(() => Changed?.Invoke(this, EventArgs.Empty));
        }
    }
}
