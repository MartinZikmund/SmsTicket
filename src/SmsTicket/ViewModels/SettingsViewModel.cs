using SmsTicket.Services.AppSettings;

namespace SmsTicket.ViewModels;

public class SettingsViewModel : ObservableObject
{
    private readonly IAppSettings _appSettings;
    private readonly LanguagePreference _startupLanguage;
    private readonly Action<ThemePreference> _applyTheme;
    private readonly Action<string> _applyLanguage;

    public SettingsViewModel(
        IAppSettings appSettings,
        LanguagePreference startupLanguage,
        Action<ThemePreference> applyTheme,
        Action<string> applyLanguage)
    {
        _appSettings = appSettings;
        _startupLanguage = startupLanguage;
        _applyTheme = applyTheme;
        _applyLanguage = applyLanguage;

        ResetTipsCommand = new RelayCommand(ResetTips, () => !_appSettings.FirstTimeUse);
    }

    public IRelayCommand ResetTipsCommand { get; }

    public int ThemeIndex
    {
        get => (int)_appSettings.Theme;
        set
        {
            var theme = (ThemePreference)value;
            if (!Enum.IsDefined(theme) || theme == _appSettings.Theme)
            {
                return;
            }

            _appSettings.Theme = theme;
            _applyTheme(theme);
            OnPropertyChanged();
        }
    }

    public int LanguageIndex
    {
        get => (int)_appSettings.Language;
        set
        {
            var language = (LanguagePreference)value;
            if (!Enum.IsDefined(language) || language == _appSettings.Language)
            {
                return;
            }

            _appSettings.Language = language;
            _applyLanguage(language.ToLanguageTag());
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsRestartRequired));
        }
    }

    // Strings already loaded don't refresh, so a language change only fully applies after restart
    public bool IsRestartRequired => _appSettings.Language != _startupLanguage;

    private void ResetTips()
    {
        _appSettings.FirstTimeUse = true;
        ResetTipsCommand.NotifyCanExecuteChanged();
    }
}
