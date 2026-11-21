using SmsTicket.Services.Favorites;

namespace SmsTicket.Tests;

[TestClass]
public class FavoritesMigrationTests
{
    [TestMethod]
    public void Merge_NoJumpListItems_KeepsFavorites() =>
        CollectionAssert.AreEqual(
            new[] { "A", "B" },
            FavoritesMigration.Merge(new[] { "A", "B" }, Array.Empty<string?>()).ToArray());

    [TestMethod]
    public void Merge_AppendsPinnedTicketsAfterExistingFavorites() =>
        CollectionAssert.AreEqual(
            new[] { "A", "C", "D" },
            FavoritesMigration.Merge(new[] { "A" }, new[] { "C", "D" }).ToArray());

    [TestMethod]
    public void Merge_SkipsDuplicatesAndEmptyArguments() =>
        CollectionAssert.AreEqual(
            new[] { "A", "B" },
            FavoritesMigration.Merge(new[] { "A" }, new[] { "A", null, "", "  ", "B", "B" }).ToArray());

    [TestMethod]
    public void Merge_EmptyFavorites_TakesPinnedOrder() =>
        CollectionAssert.AreEqual(
            new[] { "Z", "Y" },
            FavoritesMigration.Merge(Array.Empty<string>(), new[] { "Z", "Y" }).ToArray());
}
