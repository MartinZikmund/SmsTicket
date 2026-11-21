using System.Text.Json.Serialization;
using SmsTicket.Data.Models;

namespace SmsTicket.Services.PurchaseHistory;

public sealed class PurchaseHistoryDocument
{
    /// <summary>Version 2 added the optional validity fields; version 1 files load unchanged.</summary>
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;

    public List<PurchaseRecord> Purchases { get; set; } = new();
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PurchaseHistoryDocument))]
internal sealed partial class PurchaseHistoryJsonContext : JsonSerializerContext
{
}
