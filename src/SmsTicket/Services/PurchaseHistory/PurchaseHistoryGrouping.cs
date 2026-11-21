using SmsTicket.Data.Models;

namespace SmsTicket.Services.PurchaseHistory;

public sealed record PurchaseMonthGroup(int Year, int Month, int TotalPrice, IReadOnlyList<PurchaseRecord> Items);

public static class PurchaseHistoryGrouping
{
    /// <summary>Groups records by calendar month in <paramref name="timeZone"/>, newest month and record first.</summary>
    public static IReadOnlyList<PurchaseMonthGroup> GroupByMonth(IEnumerable<PurchaseRecord> records, TimeZoneInfo timeZone) =>
        records
            .Select(r => (Record: r, Local: TimeZoneInfo.ConvertTime(r.PreparedAt, timeZone)))
            .GroupBy(x => (x.Local.Year, x.Local.Month))
            .OrderByDescending(g => g.Key.Year)
            .ThenByDescending(g => g.Key.Month)
            .Select(g => new PurchaseMonthGroup(
                g.Key.Year,
                g.Key.Month,
                g.Sum(x => x.Record.Price),
                g.OrderByDescending(x => x.Record.PreparedAt).Select(x => x.Record).ToList()))
            .ToList();
}
