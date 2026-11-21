using SmsTicket.Services.Settings;

namespace SmsTicket.Tests;

internal sealed class FakeSettingsService : ISettingsService
{
    public Dictionary<string, object?> Values { get; } = new();

    public T GetSetting<T>(string key, Func<T> defaultValue, bool roamingEnabled = false) =>
        Values.TryGetValue(key, out var value) && value is T typed ? typed : defaultValue();

    public bool SetSetting<T>(string key, T value, bool roamingEnabled = false)
    {
        Values[key] = value;
        return true;
    }
}
