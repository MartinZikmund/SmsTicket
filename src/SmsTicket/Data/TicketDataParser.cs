using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SmsTicket.Data;

public static class TicketDataParser
{
    public const int SupportedSchemaVersion = 1;

    public static bool TryParse(string json, [NotNullWhen(true)] out TicketDataDocument? document) =>
        TryParse(json, out document, out _);

    public static bool TryParse(string json, [NotNullWhen(true)] out TicketDataDocument? document, out string? error)
    {
        document = null;
        TicketDataDocument? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize(json, TicketDataJsonContext.Default.TicketDataDocument);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or ArgumentException)
        {
            error = ex.Message;
            return false;
        }

        error = parsed is null ? "Document is null" : Validate(parsed).FirstOrDefault();
        if (error is not null)
        {
            return false;
        }

        document = parsed;
        return true;
    }

    public static IEnumerable<string> Validate(TicketDataDocument document)
    {
        if (document.SchemaVersion != SupportedSchemaVersion)
        {
            yield return $"Unsupported schema version {document.SchemaVersion}";
            yield break;
        }

        if (document.Cities.Count == 0)
        {
            yield return "No cities";
        }

        HashSet<string> cityIds = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> ticketIds = new();
        foreach (var city in document.Cities)
        {
            if (string.IsNullOrWhiteSpace(city.Id) || !cityIds.Add(city.Id))
            {
                yield return $"City id '{city.Id}' is empty or not unique";
            }

            if (city.Name.IsEmpty || city.Abbreviation.IsEmpty || string.IsNullOrWhiteSpace(city.OperatorName))
            {
                yield return $"{city.Id}: missing name, abbreviation or operator";
            }

            foreach (var url in new[] { city.OperatorUrl, city.SmsInfoUrl, city.JourneyPlannerUrl })
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                {
                    yield return $"{city.Id}: '{url}' is not an absolute https URL";
                }
            }

            if (!city.Tickets.Any(t => t.Id == city.DefaultTicketId))
            {
                yield return $"{city.Id}: default ticket '{city.DefaultTicketId}' does not exist";
            }

            foreach (var ticket in city.Tickets)
            {
                if (string.IsNullOrWhiteSpace(ticket.Id) || !ticketIds.Add(ticket.Id))
                {
                    yield return $"Ticket id '{ticket.Id}' is empty or not unique";
                }

                if (ticket.Price < 0)
                {
                    yield return $"{ticket.Id}: negative price";
                }

                if (ticket.PhoneNumber.Length == 0 || !ticket.PhoneNumber.All(char.IsAsciiDigit))
                {
                    yield return $"{ticket.Id}: phone number '{ticket.PhoneNumber}' must be digits only";
                }

                if (string.IsNullOrWhiteSpace(ticket.SmsText))
                {
                    yield return $"{ticket.Id}: empty SMS text";
                }
            }
        }
    }
}
