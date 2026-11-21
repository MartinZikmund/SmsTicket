using System.Diagnostics;
using System.Text.Json;
using SmsTicket.Data.Models;

namespace SmsTicket.Services.PurchaseHistory;

/// <summary>
/// Stores prepared tickets as a JSON file. Loaded once, then served from memory.
/// </summary>
public sealed class PurchaseHistoryService : IPurchaseHistoryService
{
    public const int DefaultMaxEntries = 500;

    private const string FileName = "purchase-history.json";

    private static PurchaseHistoryService? _default;

    private readonly string _filePath;
    private readonly int _maxEntries;
    private readonly SemaphoreSlim _lock = new(1, 1);

    // Newest first
    private List<PurchaseRecord>? _records;
    private bool _backupCorruptFileOnSave;

    public PurchaseHistoryService(string filePath, int maxEntries = DefaultMaxEntries)
    {
        _filePath = filePath;
        _maxEntries = maxEntries;
    }

    public static PurchaseHistoryService Default =>
        _default ??= new(Path.Combine(ApplicationData.Current.LocalFolder.Path, FileName));

    public event EventHandler? Changed;

    public async Task AddAsync(PurchaseRecord record)
    {
        await MutateAsync(records =>
        {
            var index = records.FindIndex(r => r.PreparedAt <= record.PreparedAt);
            records.Insert(index < 0 ? records.Count : index, record);
            if (records.Count > _maxEntries)
            {
                records.RemoveRange(_maxEntries, records.Count - _maxEntries);
            }

            return true;
        });
    }

    public async Task<IReadOnlyList<PurchaseRecord>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return (await LoadAsync()).ToArray();
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task<bool> DeleteAsync(Guid id) => MutateAsync(records => records.RemoveAll(r => r.Id == id) > 0);

    public Task<bool> UpdateAsync(Guid id, Func<PurchaseRecord, PurchaseRecord> update) => MutateAsync(records =>
    {
        var index = records.FindIndex(r => r.Id == id);
        if (index < 0)
        {
            return false;
        }

        records[index] = update(records[index]) with { Id = id };
        return true;
    });

    public async Task ClearAsync() => await MutateAsync(records =>
    {
        records.Clear();
        return true;
    });

    private async Task<bool> MutateAsync(Func<List<PurchaseRecord>, bool> mutation)
    {
        bool changed;
        await _lock.WaitAsync();
        try
        {
            var records = await LoadAsync();
            changed = mutation(records);
            if (changed)
            {
                await SaveAsync(records);
            }
        }
        finally
        {
            _lock.Release();
        }

        if (changed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return changed;
    }

    private async Task<List<PurchaseRecord>> LoadAsync()
    {
        if (_records is not null)
        {
            return _records;
        }

        _records = new();
        if (!File.Exists(_filePath))
        {
            return _records;
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var document = await JsonSerializer.DeserializeAsync(stream, PurchaseHistoryJsonContext.Default.PurchaseHistoryDocument);
            if (document?.Purchases is { } purchases)
            {
                _records.AddRange(purchases.Where(p => p is not null).OrderByDescending(p => p.PreparedAt));
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Trace.WriteLine($"Purchase history could not be read, starting empty: {ex.Message}");
            _backupCorruptFileOnSave = true;
        }

        return _records;
    }

    private async Task SaveAsync(List<PurchaseRecord> records)
    {
        try
        {
            if (Path.GetDirectoryName(_filePath) is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
            }

            if (_backupCorruptFileOnSave && File.Exists(_filePath))
            {
                // Keep the unreadable file around rather than silently destroying it
                File.Copy(_filePath, _filePath + ".corrupt", overwrite: true);
            }

            _backupCorruptFileOnSave = false;

            var tempPath = _filePath + ".tmp";
            await using (var stream = File.Create(tempPath))
            {
                PurchaseHistoryDocument document = new() { Purchases = records };
                await JsonSerializer.SerializeAsync(stream, document, PurchaseHistoryJsonContext.Default.PurchaseHistoryDocument);
            }

            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // History is a convenience; never let storage problems break sending a ticket
            Trace.WriteLine($"Purchase history could not be saved: {ex.Message}");
        }
    }
}
