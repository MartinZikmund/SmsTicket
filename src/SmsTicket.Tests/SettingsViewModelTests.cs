using SmsTicket.Services.Ads;
using SmsTicket.Services.AppSettings;
using SmsTicket.ViewModels;

namespace SmsTicket.Tests;

[TestClass]
public class SettingsViewModelTests
{
    private readonly FakeSettingsService _store = new();
    private readonly List<ThemePreference> _appliedThemes = new();
    private readonly List<string> _appliedLanguages = new();

    private SettingsViewModel CreateViewModel(LanguagePreference startupLanguage = LanguagePreference.System) =>
        new(new AppSettings(_store), startupLanguage, _appliedThemes.Add, _appliedLanguages.Add);

    [TestMethod]
    public void InitialIndexes_ReflectStoredSettings()
    {
        _ = new AppSettings(_store) { Theme = ThemePreference.Dark, Language = LanguagePreference.English };

        var viewModel = CreateViewModel(LanguagePreference.English);

        Assert.AreEqual((int)ThemePreference.Dark, viewModel.ThemeIndex);
        Assert.AreEqual((int)LanguagePreference.English, viewModel.LanguageIndex);
        Assert.IsFalse(viewModel.IsRestartRequired);
    }

    [TestMethod]
    public void ChangingTheme_PersistsAndApplies()
    {
        var viewModel = CreateViewModel();

        viewModel.ThemeIndex = (int)ThemePreference.Light;

        Assert.AreEqual(ThemePreference.Light, new AppSettings(_store).Theme);
        CollectionAssert.AreEqual(new[] { ThemePreference.Light }, _appliedThemes);
    }

    [TestMethod]
    public void ChangingLanguage_PersistsAppliesAndRequiresRestart()
    {
        var viewModel = CreateViewModel();

        viewModel.LanguageIndex = (int)LanguagePreference.Czech;

        Assert.AreEqual(LanguagePreference.Czech, new AppSettings(_store).Language);
        CollectionAssert.AreEqual(new[] { "cs" }, _appliedLanguages);
        Assert.IsTrue(viewModel.IsRestartRequired);
    }

    [TestMethod]
    public void RevertingToStartupLanguage_ClearsRestartRequired()
    {
        _ = new AppSettings(_store) { Language = LanguagePreference.English };
        var viewModel = CreateViewModel(LanguagePreference.English);

        viewModel.LanguageIndex = (int)LanguagePreference.Czech;
        viewModel.LanguageIndex = (int)LanguagePreference.English;

        Assert.IsFalse(viewModel.IsRestartRequired);
    }

    [TestMethod]
    public void RestartRequired_SurvivesNewViewModelInstance()
    {
        CreateViewModel().LanguageIndex = (int)LanguagePreference.English;

        Assert.IsTrue(CreateViewModel().IsRestartRequired);
    }

    [TestMethod]
    public void InvalidIndex_IsIgnored()
    {
        var viewModel = CreateViewModel();

        viewModel.ThemeIndex = -1;
        viewModel.LanguageIndex = 7;

        Assert.AreEqual(0, _appliedThemes.Count);
        Assert.AreEqual(0, _appliedLanguages.Count);
        Assert.AreEqual(ThemePreference.System, new AppSettings(_store).Theme);
    }

    [TestMethod]
    public void ResetTips_RestoresFirstTimeUse()
    {
        _ = new AppSettings(_store) { FirstTimeUse = false };
        var viewModel = CreateViewModel();
        Assert.IsTrue(viewModel.ResetTipsCommand.CanExecute(null));

        viewModel.ResetTipsCommand.Execute(null);

        Assert.IsTrue(new AppSettings(_store).FirstTimeUse);
        Assert.IsFalse(viewModel.ResetTipsCommand.CanExecute(null));
    }
}

[TestClass]
public class SettingsViewModelAdPrivacyTests
{
    private sealed class FakeAdPrivacyOptions : IAdPrivacyOptions
    {
        public bool IsRequired { get; set; }

        public int ShowCount { get; private set; }

        public event EventHandler? Changed;

        public void Show() => ShowCount++;

        public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
    }

    private static SettingsViewModel CreateViewModel(IAdPrivacyOptions? adPrivacyOptions) =>
        new(new AppSettings(new FakeSettingsService()), LanguagePreference.System, _ => { }, _ => { }, adPrivacyOptions);

    [TestMethod]
    public void WithoutAds_PrivacyOptionsStayHidden()
    {
        var viewModel = CreateViewModel(null);

        Assert.IsFalse(viewModel.IsAdPrivacyOptionsVisible);
        viewModel.ShowAdPrivacyOptionsCommand.Execute(null);
    }

    [TestMethod]
    public void PrivacyOptionsNotRequired_StayHidden()
    {
        var viewModel = CreateViewModel(new FakeAdPrivacyOptions { IsRequired = false });

        Assert.IsFalse(viewModel.IsAdPrivacyOptionsVisible);
    }

    [TestMethod]
    public void PrivacyOptionsBecomeRequired_ShowsRowAndNotifies()
    {
        FakeAdPrivacyOptions options = new();
        var viewModel = CreateViewModel(options);
        List<string?> changed = new();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        options.IsRequired = true;
        options.RaiseChanged();

        Assert.IsTrue(viewModel.IsAdPrivacyOptionsVisible);
        CollectionAssert.Contains(changed, nameof(SettingsViewModel.IsAdPrivacyOptionsVisible));
    }

    [TestMethod]
    public void ShowCommand_OpensPrivacyOptionsForm()
    {
        FakeAdPrivacyOptions options = new() { IsRequired = true };
        var viewModel = CreateViewModel(options);

        viewModel.ShowAdPrivacyOptionsCommand.Execute(null);

        Assert.AreEqual(1, options.ShowCount);
    }
}
