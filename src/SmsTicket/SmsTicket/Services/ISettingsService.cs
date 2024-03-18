using System;

namespace SmsTicket.Services.Settings;

public interface ISettingsService
{
    T GetSetting<T>( string key, Func<T> defaultValue, bool roamingEnabled = false );
    bool SetSetting<T>( string key, T value, bool roamingEnabled = false );
}