using SmsTicket.Services.Favorites;

namespace SmsTicket.Tests;

[TestClass]
public class FavoritesStoreTests
{
    private static readonly HashSet<string> KnownIds = new() { "A", "B", "C" };

    private static FavoritesStore CreateStore(InMemorySettingsService settings) => new(settings, KnownIds.Contains);

    [TestMethod]
    public void NewStore_IsEmpty()
    {
        var store = CreateStore(new());

        Assert.AreEqual(0, store.Ids.Count);
        Assert.IsFalse(store.Contains("A"));
    }

    [TestMethod]
    public void Add_AppendsInOrder()
    {
        var store = CreateStore(new());

        Assert.IsTrue(store.Add("B"));
        Assert.IsTrue(store.Add("A"));

        CollectionAssert.AreEqual(new[] { "B", "A" }, store.Ids.ToArray());
        Assert.IsTrue(store.Contains("A"));
    }

    [TestMethod]
    public void Add_IgnoresDuplicatesAndUnknownIds()
    {
        var store = CreateStore(new());
        store.Add("A");

        Assert.IsFalse(store.Add("A"));
        Assert.IsFalse(store.Add("Nope"));
        CollectionAssert.AreEqual(new[] { "A" }, store.Ids.ToArray());
    }

    [TestMethod]
    public void Remove_KeepsOrderOfTheRest()
    {
        var store = CreateStore(new());
        store.Add("A");
        store.Add("B");
        store.Add("C");

        Assert.IsTrue(store.Remove("B"));
        Assert.IsFalse(store.Remove("B"));
        CollectionAssert.AreEqual(new[] { "A", "C" }, store.Ids.ToArray());
    }

    [TestMethod]
    public void Toggle_AddsThenRemoves()
    {
        var store = CreateStore(new());

        Assert.IsTrue(store.Toggle("A"));
        Assert.IsTrue(store.Contains("A"));
        Assert.IsFalse(store.Toggle("A"));
        Assert.IsFalse(store.Contains("A"));
    }

    [TestMethod]
    public void Changes_ArePersisted()
    {
        InMemorySettingsService settings = new();
        var store = CreateStore(settings);
        store.Add("C");
        store.Add("A");

        var reloaded = CreateStore(settings);

        CollectionAssert.AreEqual(new[] { "C", "A" }, reloaded.Ids.ToArray());
    }

    [TestMethod]
    public void Load_DropsUnknownAndDuplicateIds()
    {
        InMemorySettingsService settings = new();
        settings.SetSetting(FavoritesStore.SettingKey, "A,Removed,B,A,,C");

        var store = CreateStore(settings);

        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, store.Ids.ToArray());
    }

    [TestMethod]
    public void Changed_IsRaisedOnlyOnActualChange()
    {
        var store = CreateStore(new());
        var count = 0;
        store.Changed += (_, _) => count++;

        store.Add("A");
        store.Add("A");
        store.Remove("Nope");
        store.Remove("A");

        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void ReplaceAll_FiltersAndPersists()
    {
        InMemorySettingsService settings = new();
        var store = CreateStore(settings);

        store.ReplaceAll(new[] { "C", "Gone", "C", "A" });

        CollectionAssert.AreEqual(new[] { "C", "A" }, store.Ids.ToArray());
        CollectionAssert.AreEqual(new[] { "C", "A" }, CreateStore(settings).Ids.ToArray());
    }
}
