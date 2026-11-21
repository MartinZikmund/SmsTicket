using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml.Data;
using Windows.ApplicationModel.Resources.Core;

namespace SmsTicket.Services.Localizer;

[Bindable]
public class LocalizationService : ILocalizerService
{
    private ResourceLoader _resourceLoader = null;

    public LocalizationService()
    {

    }

    public string this[string key]
    {
        get
        {
            if (!string.IsNullOrEmpty(key))
            {
                try
                {
                    if (_resourceLoader == null)
                    {
#if !HAS_UNO
                        _resourceLoader = ResourceLoader.GetForViewIndependentUse("SmsTicket/Resources");
#else
                        _resourceLoader = new ResourceLoader();
#endif
                    }
                    var translation = _resourceLoader.GetString(key);
                    if (!string.IsNullOrEmpty(translation))
                    {
                        return translation;
                    }
                }
                catch
                {
                    // ignored
                }
            }
            return key;
        }
    }

    public string SMSTicket => this["SMSTicket"];
    public string Settings => this["Settings"];
    public string Help => this["Help"];
    public string HelpText => this["HelpText"];
    public string City => this["City"];
    public string TicketType => this["TicketType"];
    public string PrepareToSend => this["PrepareToSend"];

    public string SystemTheme => this["SystemTheme"];
    public string DarkTheme => this["DarkTheme"];
    public string LightTheme => this["LightTheme"];
    public string AboutApp => this["AboutApp"];

    public string PlusPriceOfSms => this["PlusPriceOfSms"];

    public string QuickActionDescriptionFormatString => this[nameof(QuickActionDescriptionFormatString)];

    public string FavoriteAddedTitle => this[nameof(FavoriteAddedTitle)];

    public string FavoriteAddedDescription => this[nameof(FavoriteAddedDescription)];
    public string PrepareSmsNote => this[nameof(PrepareSmsNote)];
    public string PrepareSmsNoteTitle => this[nameof(PrepareSmsNoteTitle)];
    public string ChangeCity => this[nameof(ChangeCity)];
    public string ChooseCity => this[nameof(ChooseCity)];
    public string PhotoInfo => this[nameof(PhotoInfo)];
    public string PhotoBy => this[nameof(PhotoBy)];
    public string ViewSource => this[nameof(ViewSource)];
    public string Duplicate => this[nameof(Duplicate)];
    public string Favorites => this[nameof(Favorites)];
    public string AddToFavorites => this[nameof(AddToFavorites)];
    public string RemoveFromFavorites => this[nameof(RemoveFromFavorites)];
    public string NavTickets => this[nameof(NavTickets)];
    public string PhotoCredits => this[nameof(PhotoCredits)];
    public string Version => this[nameof(Version)];
    public string MadeBy => this[nameof(MadeBy)];
    public string FeedbackTitle => this[nameof(FeedbackTitle)];
    public string ReportProblem => this[nameof(ReportProblem)];
    public string SourceCode => this[nameof(SourceCode)];
    public string OpenSourceCaption => this[nameof(OpenSourceCaption)];
    public string CityInfo => this[nameof(CityInfo)];
    public string TransportOperator => this[nameof(TransportOperator)];
    public string OperatorWebsite => this[nameof(OperatorWebsite)];
    public string SmsTicketInfo => this[nameof(SmsTicketInfo)];
    public string JourneyPlanner => this[nameof(JourneyPlanner)];
    public string UseMyLocation => this[nameof(UseMyLocation)];
    public string AutoSelectNearestCity => this[nameof(AutoSelectNearestCity)];
    public string NearestCityFound => this[nameof(NearestCityFound)];
    public string NearestCityNoneNearby => this[nameof(NearestCityNoneNearby)];
    public string NearestCityDenied => this[nameof(NearestCityDenied)];
    public string NearestCityUnavailable => this[nameof(NearestCityUnavailable)];
    public string Personalization => this[nameof(Personalization)];
    public string AppTheme => this[nameof(AppTheme)];
    public string AppThemeDescription => this[nameof(AppThemeDescription)];
    public string Language => this[nameof(Language)];
    public string LanguageDescription => this[nameof(LanguageDescription)];
    public string SystemLanguage => this[nameof(SystemLanguage)];
    public string CzechLanguage => this[nameof(CzechLanguage)];
    public string EnglishLanguage => this[nameof(EnglishLanguage)];
    public string RestartRequired => this[nameof(RestartRequired)];
    public string RestartRequiredMessage => this[nameof(RestartRequiredMessage)];
    public string Other => this[nameof(Other)];
    public string ShowTipsAgainTitle => this[nameof(ShowTipsAgainTitle)];
    public string ShowTipsAgainDescription => this[nameof(ShowTipsAgainDescription)];
    public string ShowTipsAgain => this[nameof(ShowTipsAgain)];
    public string NavHistory => this[nameof(NavHistory)];
    public string HistoryTitle => this[nameof(HistoryTitle)];
    public string HistoryEmptyTitle => this[nameof(HistoryEmptyTitle)];
    public string HistoryEmptyText => this[nameof(HistoryEmptyText)];
    public string HistoryDisclaimer => this[nameof(HistoryDisclaimer)];
    public string Delete => this[nameof(Delete)];
    public string DeleteEntryFormat => this[nameof(DeleteEntryFormat)];
    public string ClearHistory => this[nameof(ClearHistory)];
    public string ClearHistoryConfirmTitle => this[nameof(ClearHistoryConfirmTitle)];
    public string ClearHistoryConfirmText => this[nameof(ClearHistoryConfirmText)];
    public string Cancel => this[nameof(Cancel)];
    public string MonthTotal => this[nameof(MonthTotal)];
    public string ActiveTicketsTitle => this[nameof(ActiveTicketsTitle)];
    public string ActiveTicketEstimate => this[nameof(ActiveTicketEstimate)];
    public string ActiveTicketUntilFormat => this[nameof(ActiveTicketUntilFormat)];
    public string ActiveTicketStartsFormat => this[nameof(ActiveTicketStartsFormat)];
    public string ActiveTicketAwaitingConfirmation => this[nameof(ActiveTicketAwaitingConfirmation)];
    public string ActiveTicketEstimateHint => this[nameof(ActiveTicketEstimateHint)];
    public string ActiveTicketOptionsFormat => this[nameof(ActiveTicketOptionsFormat)];
    public string ActiveTicketAutomationFormat => this[nameof(ActiveTicketAutomationFormat)];
    public string ActiveTicketEstimatedAutomation => this[nameof(ActiveTicketEstimatedAutomation)];
    public string AdjustStartTime => this[nameof(AdjustStartTime)];
    public string AdjustStartTimeTitle => this[nameof(AdjustStartTimeTitle)];
    public string AdjustStartTimeText => this[nameof(AdjustStartTimeText)];
    public string Save => this[nameof(Save)];
    public string MarkNotSent => this[nameof(MarkNotSent)];
    public string DismissActiveTicket => this[nameof(DismissActiveTicket)];
    public string RemainingMinutesFormat => this[nameof(RemainingMinutesFormat)];
    public string RemainingHoursFormat => this[nameof(RemainingHoursFormat)];
    public string RemainingDaysFormat => this[nameof(RemainingDaysFormat)];
    public string ExpiryReminderTitle => this[nameof(ExpiryReminderTitle)];
    public string ExpiryReminderBodyFormat => this[nameof(ExpiryReminderBodyFormat)];
    public string ExpiryReminderChannelName => this[nameof(ExpiryReminderChannelName)];
    public string ExpiryReminderChannelDescription => this[nameof(ExpiryReminderChannelDescription)];
    public string PasteConfirmationSms => this[nameof(PasteConfirmationSms)];
    public string ShowTicketSms => this[nameof(ShowTicketSms)];
    public string PasteConfirmationSmsText => this[nameof(PasteConfirmationSmsText)];
    public string PasteConfirmationSmsPlaceholder => this[nameof(PasteConfirmationSmsPlaceholder)];
    public string ConfirmationUnrecognizedTitle => this[nameof(ConfirmationUnrecognizedTitle)];
    public string ConfirmationUnrecognizedText => this[nameof(ConfirmationUnrecognizedText)];
    public string ConfirmationStaleTitle => this[nameof(ConfirmationStaleTitle)];
    public string ConfirmationStaleText => this[nameof(ConfirmationStaleText)];
    public string ConfirmationSavedTitle => this[nameof(ConfirmationSavedTitle)];
    public string ConfirmationSavedFormat => this[nameof(ConfirmationSavedFormat)];
    public string ChooseTicketTitle => this[nameof(ChooseTicketTitle)];
    public string ChooseTicketItemFormat => this[nameof(ChooseTicketItemFormat)];
    public string NoTicketForSmsTitle => this[nameof(NoTicketForSmsTitle)];
    public string NoTicketForSmsText => this[nameof(NoTicketForSmsText)];
    public string TicketSmsTitle => this[nameof(TicketSmsTitle)];
    public string TicketSmsInspectionNote => this[nameof(TicketSmsInspectionNote)];
    public string TicketSmsValidityFormat => this[nameof(TicketSmsValidityFormat)];
    public string TicketSmsValidityUnknown => this[nameof(TicketSmsValidityUnknown)];
    public string TicketSmsCodeFormat => this[nameof(TicketSmsCodeFormat)];
    public string Close => this[nameof(Close)];
}
