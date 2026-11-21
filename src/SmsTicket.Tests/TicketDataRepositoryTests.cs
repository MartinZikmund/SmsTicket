using System.Net;
using System.Text;
using SmsTicket.Data;

namespace SmsTicket.Tests;

[TestClass]
public class TicketDataRepositoryTests
{
    private static readonly Uri RemoteUri = new("https://example.test/tickets.json");

    private string _folder = null!;

    private string CachePath => Path.Combine(_folder, "cache", "tickets.json");

    [TestInitialize]
    public void Setup()
    {
        _folder = Path.Combine(Path.GetTempPath(), "SmsTicketTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_folder, recursive: true);

    [TestMethod]
    public void LoadCurrent_NoCache_UsesBundled()
    {
        var repository = CreateRepository(bundledVersion: "2026-01-01");

        Assert.AreEqual(new DateOnly(2026, 1, 1), repository.LoadCurrent().DataVersion);
    }

    [TestMethod]
    public void LoadCurrent_NewerCache_UsesCache()
    {
        WriteCache(TicketDataJson.Build(dataVersion: "2026-06-01"));

        Assert.AreEqual(new DateOnly(2026, 6, 1), CreateRepository(bundledVersion: "2026-01-01").LoadCurrent().DataVersion);
    }

    [TestMethod]
    public void LoadCurrent_OlderCache_UsesBundled()
    {
        // e.g. an app update shipped fresher data than the last download
        WriteCache(TicketDataJson.Build(dataVersion: "2025-01-01"));

        Assert.AreEqual(new DateOnly(2026, 1, 1), CreateRepository(bundledVersion: "2026-01-01").LoadCurrent().DataVersion);
    }

    [TestMethod]
    [DataRow("{ broken")]
    [DataRow("")]
    public void LoadCurrent_CorruptCache_UsesBundled(string cache)
    {
        WriteCache(cache);

        Assert.AreEqual(new DateOnly(2026, 1, 1), CreateRepository(bundledVersion: "2026-01-01").LoadCurrent().DataVersion);
    }

    [TestMethod]
    public void LoadCurrent_InvalidNewerCache_UsesBundled()
    {
        WriteCache(TicketDataJson.Build(dataVersion: "2027-01-01", cities: TicketDataJson.City(defaultTicketId: "Missing")));

        Assert.AreEqual(new DateOnly(2026, 1, 1), CreateRepository(bundledVersion: "2026-01-01").LoadCurrent().DataVersion);
    }

    [TestMethod]
    public void LoadCurrent_NoCachePath_UsesBundled() =>
        Assert.AreEqual(
            new DateOnly(2026, 1, 1),
            new TicketDataRepository(Bundled("2026-01-01"), cachePath: null, new HttpClient(new FakeHandler()), RemoteUri).LoadCurrent().DataVersion);

    [TestMethod]
    public async Task Refresh_NewerRemote_IsCachedAndUsedNextTime()
    {
        FakeHandler handler = new() { Response = () => Ok(TicketDataJson.Build(dataVersion: "2026-06-01")) };
        var repository = CreateRepository("2026-01-01", handler);

        var refreshed = await repository.RefreshAsync();

        Assert.AreEqual(new DateOnly(2026, 6, 1), refreshed?.DataVersion);
        Assert.AreEqual(RemoteUri, handler.LastRequestUri);
        Assert.IsTrue(File.Exists(CachePath));
        Assert.AreEqual(new DateOnly(2026, 6, 1), CreateRepository("2026-01-01").LoadCurrent().DataVersion);
    }

    [TestMethod]
    [DataRow("2026-01-01")]
    [DataRow("2025-01-01")]
    public async Task Refresh_RemoteNotNewer_DoesNotTouchCache(string remoteVersion)
    {
        FakeHandler handler = new() { Response = () => Ok(TicketDataJson.Build(dataVersion: remoteVersion)) };

        Assert.IsNull(await CreateRepository("2026-01-01", handler).RefreshAsync());
        Assert.IsFalse(File.Exists(CachePath));
    }

    [TestMethod]
    public async Task Refresh_InvalidRemote_KeepsCurrentCache()
    {
        WriteCache(TicketDataJson.Build(dataVersion: "2026-03-01"));
        FakeHandler handler = new()
        {
            Response = () => Ok(TicketDataJson.Build(dataVersion: "2026-06-01", cities: TicketDataJson.City(defaultTicketId: "Missing"))),
        };
        var repository = CreateRepository("2026-01-01", handler);

        Assert.IsNull(await repository.RefreshAsync());
        Assert.AreEqual(new DateOnly(2026, 3, 1), repository.LoadCurrent().DataVersion);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task Refresh_HttpError_ReturnsNull(HttpStatusCode status)
    {
        FakeHandler handler = new() { Response = () => new HttpResponseMessage(status) { Content = new StringContent(TicketDataJson.Build(dataVersion: "2027-01-01")) } };

        Assert.IsNull(await CreateRepository("2026-01-01", handler).RefreshAsync());
        Assert.IsFalse(File.Exists(CachePath));
    }

    [TestMethod]
    public async Task Refresh_Offline_ReturnsNull()
    {
        FakeHandler handler = new() { Response = () => throw new HttpRequestException("No route to host") };

        Assert.IsNull(await CreateRepository("2026-01-01", handler).RefreshAsync());
    }

    [TestMethod]
    public async Task Refresh_MalformedJson_ReturnsNull()
    {
        FakeHandler handler = new() { Response = () => Ok("<html>Rate limited</html>") };

        Assert.IsNull(await CreateRepository("2026-01-01", handler).RefreshAsync());
        Assert.IsFalse(File.Exists(CachePath));
    }

    [TestMethod]
    public async Task Refresh_Timeout_ReturnsNull()
    {
        FakeHandler handler = new() { Delay = TimeSpan.FromSeconds(30), Response = () => Ok(TicketDataJson.Build(dataVersion: "2027-01-01")) };
        var repository = new TicketDataRepository(Bundled("2026-01-01"), CachePath, new HttpClient(handler), RemoteUri, TimeSpan.FromMilliseconds(100));

        var started = DateTime.UtcNow;
        Assert.IsNull(await repository.RefreshAsync());
        Assert.IsTrue(DateTime.UtcNow - started < TimeSpan.FromSeconds(10));
    }

    private TicketDataRepository CreateRepository(string bundledVersion, FakeHandler? handler = null) =>
        new(Bundled(bundledVersion), CachePath, new HttpClient(handler ?? new FakeHandler()), RemoteUri);

    private static Func<Stream> Bundled(string version) =>
        () => new MemoryStream(Encoding.UTF8.GetBytes(TicketDataJson.Build(dataVersion: version)));

    private void WriteCache(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
        File.WriteAllText(CachePath, json);
    }

    private static HttpResponseMessage Ok(string content) => new(HttpStatusCode.OK) { Content = new StringContent(content) };

    private sealed class FakeHandler : HttpMessageHandler
    {
        public Func<HttpResponseMessage> Response { get; init; } = () => new HttpResponseMessage(HttpStatusCode.NotFound);

        public TimeSpan Delay { get; init; }

        public Uri? LastRequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            if (Delay > TimeSpan.Zero)
            {
                await Task.Delay(Delay, cancellationToken);
            }

            return Response();
        }
    }
}
