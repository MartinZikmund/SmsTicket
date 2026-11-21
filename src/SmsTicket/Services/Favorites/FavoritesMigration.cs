namespace SmsTicket.Services.Favorites;

public static class FavoritesMigration
{
    internal const string MigratedSettingKey = "FavoritesMigratedFromJumpList";

    /// <summary>
    /// Folds tickets pinned to the JumpList (whose arguments are ticket ids) into favorites.
    /// </summary>
    public static IReadOnlyList<string> Merge(IEnumerable<string> favorites, IEnumerable<string?> jumpListArguments) =>
        favorites
            .Concat(jumpListArguments.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a!))
            .Distinct()
            .ToList();
}
