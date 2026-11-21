namespace SmsTicket.Tests;

internal static class TicketDataJson
{
    public static string Build(
        string dataVersion = "2026-01-01",
        int schemaVersion = 1,
        string? cities = null) =>
        $$"""
        {
          "schemaVersion": {{schemaVersion}},
          "dataVersion": "{{dataVersion}}",
          "cities": [ {{cities ?? City()}} ]
        }
        """;

    public static string City(
        string id = "PHA",
        string name = """{ "cs": "Praha", "en": "Prague" }""",
        string defaultTicketId = "T1",
        string? tickets = null,
        string operatorUrl = "https://www.dpp.cz/") =>
        $$"""
        {
          "id": "{{id}}",
          "name": {{name}},
          "abbreviation": { "cs": "PHA", "en": "PRG" },
          "operatorName": "DPP",
          "operatorUrl": "{{operatorUrl}}",
          "smsInfoUrl": "https://www.dpp.cz/sms",
          "journeyPlannerUrl": "https://idos.cz/pid/spojeni/",
          "defaultTicketId": "{{defaultTicketId}}",
          "tickets": [ {{tickets ?? Ticket()}} ]
        }
        """;

    public static string Ticket(
        string id = "T1",
        string price = "42",
        string phoneNumber = "90206",
        string? timeText = "\"30 min\"",
        string? description = null) =>
        $$"""
        {
          "id": "{{id}}",
          {{(description is null ? "" : $"\"description\": {description},")}}
          {{(timeText is null ? "" : $"\"timeText\": {timeText},")}}
          "price": {{price}},
          "phoneNumber": "{{phoneNumber}}",
          "smsText": "DPT42"
        }
        """;
}
