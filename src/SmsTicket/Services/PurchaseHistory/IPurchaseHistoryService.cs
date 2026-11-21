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

    Task ClearAsync();
}
