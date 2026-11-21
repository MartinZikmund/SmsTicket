using Trace = System.Diagnostics.Trace;
using Android.App;
using Android.Content;
using Android.Gms.Auth.Api.Phone;
using Android.Gms.Common.Apis;
using SmsTicket.Services.Confirmation;

namespace SmsTicket.Droid;

/// <summary>
/// SMS User Consent API: for 5 minutes, the next SMS that looks like a one-time code makes Google Play services
/// ask the user whether SmsTicket may read that single message. No SMS permission needed.
/// </summary>
public sealed class SmsUserConsentListener : IConfirmationSmsListener
{
    internal const int ConsentRequestCode = 4712;

    private ConsentReceiver? _receiver;
    private Guid? _ticketId;
    private Intent? _pendingConsent;
    private bool _isActivityResumed = true;

    private SmsUserConsentListener()
    {
    }

    public static SmsUserConsentListener Instance { get; } = new();

    public void Start(Guid ticketId)
    {
        if (Uno.UI.ContextHelper.Current is not Activity activity)
        {
            return;
        }

        try
        {
            _ticketId = ticketId;
            _pendingConsent = null;
            Register(activity);

            // The sender is not passed: confirmations may come from a different number than the one the order went to
            SmsRetriever.GetClient(activity).StartSmsUserConsent(null);
        }
        catch (Exception ex)
        {
            // Missing or outdated Google Play services; pasting the SMS still works
            Trace.WriteLine($"SMS User Consent unavailable: {ex.Message}");
            Unregister(activity);
        }
    }

    internal void OnActivityResumed(Activity activity)
    {
        _isActivityResumed = true;
        if (_pendingConsent is { } consent)
        {
            _pendingConsent = null;
            activity.StartActivityForResult(consent, ConsentRequestCode);
        }
    }

    internal void OnActivityPaused() => _isActivityResumed = false;

    /// <summary>Called by <see cref="MainActivity"/> once the user answered the consent prompt.</summary>
    internal void OnConsentResult(Result resultCode, Intent? data)
    {
        var ticketId = _ticketId;
        _ticketId = null;
        if (resultCode == Result.Ok && data?.GetStringExtra(SmsRetriever.ExtraSmsMessage) is { Length: > 0 } message)
        {
            ConfirmationSmsInbox.Post(new(message, ticketId));
        }
    }

    private void Register(Activity activity)
    {
        if (_receiver is not null)
        {
            return;
        }

        _receiver = new ConsentReceiver(this, activity);
        IntentFilter filter = new(SmsRetriever.SmsRetrievedAction);
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            // Exported so Play services can reach it; the permission keeps everyone else out
            activity.RegisterReceiver(_receiver, filter, SmsRetriever.SendPermission, null, ReceiverFlags.Exported);
        }
        else
        {
            activity.RegisterReceiver(_receiver, filter, SmsRetriever.SendPermission, null);
        }
    }

    private void Unregister(Context context)
    {
        if (_receiver is null)
        {
            return;
        }

        try
        {
            context.UnregisterReceiver(_receiver);
        }
        catch (Java.Lang.IllegalArgumentException)
        {
            // Already gone with the activity
        }

        _receiver = null;
    }

    private sealed class ConsentReceiver(SmsUserConsentListener owner, Activity activity) : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action != SmsRetriever.SmsRetrievedAction || intent.Extras is not { } extras)
            {
                return;
            }

            owner.Unregister(activity);
            var status = extras.Get(SmsRetriever.ExtraStatus) as Statuses;
            if (status?.StatusCode != CommonStatusCodes.Success)
            {
                // Timeout: no matching SMS within 5 minutes
                return;
            }

#pragma warning disable CA1422 // The typed overload needs API 33
            var consentIntent = extras.GetParcelable(SmsRetriever.ExtraConsentIntent) as Intent;
#pragma warning restore CA1422

            // Only launch what Play services handed us, never an arbitrary component
            if (consentIntent?.ResolveActivity(activity.PackageManager!) is not { PackageName: "com.google.android.gms" })
            {
                return;
            }

            // Usually the user is still in the messaging app; background activity starts are blocked, so wait to be back
            if (owner._isActivityResumed)
            {
                activity.StartActivityForResult(consentIntent, ConsentRequestCode);
            }
            else
            {
                owner._pendingConsent = consentIntent;
            }
        }
    }
}
