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

    public string PinnedTitle => this[nameof(PinnedTitle)];

    public string PinnedDescription => this[nameof(PinnedDescription)];
    public string PrepareSmsNote => this[nameof(PrepareSmsNote)];
    public string PrepareSmsNoteTitle => this[nameof(PrepareSmsNoteTitle)];
    public string ChangeCity => this[nameof(ChangeCity)];
    public string ChooseCity => this[nameof(ChooseCity)];
    public string PhotoInfo => this[nameof(PhotoInfo)];
    public string PhotoBy => this[nameof(PhotoBy)];
    public string ViewSource => this[nameof(ViewSource)];
    public string Duplicate => this[nameof(Duplicate)];
    public string Pin => this[nameof(Pin)];
    public string Unpin => this[nameof(Unpin)];
    public string NavTickets => this[nameof(NavTickets)];
    public string PhotoCredits => this[nameof(PhotoCredits)];
    public string Version => this[nameof(Version)];
    public string MadeBy => this[nameof(MadeBy)];
}
