using SmsTicket.Services.Localizer;

namespace SmsTicket.Data.Models;

public sealed record CityTicket(City City, TicketType Ticket)
{
    private static readonly LocalizationService _localizer = new();

    public string ValidityText => Ticket.IsDuplicate ? _localizer.Duplicate : Ticket.TimeText;

    public string PriceText => $"{Ticket.Price} {_localizer["CzechPriceTag"]}";

    // Automation name of the favorite chip
    public override string ToString() => $"{City.Name}, {ValidityText}, {PriceText}";
}
