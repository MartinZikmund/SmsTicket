using SmsTicket.Services.Settings;

namespace SmsTicket.Services.Favorites;

/// <summary>
/// Ordered list of favorite ticket ids, persisted as a single comma-separated setting.
/// </summary>
public class FavoritesStore
{
    public const string SettingKey = "FavoriteTicketIds";
    private const char Separator = ',';

    private readonly ISettingsService _settings;
    private readonly Func<string, bool> _isKnownTicket;
    private readonly List<string> _ids;

    public FavoritesStore(ISettingsService settings, Func<string, bool> isKnownTicket)
    {
        _settings = settings;
        _isKnownTicket = isKnownTicket;

        var stored = _settings.GetSetting(SettingKey, () => "");
        _ids = Sanitize(stored.Split(Separator)).ToList();
    }

    public event EventHandler? Changed;

    public IReadOnlyList<string> Ids => _ids;

    public bool Contains(string id) => _ids.Contains(id);

    public bool Add(string id)
    {
        if (Contains(id) || !_isKnownTicket(id))
        {
            return false;
        }

        _ids.Add(id);
        Save();
        return true;
    }

    public bool Remove(string id)
    {
        if (!_ids.Remove(id))
        {
            return false;
        }

        Save();
        return true;
    }

    /// <returns>Whether the ticket is a favorite after the call.</returns>
    public bool Toggle(string id)
    {
        if (!Remove(id))
        {
            Add(id);
        }

        return Contains(id);
    }

    public void ReplaceAll(IEnumerable<string> ids)
    {
        var sanitized = Sanitize(ids).ToList();
        if (sanitized.SequenceEqual(_ids))
        {
            return;
        }

        _ids.Clear();
        _ids.AddRange(sanitized);
        Save();
    }

    private IEnumerable<string> Sanitize(IEnumerable<string> ids) =>
        ids.Where(id => !string.IsNullOrWhiteSpace(id) && _isKnownTicket(id)).Distinct();

    private void Save()
    {
        _settings.SetSetting(SettingKey, string.Join(Separator, _ids));
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
