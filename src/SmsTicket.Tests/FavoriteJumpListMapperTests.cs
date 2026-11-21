using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.Favorites;

namespace SmsTicket.Tests;

[TestClass]
public class FavoriteJumpListMapperTests
{
    private static readonly JumpListTexts Texts = new("Duplicate", "Price {0} CZK");

    private static CityTicket Ticket(string id) =>
        DataSource.FindTicket(id) ?? throw new AssertFailedException($"Unknown ticket {id}");

    [TestMethod]
    public void ToSpecs_MapsTicketToItem()
    {
        var spec = FavoriteJumpListMapper.ToSpecs(new[] { Ticket("LIB") }, Texts).Single();

        Assert.AreEqual("LIB", spec.Arguments);
        Assert.AreEqual("Liberec - 60 min", spec.DisplayName);
        Assert.AreEqual("Price 38 CZK", spec.Description);
        Assert.AreEqual("ms-appx:///Assets/ticket.png", spec.Logo);
    }

    [TestMethod]
    public void ToSpecs_DuplicateTicket_UsesDuplicateTextAndLogo()
    {
        var spec = FavoriteJumpListMapper.ToSpecs(new[] { Ticket("LIBDupe") }, Texts).Single();

        Assert.AreEqual("Liberec - Duplicate", spec.DisplayName);
        Assert.AreEqual("ms-appx:///Assets/ticket_dupe.png", spec.Logo);
    }

    [TestMethod]
    public void ToSpecs_KeepsOrder() =>
        CollectionAssert.AreEqual(
            new[] { "BR29", "LIB", "HK" },
            FavoriteJumpListMapper.ToSpecs(new[] { Ticket("BR29"), Ticket("LIB"), Ticket("HK") }, Texts)
                .Select(s => s.Arguments)
                .ToArray());

    [TestMethod]
    public void ToSpecs_CapsAtLauncherLimit()
    {
        var tickets = new[] { "BR20", "BR29", "BR99", "LIB", "HK", "DPMP" }.Select(Ticket).ToList();

        var specs = FavoriteJumpListMapper.ToSpecs(tickets, Texts);

        Assert.AreEqual(FavoriteJumpListMapper.MaxItems, specs.Count);
        Assert.AreEqual("BR20", specs[0].Arguments);
    }

    [TestMethod]
    public void FindTicket_ReturnsOwningCity()
    {
        var favorite = Ticket("HK24");

        Assert.AreEqual("HK", favorite.City.Id);
        Assert.AreEqual("HK24", favorite.Ticket.Id);
    }

    [TestMethod]
    public void FindTicket_UnknownId_ReturnsNull() => Assert.IsNull(DataSource.FindTicket("DoesNotExist"));
}
