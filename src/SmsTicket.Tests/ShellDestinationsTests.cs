using SmsTicket.Views;

namespace SmsTicket.Tests;

[TestClass]
public class ShellDestinationsTests
{
    [TestMethod]
    [DataRow("Tickets", typeof(MainPage))]
    [DataRow("History", typeof(HistoryPage))]
    [DataRow("Settings", typeof(SettingsPage))]
    [DataRow("About", typeof(AboutPage))]
    public void Tag_RoundTripsThroughPage(string tag, Type page)
    {
        Assert.AreEqual(page, ShellDestinations.PageFor(tag));
        Assert.AreEqual(tag, ShellDestinations.TagFor(page));
    }

    [TestMethod]
    [DataRow("Tickets", "Tickets")]
    [DataRow("History", "History")]
    [DataRow("Settings", "Settings")]
    [DataRow("About", "Settings")]
    public void Destination_IsUnderItsTab(string tag, string tab) => Assert.AreEqual(tab, ShellDestinations.TabFor(tag));

    [TestMethod]
    public void UnknownTag_HasNoPage() => Assert.IsNull(ShellDestinations.PageFor("Nope"));

    [TestMethod]
    public void NullTag_HasNoPage() => Assert.IsNull(ShellDestinations.PageFor(null));

    [TestMethod]
    public void UnknownPage_HasNoTag() => Assert.IsNull(ShellDestinations.TagFor(typeof(ShellPage)));
}
