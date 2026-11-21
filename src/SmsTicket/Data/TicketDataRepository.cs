namespace SmsTicket.Data;

/// <summary>
/// Picks the newest valid ticket data (bundled vs. cached download) and refreshes the cache from the web.
/// </summary>
public sealed class TicketDataRepository
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private const int MaxDownloadLength = 1024 * 1024;

    private readonly Func<Stream> _openBundled;
    private readonly string? _cachePath;
    private readonly HttpClient _httpClient;
    private readonly Uri _remoteUri;
    private readonly TimeSpan _timeout;

    public TicketDataRepository(Func<Stream> openBundled, string? cachePath, HttpClient httpClient, Uri remoteUri, TimeSpan? timeout = null)
    {
        _openBundled = openBundled;
        _cachePath = cachePath;
        _httpClient = httpClient;
        _remoteUri = remoteUri;
        _timeout = timeout ?? DefaultTimeout;
    }

    public TicketDataDocument LoadCurrent()
    {
        var bundled = TryLoad(ReadBundled);
        var cached = _cachePath is null ? null : TryLoad(() => File.ReadAllText(_cachePath));

        return (bundled, cached) switch
        {
            ({ } b, { } c) => c.DataVersion > b.DataVersion ? c : b,
            _ => bundled ?? cached ?? throw new InvalidOperationException("Bundled ticket data is invalid"),
        };
    }

    /// <summary>
    /// Downloads the remote data and caches it when it is valid and newer. Never throws.
    /// </summary>
    /// <returns>The newly cached data, or null when nothing changed.</returns>
    public async Task<TicketDataDocument?> RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_cachePath is null)
        {
            return null;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_timeout);

            using var response = await _httpClient.GetAsync(_remoteUri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaxDownloadLength)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(timeout.Token);
            if (json.Length > MaxDownloadLength ||
                !TicketDataParser.TryParse(json, out var remote) ||
                remote.DataVersion <= LoadCurrent().DataVersion)
            {
                return null;
            }

            WriteCache(json);
            return remote;
        }
        catch (Exception)
        {
            // Offline, timeout, IO error... the current data stays in use
            return null;
        }
    }

    private string ReadBundled()
    {
        using var stream = _openBundled();
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    private void WriteCache(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_cachePath)!);

        // Write aside first so a crash mid-write cannot leave a truncated cache
        var temp = _cachePath + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, _cachePath!, overwrite: true);
    }

    private static TicketDataDocument? TryLoad(Func<string> read)
    {
        try
        {
            return TicketDataParser.TryParse(read(), out var document) ? document : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
