using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.AppSettings;
using SmsTicket.Services.Location;
using SmsTicket.ViewModels;

namespace SmsTicket.Tests;

[TestClass]
public class NearestCityViewModelTests
{
    private static readonly City Prague = DataSource.Cities.Single(c => c.Id == "PHA");
    private static readonly City Brno = DataSource.Cities.Single(c => c.Id == "BR");

    private readonly AppSettings _appSettings = new(new InMemorySettingsService());
    private City _selectedCity = Prague;

    private NearestCityViewModel CreateViewModel(ILocationProvider provider, TimeSpan? timeout = null) =>
        new(provider, _appSettings, DataSource.Cities, () => _selectedCity, city => _selectedCity = city, timeout);

    private static FakeLocationProvider AtBrno() => FakeLocationProvider.At(49.19, 16.61);

    private static FakeLocationProvider NeverCompleting() =>
        new(_ => new TaskCompletionSource<LocationResult>().Task);

    private static FakeLocationProvider Throwing() =>
        new(_ => throw new InvalidOperationException("boom"));

    [TestMethod]
    public void IsSupported_FollowsProvider()
    {
        Assert.IsTrue(CreateViewModel(AtBrno()).IsSupported);
        Assert.IsFalse(CreateViewModel(FakeLocationProvider.Returning(LocationResult.Unsupported, isSupported: false)).IsSupported);
    }

    [TestMethod]
    public async Task UseMyLocation_SelectsNearestCity_AndAllowsPrompt()
    {
        var provider = AtBrno();
        var viewModel = CreateViewModel(provider);

        await viewModel.UseMyLocationAsync();

        Assert.AreSame(Brno, _selectedCity);
        Assert.AreEqual(NearestCityStatus.Found, viewModel.Status);
        CollectionAssert.AreEqual(new[] { true }, provider.Calls);
    }

    [TestMethod]
    public async Task UseMyLocation_Success_OffersAutoSelect()
    {
        var viewModel = CreateViewModel(AtBrno());
        Assert.IsFalse(viewModel.CanOfferAutoSelect);

        await viewModel.UseMyLocationAsync();

        Assert.IsTrue(viewModel.CanOfferAutoSelect);
        Assert.IsTrue(_appSettings.HasUsedNearestCity);
        Assert.IsFalse(viewModel.AutoSelectOnLaunch, "Auto-select must stay opt-in");
    }

    [TestMethod]
    public async Task UseMyLocation_FarAway_KeepsCity_AndReportsNoneNearby()
    {
        var viewModel = CreateViewModel(FakeLocationProvider.At(49.3961, 15.5912));

        await viewModel.UseMyLocationAsync();

        Assert.AreSame(Prague, _selectedCity);
        Assert.AreEqual(NearestCityStatus.NoneNearby, viewModel.Status);
        Assert.IsFalse(viewModel.CanOfferAutoSelect);
    }

    [TestMethod]
    public async Task UseMyLocation_Denied_KeepsCity()
    {
        var viewModel = CreateViewModel(FakeLocationProvider.Returning(LocationResult.Denied));

        await viewModel.UseMyLocationAsync();

        Assert.AreSame(Prague, _selectedCity);
        Assert.AreEqual(NearestCityStatus.Denied, viewModel.Status);
    }

    [TestMethod]
    public async Task UseMyLocation_Unavailable_KeepsCity()
    {
        var viewModel = CreateViewModel(FakeLocationProvider.Returning(LocationResult.Unavailable));

        await viewModel.UseMyLocationAsync();

        Assert.AreSame(Prague, _selectedCity);
        Assert.AreEqual(NearestCityStatus.Unavailable, viewModel.Status);
    }

    [TestMethod]
    public async Task UseMyLocation_ProviderThrows_ReportsUnavailable()
    {
        var viewModel = CreateViewModel(Throwing());

        await viewModel.UseMyLocationAsync();

        Assert.AreSame(Prague, _selectedCity);
        Assert.AreEqual(NearestCityStatus.Unavailable, viewModel.Status);
    }

    [TestMethod]
    public async Task UseMyLocation_Timeout_ReportsUnavailable()
    {
        var viewModel = CreateViewModel(NeverCompleting(), TimeSpan.FromMilliseconds(50));

        await viewModel.UseMyLocationAsync();

        Assert.AreSame(Prague, _selectedCity);
        Assert.AreEqual(NearestCityStatus.Unavailable, viewModel.Status);
        Assert.IsFalse(viewModel.IsBusy);
    }

    [TestMethod]
    public async Task UseMyLocation_UnsupportedAtRuntime_HidesAction()
    {
        var viewModel = CreateViewModel(FakeLocationProvider.Returning(LocationResult.Unsupported));

        await viewModel.UseMyLocationAsync();

        Assert.IsFalse(viewModel.IsSupported);
        Assert.AreSame(Prague, _selectedCity);
    }

    [TestMethod]
    public async Task UseMyLocation_IsBusyWhileSearching()
    {
        TaskCompletionSource<LocationResult> pending = new();
        var viewModel = CreateViewModel(new FakeLocationProvider(_ => pending.Task));

        var task = viewModel.UseMyLocationAsync();
        Assert.IsTrue(viewModel.IsBusy);
        Assert.AreEqual(NearestCityStatus.Searching, viewModel.Status);

        pending.SetResult(LocationResult.Success(new(49.19, 16.61)));
        await task;
        Assert.IsFalse(viewModel.IsBusy);
    }

    [TestMethod]
    public async Task ClearStatus_HidesPreviousMessage()
    {
        var viewModel = CreateViewModel(FakeLocationProvider.Returning(LocationResult.Denied));
        await viewModel.UseMyLocationAsync();
        Assert.IsTrue(viewModel.HasStatusMessage);

        viewModel.ClearStatus();

        Assert.AreEqual(NearestCityStatus.None, viewModel.Status);
        Assert.IsFalse(viewModel.HasStatusMessage);
    }

    [TestMethod]
    public void ClearStatus_WhileSearching_KeepsSearching()
    {
        var viewModel = CreateViewModel(NeverCompleting());
        _ = viewModel.UseMyLocationAsync();

        viewModel.ClearStatus();

        Assert.IsTrue(viewModel.IsBusy);
    }

    [TestMethod]
    public void AutoSelectOnLaunch_IsPersisted()
    {
        var viewModel = CreateViewModel(AtBrno());

        viewModel.AutoSelectOnLaunch = true;

        Assert.IsTrue(_appSettings.AutoSelectNearestCity);
        Assert.IsTrue(CreateViewModel(AtBrno()).AutoSelectOnLaunch);
    }

    [TestMethod]
    public async Task Launch_Disabled_DoesNotTouchLocation()
    {
        var provider = AtBrno();
        var viewModel = CreateViewModel(provider);

        await viewModel.AutoSelectOnLaunchAsync();

        Assert.IsEmpty(provider.Calls);
        Assert.AreSame(Prague, _selectedCity);
    }

    [TestMethod]
    public async Task Launch_Enabled_SelectsNearestCity_WithoutPrompt()
    {
        _appSettings.AutoSelectNearestCity = true;
        var provider = AtBrno();
        var viewModel = CreateViewModel(provider);

        await viewModel.AutoSelectOnLaunchAsync();

        Assert.AreSame(Brno, _selectedCity);
        CollectionAssert.AreEqual(new[] { false }, provider.Calls);
        Assert.AreEqual(NearestCityStatus.None, viewModel.Status, "Launch auto-select is silent");
    }

    [TestMethod]
    [DataRow(LocationStatus.Denied)]
    [DataRow(LocationStatus.Unavailable)]
    [DataRow(LocationStatus.Unsupported)]
    public async Task Launch_Enabled_LocationFails_KeepsLastCitySilently(LocationStatus status)
    {
        _appSettings.AutoSelectNearestCity = true;
        var viewModel = CreateViewModel(FakeLocationProvider.Returning(new LocationResult(status)));

        await viewModel.AutoSelectOnLaunchAsync();

        Assert.AreSame(Prague, _selectedCity);
        Assert.AreEqual(NearestCityStatus.None, viewModel.Status);
    }

    [TestMethod]
    public async Task Launch_Enabled_Timeout_KeepsLastCity()
    {
        _appSettings.AutoSelectNearestCity = true;
        var viewModel = CreateViewModel(NeverCompleting(), TimeSpan.FromMilliseconds(50));

        await viewModel.AutoSelectOnLaunchAsync();

        Assert.AreSame(Prague, _selectedCity);
        Assert.AreEqual(NearestCityStatus.None, viewModel.Status);
    }

    [TestMethod]
    public async Task Launch_Enabled_ProviderThrows_KeepsLastCity()
    {
        _appSettings.AutoSelectNearestCity = true;
        var viewModel = CreateViewModel(Throwing());

        await viewModel.AutoSelectOnLaunchAsync();

        Assert.AreSame(Prague, _selectedCity);
    }

    [TestMethod]
    public async Task Launch_Enabled_FarAway_KeepsLastCity()
    {
        _appSettings.AutoSelectNearestCity = true;
        var viewModel = CreateViewModel(FakeLocationProvider.At(48.2082, 16.3738));

        await viewModel.AutoSelectOnLaunchAsync();

        Assert.AreSame(Prague, _selectedCity);
        Assert.AreEqual(NearestCityStatus.None, viewModel.Status);
    }

    [TestMethod]
    public async Task Launch_Enabled_UnsupportedProvider_IsNotCalled()
    {
        _appSettings.AutoSelectNearestCity = true;
        var provider = FakeLocationProvider.Returning(LocationResult.Unsupported, isSupported: false);
        var viewModel = CreateViewModel(provider);

        await viewModel.AutoSelectOnLaunchAsync();

        Assert.IsEmpty(provider.Calls);
        Assert.AreSame(Prague, _selectedCity);
    }

    [TestMethod]
    public async Task Launch_Enabled_UserPickedCityMeanwhile_KeepsUserChoice()
    {
        _appSettings.AutoSelectNearestCity = true;
        var olomouc = DataSource.Cities.Single(c => c.Id == "OL");
        TaskCompletionSource<LocationResult> pending = new();
        var viewModel = CreateViewModel(new FakeLocationProvider(_ => pending.Task));

        var task = viewModel.AutoSelectOnLaunchAsync();
        _selectedCity = olomouc;
        pending.SetResult(LocationResult.Success(new(49.19, 16.61)));
        await task;

        Assert.AreSame(olomouc, _selectedCity);
    }
}
