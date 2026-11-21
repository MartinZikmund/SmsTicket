using SmsTicket.Services.Localizer;

namespace SmsTicket.Data.Models;

public class TicketType
{
    private static readonly LocalizationService _localizer = new();

    public TicketType(
        string id,
        string description,
        string price,
        string timeText,
        string phoneNumber,
        string smsText,
        bool smsPriceAdditional)
    {
        Id = id;
        Description = description;
        Price = price;
        TimeText = timeText;
        PhoneNumber = phoneNumber;
        SmsText = smsText;
        SmsPriceAdditional = smsPriceAdditional;
    }

    public string Id { get; set; }
    public string Price { get; set; }
    public string TimeText { get; set; }

    public bool SmsPriceAdditional { get; set; }

    public string Description { get; set; }

    public string PhoneNumber { get; set; }
    public string SmsText { get; set; }

    public bool IsDuplicate => string.IsNullOrEmpty(TimeText);

    public bool HasDescription => !string.IsNullOrEmpty(Description);

    // Used by GridView/ListView as the item's automation name
    public override string ToString() => $"{(IsDuplicate ? _localizer.Duplicate : TimeText)}, {Price} {_localizer["CzechPriceTag"]}";
}
