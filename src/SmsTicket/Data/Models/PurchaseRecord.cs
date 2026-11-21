using System.Globalization;
using System.Text.Json.Serialization;

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

    /// <summary>When validity started, if known better than the estimate.</summary>
    public DateTimeOffset? ValidFrom { get; init; }

    /// <summary>When validity ends, if known better than the rule's result.</summary>
    public DateTimeOffset? ValidUntil { get; init; }

    /// <summary>Where <see cref="ValidFrom"/> and <see cref="ValidUntil"/> came from; null means estimated.</summary>
    public ValiditySource? ValiditySource { get; init; }

    /// <summary>Set when the user hid the ticket from the active tickets.</summary>
    public DateTimeOffset? DismissedAt { get; init; }

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

[JsonConverter(typeof(JsonStringEnumConverter<ValiditySource>))]
public enum ValiditySource
{
    /// <summary>Computed from the time the SMS was prepared.</summary>
    [JsonStringEnumMemberName("estimated")]
    Estimated,

    /// <summary>The user entered when the ticket SMS arrived.</summary>
    [JsonStringEnumMemberName("adjusted")]
    Adjusted,

    /// <summary>Read from the operator's ticket SMS.</summary>
    [JsonStringEnumMemberName("confirmed")]
    Confirmed,
}
