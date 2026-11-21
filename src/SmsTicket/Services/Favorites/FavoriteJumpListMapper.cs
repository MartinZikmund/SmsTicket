using SmsTicket.Data.Models;

namespace SmsTicket.Services.Favorites;

public record JumpListTexts(string Duplicate, string DescriptionFormat);

public record JumpListItemSpec(string Arguments, string DisplayName, string Description, string Logo);

public static class FavoriteJumpListMapper
{
    // Android throws from SetDynamicShortcuts above the launcher limit (typically 4-5)
    public const int MaxItems = 4;

    public static IReadOnlyList<JumpListItemSpec> ToSpecs(IEnumerable<CityTicket> favorites, JumpListTexts texts) =>
        favorites
            .Take(MaxItems)
            .Select(f => new JumpListItemSpec(
                f.Ticket.Id,
                $"{f.City.Name} - {(f.Ticket.IsDuplicate ? texts.Duplicate : f.Ticket.TimeText)}",
                string.Format(texts.DescriptionFormat, f.Ticket.Price),
                f.Ticket.IsDuplicate ? "ms-appx:///Assets/ticket_dupe.png" : "ms-appx:///Assets/ticket.png"))
            .ToList();
}
