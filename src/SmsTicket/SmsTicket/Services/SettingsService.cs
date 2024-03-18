using System;
using Windows.Storage;

namespace SmsTicket.Services.Settings;

public class SettingsService : ISettingsService
{
    public T GetSetting<T>( string key, Func<T> defaultValue, bool roamingEnabled = false )
    {
        return GetSetting( key, defaultValue,
            roamingEnabled ? ApplicationData.Current.RoamingSettings : ApplicationData.Current.LocalSettings );
    }

    public bool SetSetting<T>( string key, T value, bool roamingEnabled = false )
    {
        return SetSetting( key, value,
            roamingEnabled ? ApplicationData.Current.RoamingSettings : ApplicationData.Current.LocalSettings );
    }

    /// <summary>
    /// Retrieves the given setting from application data container
    /// </summary>
    /// <typeparam name="T">Type of the stored setting</typeparam>
    /// <param name="key">String key identification of the setting</param>
    /// <param name="defaultValue">Default value if setting not set</param>
    /// <param name="container">Application data container (local, roaming)</param>
    /// <returns></returns>
    private static T GetSetting<T>( string key, Func<T> defaultValue, ApplicationDataContainer container )
    {
        T result = default( T );
        try
        {
            if ( container.Values.ContainsKey( key ) )
            {
                result = ( T )container.Values[ key ];
            }
            else
            {
                result = defaultValue();
                container.Values.Add( key, result );
            }
        }
        catch
        {
            result = defaultValue();
            if ( container.Values.ContainsKey( key ) )
            {
                container.Values[ key ] = result;
            }
            else
            {
                container.Values.Add( key, result );
            }
        }
        return result;
    }

    /// <summary>
    /// Stores a value for setting in application data container settings
    /// </summary>
    /// <typeparam name="T">Type of the setting</typeparam>
    /// <param name="key">String key identification for the setting</param>
    /// <param name="value">Value to be stored</param>
    /// <param name="container">Application data container used (local, roaming)</param>
    /// <returns></returns>
    private static bool SetSetting<T>( string key, T value, ApplicationDataContainer container )
    {
        try
        {
            if ( container.Values.ContainsKey( key ) )
            {
                container.Values[ key ] = value;
            }
            else
            {
                container.Values.Add( key, value );
            }
            return true;
        }
        catch
        {

        }
        return false;
    }
}
