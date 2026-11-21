using System.Globalization;
using SmsTicket.Data.Models;

namespace SmsTicket.Data;

public static class TicketDataMapper
{
    public static IReadOnlyList<City> ToCities(TicketDataDocument document, string language) =>
        document.Cities
            .Select(city => new City
            {
                Id = city.Id,
                Name = city.Name.Resolve(language),
                Abbreviation = city.Abbreviation.Resolve(language),
                OperatorName = city.OperatorName,
                OperatorUrl = city.OperatorUrl,
                SmsInfoUrl = city.SmsInfoUrl,
                JourneyPlannerUrl = city.JourneyPlannerUrl,
                DefaultTicketId = city.DefaultTicketId,
                TicketTypes = city.Tickets.Select(ticket => new TicketType(
                    ticket.Id,
                    ticket.Description?.Resolve(language) ?? "",
                    ticket.Price.ToString(CultureInfo.InvariantCulture),
                    ticket.TimeText?.Resolve(language) ?? "",
                    ticket.PhoneNumber,
                    ticket.SmsText,
                    ticket.SmsPriceAdditional ?? true)).ToList(),
            })
            .OrderBy(city => city.Name)
            .ToArray();
}
