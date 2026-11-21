using Android.App;
using Android.Content;
using Android.OS;
using SmsTicket.Services.Confirmation;

namespace SmsTicket.Droid;

/// <summary>
/// Receives a ticket SMS shared from the messaging app, hands it to the app and brings the main activity forward.
/// A separate activity keeps the single Uno activity out of the sharing app's task.
/// </summary>
[Activity(
    Exported = true,
    NoHistory = true,
    ExcludeFromRecents = true,
    Label = "@string/ApplicationName",
    Theme = "@android:style/Theme.Translucent.NoTitleBar")]
[IntentFilter([Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "text/plain")]
public sealed class ShareTargetActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Untrusted input: only plain text, and it is only ever parsed
        if (Intent?.Action == Intent.ActionSend && Intent.GetStringExtra(Intent.ExtraText) is { Length: > 0 and < 4096 } text)
        {
            ConfirmationSmsInbox.Post(new(text, null));
        }

        Intent main = new(this, typeof(MainActivity));
        main.AddFlags(ActivityFlags.NewTask | ActivityFlags.ReorderToFront);
        StartActivity(main);
        Finish();
    }
}
