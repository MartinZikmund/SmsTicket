using SmsTicket.Data.Models;

namespace SmsTicket.Services.PurchaseHistory;

public interface IPurchaseHistoryService
{
    /// <summary>Raised after any change. May fire on a background thread.</summary>
    event EventHandler? Changed;

    Task AddAsync(PurchaseRecord record);

    /// <summary>Returns all records, newest first.</summary>
    Task<IReadOnlyList<PurchaseRecord>> GetAllAsync();

    Task<bool> DeleteAsync(Guid id);

    /// <summary>Replaces the record with <paramref name="id"/> by the result of <paramref name="update"/>. The id must not change.</summary>
    Task<bool> UpdateAsync(Guid id, Func<PurchaseRecord, PurchaseRecord> update);

    Task ClearAsync();
}
