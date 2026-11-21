using System.Globalization;

namespace SmsTicket.Data.Models;

/// <summary>
/// A ticket SMS the user prepared. Whether it was actually sent is unknown.
/// </summary>
public sealed partial record PurchaseRecord
{
    public required Guid Id { get; init; }

    public required string CityId { get; init; }

    public required string CityName { get; init; }

    public required string TicketId { get; init; }

    /// <summary>Validity text such as "30 min"; empty for duplicates.</summary>
    public string TimeText { get; init; } = "";

    /// <summary>Price in CZK.</summary>
    public required int Price { get; init; }

    public required DateTimeOffset PreparedAt { get; init; }

    public static PurchaseRecord FromTicket(City city, TicketType ticket, DateTimeOffset preparedAt) => new()
    {
        Id = Guid.NewGuid(),
        CityId = city.Id,
        CityName = city.Name,
        TicketId = ticket.Id,
        TimeText = ticket.TimeText,
        Price = int.TryParse(ticket.Price, NumberStyles.Integer, CultureInfo.InvariantCulture, out var price) ? price : 0,
        PreparedAt = preparedAt,
    };
}
