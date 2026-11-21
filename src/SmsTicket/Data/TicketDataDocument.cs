using System.Text.Json.Serialization;

namespace SmsTicket.Data;

/// <summary>
/// Shape of data/tickets.json. Bump <see cref="SchemaVersion"/> only for breaking changes,
/// <see cref="DataVersion"/> for every data update.
/// </summary>
public sealed class TicketDataDocument
{
    public required int SchemaVersion { get; init; }

    public required DateOnly DataVersion { get; init; }

    public required IReadOnlyList<CityData> Cities { get; init; }
}

public sealed class CityData
{
    public required string Id { get; init; }

    public required LocalizedText Name { get; init; }

    public required LocalizedText Abbreviation { get; init; }

    public required string OperatorName { get; init; }

    public required string OperatorUrl { get; init; }

    public required string SmsInfoUrl { get; init; }

    public required string JourneyPlannerUrl { get; init; }

    public required string DefaultTicketId { get; init; }

    public required IReadOnlyList<TicketData> Tickets { get; init; }
}

public sealed class TicketData
{
    public required string Id { get; init; }

    public LocalizedText? Description { get; init; }

    // Missing for duplicate-ticket requests
    public LocalizedText? TimeText { get; init; }

    public required int Price { get; init; }

    public required string PhoneNumber { get; init; }

    public required string SmsText { get; init; }

    // Defaults to true; nullable because source-generated init properties lose initializers
    public bool? SmsPriceAdditional { get; init; }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    RespectNullableAnnotations = true)]
[JsonSerializable(typeof(TicketDataDocument))]
internal sealed partial class TicketDataJsonContext : JsonSerializerContext
{
}
