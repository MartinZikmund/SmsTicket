using SmsTicket.Services.AppSettings;

namespace SmsTicket.Services.Reminders;

/// <summary>Asks the OS for permission to show expiry reminders.</summary>
public interface IReminderPermission
{
    Task<bool> RequestAsync();
}

/// <summary>
/// Asks for the reminder permission once, before the first ticket SMS is prepared.
/// Asking later races the messaging app: the system prompt takes over and the SMS never shows.
/// </summary>
public sealed class ReminderPermissionGate
{
    private readonly IAppSettings _appSettings;
    private readonly IReminderPermission _permission;

    public ReminderPermissionGate(IAppSettings appSettings, IReminderPermission permission)
    {
        _appSettings = appSettings;
        _permission = permission;
    }

    public async Task EnsureAskedAsync()
    {
        if (_appSettings.ReminderPermissionAsked)
        {
            return;
        }

        _appSettings.ReminderPermissionAsked = true;
        await _permission.RequestAsync();
    }
}

public static class ReminderPermissions
{
    public static IReminderPermission Create() =>
#if __ANDROID__
        new SmsTicket.Droid.AndroidReminderPermission();
#elif __IOS__
        new SmsTicket.iOS.IosReminderPermission();
#else
        new GrantedReminderPermission();
#endif
}

/// <summary>Platforms that need no runtime permission for reminders, or can't show them.</summary>
public sealed class GrantedReminderPermission : IReminderPermission
{
    public Task<bool> RequestAsync() => Task.FromResult(true);
}
