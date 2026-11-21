using Microsoft.UI.Xaml;
using SmsTicket.Services.AppSettings;

namespace SmsTicket.Tests;

[TestClass]
public class AppSettingsTests
{
    [TestMethod]
    public void Defaults_AreSystemAndFirstTimeUse()
    {
        AppSettings settings = new(new FakeSettingsService());

        Assert.AreEqual(ThemePreference.System, settings.Theme);
        Assert.AreEqual(LanguagePreference.System, settings.Language);
        Assert.IsTrue(settings.FirstTimeUse);
    }

    [TestMethod]
    public void Theme_IsPersisted()
    {
        FakeSettingsService store = new();
        new AppSettings(store).Theme = ThemePreference.Dark;

        Assert.AreEqual(ThemePreference.Dark, new AppSettings(store).Theme);
    }

    [TestMethod]
    public void Language_IsPersisted()
    {
        FakeSettingsService store = new();
        new AppSettings(store).Language = LanguagePreference.English;

        Assert.AreEqual(LanguagePreference.English, new AppSettings(store).Language);
    }

    [TestMethod]
    public void StoredValues_AreParsedFromStrings()
    {
        FakeSettingsService store = new();
        store.Values["Theme"] = "Light";
        store.Values["Language"] = "Czech";
        AppSettings settings = new(store);

        Assert.AreEqual(ThemePreference.Light, settings.Theme);
        Assert.AreEqual(LanguagePreference.Czech, settings.Language);
    }

    [TestMethod]
    [DataRow("Purple")]
    [DataRow("")]
    [DataRow("42")]
    public void UnknownStoredValue_FallsBackToSystem(string stored)
    {
        FakeSettingsService store = new();
        store.Values["Theme"] = stored;
        store.Values["Language"] = stored;
        AppSettings settings = new(store);

        Assert.AreEqual(ThemePreference.System, settings.Theme);
        Assert.AreEqual(LanguagePreference.System, settings.Language);
    }

    [TestMethod]
    [DataRow(ThemePreference.System, ElementTheme.Default)]
    [DataRow(ThemePreference.Light, ElementTheme.Light)]
    [DataRow(ThemePreference.Dark, ElementTheme.Dark)]
    public void Theme_MapsToElementTheme(ThemePreference preference, ElementTheme expected) =>
        Assert.AreEqual(expected, preference.ToElementTheme());

    [TestMethod]
    [DataRow(LanguagePreference.System, "")]
    [DataRow(LanguagePreference.Czech, "cs")]
    [DataRow(LanguagePreference.English, "en")]
    public void Language_MapsToLanguageTag(LanguagePreference preference, string expected) =>
        Assert.AreEqual(expected, preference.ToLanguageTag());
}
