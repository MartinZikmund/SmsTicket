using System.Text.Json.Serialization;
using SmsTicket.Data.Models;

namespace SmsTicket.Services.PurchaseHistory;

public sealed class PurchaseHistoryDocument
{
    public int Version { get; set; } = 1;

    public List<PurchaseRecord> Purchases { get; set; } = new();
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PurchaseHistoryDocument))]
internal sealed partial class PurchaseHistoryJsonContext : JsonSerializerContext
{
}
