using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmsTicket.Data;

/// <summary>
/// Text in the ticket data: either a plain string used for every language,
/// or an object keyed by language code, e.g. <c>{ "cs": "1 den", "en": "1 day" }</c>.
/// </summary>
[JsonConverter(typeof(LocalizedTextConverter))]
public sealed class LocalizedText
{
    private static readonly string[] FallbackLanguages = ["en", "cs"];

    private readonly string? _invariant;

    public LocalizedText(string invariant)
    {
        _invariant = invariant;
        Values = new Dictionary<string, string>();
    }

    public LocalizedText(IReadOnlyDictionary<string, string> values) =>
        Values = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> Values { get; }

    public bool IsInvariant => _invariant is not null;

    public bool IsEmpty => IsInvariant
        ? string.IsNullOrWhiteSpace(_invariant)
        : Values.Values.All(string.IsNullOrWhiteSpace);

    public string Resolve(string language)
    {
        if (_invariant is not null)
        {
            return _invariant;
        }

        var neutral = language.Split('-')[0];
        foreach (var candidate in new[] { language, neutral }.Concat(FallbackLanguages))
        {
            if (Values.TryGetValue(candidate, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return Values.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";
    }

    public override string ToString() => Resolve("en");
}

public sealed class LocalizedTextConverter : JsonConverter<LocalizedText>
{
    public override LocalizedText Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new LocalizedText(reader.GetString()!);
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a string or an object of translations.");
        }

        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var language = reader.GetString()!;
            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Translation '{language}' must be a string.");
            }

            values[language] = reader.GetString()!;
        }

        return new LocalizedText(values);
    }

    public override void Write(Utf8JsonWriter writer, LocalizedText value, JsonSerializerOptions options)
    {
        if (value.IsInvariant)
        {
            writer.WriteStringValue(value.Resolve(""));
            return;
        }

        writer.WriteStartObject();
        foreach (var (language, text) in value.Values)
        {
            writer.WriteString(language, text);
        }

        writer.WriteEndObject();
    }
}
