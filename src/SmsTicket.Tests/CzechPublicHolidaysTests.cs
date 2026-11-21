using SmsTicket.Data;

namespace SmsTicket.Tests;

[TestClass]
public class CzechPublicHolidaysTests
{
    [TestMethod]
    [DataRow(2024, 3, 31)]
    [DataRow(2025, 4, 20)]
    [DataRow(2026, 4, 5)]
    [DataRow(2027, 3, 28)]
    [DataRow(2038, 4, 25)]
    public void EasterSunday_IsComputed(int year, int month, int day) =>
        Assert.AreEqual(new DateOnly(year, month, day), CzechPublicHolidays.EasterSunday(year));

    [TestMethod]
    [DataRow(2026, 1, 1)]
    [DataRow(2026, 4, 3)] // Good Friday
    [DataRow(2026, 4, 6)] // Easter Monday
    [DataRow(2026, 5, 1)]
    [DataRow(2026, 5, 8)]
    [DataRow(2026, 7, 5)]
    [DataRow(2026, 7, 6)]
    [DataRow(2026, 9, 28)]
    [DataRow(2026, 10, 28)]
    [DataRow(2026, 11, 17)]
    [DataRow(2026, 12, 24)]
    [DataRow(2026, 12, 25)]
    [DataRow(2026, 12, 26)]
    [DataRow(2027, 3, 26)] // Good Friday
    [DataRow(2027, 3, 29)] // Easter Monday
    public void IsHoliday_True(int year, int month, int day) =>
        Assert.IsTrue(CzechPublicHolidays.IsHoliday(new DateOnly(year, month, day)));

    [TestMethod]
    [DataRow(2026, 4, 2)] // Maundy Thursday
    [DataRow(2026, 4, 5)] // Easter Sunday is a Sunday, not a holiday of its own
    [DataRow(2026, 4, 7)]
    [DataRow(2026, 12, 23)]
    [DataRow(2026, 12, 31)]
    [DataRow(2026, 11, 20)]
    public void IsHoliday_False(int year, int month, int day) =>
        Assert.IsFalse(CzechPublicHolidays.IsHoliday(new DateOnly(year, month, day)));

    [TestMethod]
    [DataRow(2026, 11, 20, true)] // Friday
    [DataRow(2026, 11, 21, false)] // Saturday
    [DataRow(2026, 11, 22, false)] // Sunday
    [DataRow(2026, 11, 23, true)] // Monday
    [DataRow(2026, 11, 17, false)] // Tuesday, holiday
    [DataRow(2026, 4, 3, false)] // Good Friday
    public void IsWorkingDay(int year, int month, int day, bool expected) =>
        Assert.AreEqual(expected, CzechPublicHolidays.IsWorkingDay(new DateOnly(year, month, day)));
}
