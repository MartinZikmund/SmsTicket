using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using SmsTicket.Services.Localizer;
using SmsTicket.Services.Reminders;
using AndroidApplication = Android.App.Application;

namespace SmsTicket.Droid;

/// <summary>Schedules expiry reminders with AlarmManager; <see cref="ExpiryReminderReceiver"/> posts them.</summary>
public sealed class AndroidExpiryReminderScheduler : IExpiryReminderScheduler
{
    private const string PreferencesName = "expiry-reminders";
    private const string ScheduledKey = "scheduled";

    // Android 14 stretches shorter inexact windows to 10 minutes - at their end, which would make us late
    private static readonly TimeSpan InexactWindow = TimeSpan.FromMinutes(10);

    public Task ReplaceAllAsync(IReadOnlyList<ExpiryReminder> reminders)
    {
        var context = AndroidApplication.Context;
        var alarms = (AlarmManager)context.GetSystemService(Context.AlarmService)!;
        var preferences = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)!;
        var wanted = reminders.Select(r => r.Tag).ToHashSet();

        foreach (var tag in preferences.GetStringSet(ScheduledKey, null) ?? [])
        {
            if (!wanted.Contains(tag) &&
                ExpiryReminderReceiver.CreatePendingIntent(context, tag, null, PendingIntentFlags.NoCreate) is { } stale)
            {
                alarms.Cancel(stale);
                stale.Cancel();
            }
        }

        if (reminders.Count > 0)
        {
            ExpiryReminderReceiver.EnsureChannel(context);
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var reminder in reminders)
        {
            var pendingIntent = ExpiryReminderReceiver.CreatePendingIntent(context, reminder.Tag, reminder, PendingIntentFlags.UpdateCurrent)!;
            if (!OperatingSystem.IsAndroidVersionAtLeast(31) || alarms.CanScheduleExactAlarms())
            {
                alarms.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, reminder.NotifyAt.ToUnixTimeMilliseconds(), pendingIntent);
            }
            else
            {
                var (start, length) = ExpiryReminderPlanner.GetInexactWindow(reminder.NotifyAt, now, InexactWindow);
                alarms.SetWindow(AlarmType.RtcWakeup, start.ToUnixTimeMilliseconds(), (long)length.TotalMilliseconds, pendingIntent);
            }
        }

        preferences.Edit()!.PutStringSet(ScheduledKey, wanted)!.Apply();
        return Task.CompletedTask;
    }
}

/// <summary>Android 13+ needs POST_NOTIFICATIONS at runtime; earlier versions grant it at install.</summary>
public sealed class AndroidReminderPermission : IReminderPermission
{
    public async Task<bool> RequestAsync() =>
        !OperatingSystem.IsAndroidVersionAtLeast(33) ||
        await Windows.Extensions.PermissionsHelper.TryGetPermission(CancellationToken.None, Manifest.Permission.PostNotifications);
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class ExpiryReminderReceiver : BroadcastReceiver
{
    private const string Action = "com.sphereline.smsticket.EXPIRY_REMINDER";
    private const string ChannelId = "ticket-expiry";
    private const string TitleExtra = "title";
    private const string BodyExtra = "body";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent?.Data?.LastPathSegment is not { } tag)
        {
            return;
        }

        EnsureChannel(context);
        var launch = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
        var contentIntent = launch is null
            ? null
            : PendingIntent.GetActivity(context, 0, launch, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

#pragma warning disable CA1422 // The channel-less builder is only used below API 26
        var builder = OperatingSystem.IsAndroidVersionAtLeast(26)
            ? new Notification.Builder(context, ChannelId)
            : new Notification.Builder(context);
#pragma warning restore CA1422
        builder
            .SetSmallIcon(context.ApplicationInfo!.Icon)
            .SetContentTitle(intent.GetStringExtra(TitleExtra))
            .SetContentText(intent.GetStringExtra(BodyExtra))
            .SetAutoCancel(true)
            .SetCategory(Notification.CategoryReminder);
        if (contentIntent is not null)
        {
            builder.SetContentIntent(contentIntent);
        }

        var manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
        manager.Notify(tag, 0, builder.Build());
    }

    internal static void EnsureChannel(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
        if (manager.GetNotificationChannel(ChannelId) is not null)
        {
            return;
        }

        LocalizationService localizer = new();
        NotificationChannel channel = new(ChannelId, localizer.ExpiryReminderChannelName, NotificationImportance.High)
        {
            Description = localizer.ExpiryReminderChannelDescription,
        };
        manager.CreateNotificationChannel(channel);
    }

    /// <summary>One alarm per ticket: the tag lives in the intent data, which is what makes intents distinct.</summary>
    internal static PendingIntent? CreatePendingIntent(Context context, string tag, ExpiryReminder? reminder, PendingIntentFlags flags)
    {
        Intent intent = new(context, typeof(ExpiryReminderReceiver));
        intent.SetAction(Action);
        intent.SetData(Android.Net.Uri.Parse($"smsticket://expiry/{tag}"));
        if (reminder is not null)
        {
            intent.PutExtra(TitleExtra, reminder.Title);
            intent.PutExtra(BodyExtra, reminder.Body);
        }

        return PendingIntent.GetBroadcast(context, 0, intent, flags | PendingIntentFlags.Immutable);
    }
}
