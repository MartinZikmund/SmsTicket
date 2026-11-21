using SmsTicket.Data.Models;
using SmsTicket.Services.PurchaseHistory;

namespace SmsTicket.Tests;

[TestClass]
public class PurchaseHistoryServiceTests
{
    private string _directory = null!;

    private string FilePath => Path.Combine(_directory, "history.json");

    [TestInitialize]
    public void Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "SmsTicketTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task GetAll_WhenFileMissing_ReturnsEmpty()
    {
        PurchaseHistoryService service = new(FilePath);

        var all = await service.GetAllAsync();

        Assert.AreEqual(0, all.Count);
    }

    [TestMethod]
    public async Task Add_PersistsAcrossInstances()
    {
        var record = CreateRecord(new DateTimeOffset(2026, 11, 21, 8, 30, 0, TimeSpan.FromHours(1)));
        PurchaseHistoryService writer = new(FilePath);
        await writer.AddAsync(record);

        PurchaseHistoryService reader = new(FilePath);
        var all = await reader.GetAllAsync();

        Assert.AreEqual(1, all.Count);
        var loaded = all[0];
        Assert.AreEqual(record.Id, loaded.Id);
        Assert.AreEqual(record.CityId, loaded.CityId);
        Assert.AreEqual(record.CityName, loaded.CityName);
        Assert.AreEqual(record.TicketId, loaded.TicketId);
        Assert.AreEqual(record.TimeText, loaded.TimeText);
        Assert.AreEqual(record.Price, loaded.Price);
        Assert.AreEqual(record.PreparedAt, loaded.PreparedAt);
        Assert.AreEqual(record.PreparedAt.Offset, loaded.PreparedAt.Offset);
    }

    [TestMethod]
    public async Task GetAll_ReturnsNewestFirst()
    {
        PurchaseHistoryService service = new(FilePath);
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await service.AddAsync(CreateRecord(start.AddDays(1)));
        await service.AddAsync(CreateRecord(start.AddDays(3)));
        await service.AddAsync(CreateRecord(start.AddDays(2)));

        var all = await service.GetAllAsync();

        CollectionAssert.AreEqual(
            new[] { start.AddDays(3), start.AddDays(2), start.AddDays(1) },
            all.Select(r => r.PreparedAt).ToArray());
    }

    [TestMethod]
    public async Task Add_BeyondCap_DropsOldestEntries()
    {
        PurchaseHistoryService service = new(FilePath, maxEntries: 3);
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < 5; i++)
        {
            await service.AddAsync(CreateRecord(start.AddHours(i)));
        }

        var all = await new PurchaseHistoryService(FilePath, maxEntries: 3).GetAllAsync();

        CollectionAssert.AreEqual(
            new[] { start.AddHours(4), start.AddHours(3), start.AddHours(2) },
            all.Select(r => r.PreparedAt).ToArray());
    }

    [TestMethod]
    public void DefaultCap_Is500() => Assert.AreEqual(500, PurchaseHistoryService.DefaultMaxEntries);

    [TestMethod]
    public async Task Delete_RemovesOnlyThatEntry()
    {
        PurchaseHistoryService service = new(FilePath);
        var keep = CreateRecord(DateTimeOffset.UtcNow);
        var remove = CreateRecord(DateTimeOffset.UtcNow.AddMinutes(-5));
        await service.AddAsync(keep);
        await service.AddAsync(remove);

        var removed = await service.DeleteAsync(remove.Id);
        var all = await new PurchaseHistoryService(FilePath).GetAllAsync();

        Assert.IsTrue(removed);
        Assert.AreEqual(1, all.Count);
        Assert.AreEqual(keep.Id, all[0].Id);
    }

    [TestMethod]
    public async Task Delete_UnknownId_ReturnsFalse()
    {
        PurchaseHistoryService service = new(FilePath);
        await service.AddAsync(CreateRecord(DateTimeOffset.UtcNow));

        Assert.IsFalse(await service.DeleteAsync(Guid.NewGuid()));
    }

    [TestMethod]
    public async Task Clear_RemovesEverything()
    {
        PurchaseHistoryService service = new(FilePath);
        await service.AddAsync(CreateRecord(DateTimeOffset.UtcNow));
        await service.AddAsync(CreateRecord(DateTimeOffset.UtcNow));

        await service.ClearAsync();
        var all = await new PurchaseHistoryService(FilePath).GetAllAsync();

        Assert.AreEqual(0, all.Count);
    }

    [TestMethod]
    public async Task CorruptFile_IsTreatedAsEmpty_AndRecoversOnNextWrite()
    {
        await File.WriteAllTextAsync(FilePath, "{ this is not json ]");
        PurchaseHistoryService service = new(FilePath);

        Assert.AreEqual(0, (await service.GetAllAsync()).Count);

        await service.AddAsync(CreateRecord(DateTimeOffset.UtcNow));
        var all = await new PurchaseHistoryService(FilePath).GetAllAsync();

        Assert.AreEqual(1, all.Count);
    }

    [TestMethod]
    public async Task CorruptFile_IsBackedUpBeforeOverwrite()
    {
        await File.WriteAllTextAsync(FilePath, "garbage");
        PurchaseHistoryService service = new(FilePath);

        await service.AddAsync(CreateRecord(DateTimeOffset.UtcNow));

        Assert.IsTrue(File.Exists(FilePath + ".corrupt"));
        Assert.AreEqual("garbage", await File.ReadAllTextAsync(FilePath + ".corrupt"));
    }

    [TestMethod]
    public async Task Changed_IsRaisedOnEveryMutation()
    {
        PurchaseHistoryService service = new(FilePath);
        var count = 0;
        service.Changed += (_, _) => count++;
        var record = CreateRecord(DateTimeOffset.UtcNow);

        await service.AddAsync(record);
        await service.DeleteAsync(record.Id);
        await service.ClearAsync();

        Assert.AreEqual(3, count);
    }

    [TestMethod]
    public async Task ConcurrentAdds_AreAllKept()
    {
        PurchaseHistoryService service = new(FilePath);

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => service.AddAsync(CreateRecord(DateTimeOffset.UtcNow))));
        var all = await new PurchaseHistoryService(FilePath).GetAllAsync();

        Assert.AreEqual(20, all.Count);
    }

    [TestMethod]
    public async Task Add_CreatesMissingDirectory()
    {
        var nestedPath = Path.Combine(_directory, "nested", "history.json");
        PurchaseHistoryService service = new(nestedPath);

        await service.AddAsync(CreateRecord(DateTimeOffset.UtcNow));

        Assert.IsTrue(File.Exists(nestedPath));
    }

    internal static PurchaseRecord CreateRecord(DateTimeOffset preparedAt, int price = 42) => new()
    {
        Id = Guid.NewGuid(),
        CityId = "PHA",
        CityName = "Praha",
        TicketId = "PHA30min",
        TimeText = "30 min",
        Price = price,
        PreparedAt = preparedAt,
    };
}
