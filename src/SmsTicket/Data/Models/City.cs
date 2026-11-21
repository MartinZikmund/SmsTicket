namespace SmsTicket.Data.Models;

public class City
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Abbreviation { get; init; }

    public required string DefaultTicketId { get; init; }

    public required List<TicketType> TicketTypes { get; init; }

    public required string OperatorName { get; init; }

    public required string OperatorUrl { get; init; }

    public required string SmsInfoUrl { get; init; }

    public required string JourneyPlannerUrl { get; init; }

    // Photos are bundled, so a city added remotely later may have none
    public string? HeroImage => Photo is null ? null : $"ms-appx:///Assets/Cities/{Id.ToLowerInvariant()}.jpg";

    public PhotoCredit? Photo => PhotoCredits.ForCity(Id);

    // Used by ListView as the item's automation name
    public override string ToString() => Name;
}
