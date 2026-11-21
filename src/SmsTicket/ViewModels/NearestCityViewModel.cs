using SmsTicket.Data.Models;
using SmsTicket.Services.AppSettings;
using SmsTicket.Services.Localizer;
using SmsTicket.Services.Location;

namespace SmsTicket.ViewModels;

public enum NearestCityStatus
{
    None,
    Searching,
    Found,
    NoneNearby,
    Denied,
    Unavailable,
}

public class NearestCityViewModel : ObservableObject
{
    // Safety net on top of the provider's own position timeout, so a stuck platform call never spins forever
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    private readonly ILocationProvider _locationProvider;
    private readonly IAppSettings _appSettings;
    private readonly IReadOnlyList<City> _cities;
    private readonly Func<City> _getSelectedCity;
    private readonly Action<City> _selectCity;
    private readonly TimeSpan _timeout;
    private readonly LocalizationService _localizer = new();

    private bool _isSupported;
    private NearestCityStatus _status;
    private City? _foundCity;

    public NearestCityViewModel(
        ILocationProvider locationProvider,
        IAppSettings appSettings,
        IReadOnlyList<City> cities,
        Func<City> getSelectedCity,
        Action<City> selectCity,
        TimeSpan? timeout = null)
    {
        _locationProvider = locationProvider;
        _appSettings = appSettings;
        _cities = cities;
        _getSelectedCity = getSelectedCity;
        _selectCity = selectCity;
        _timeout = timeout ?? DefaultTimeout;
        _isSupported = locationProvider.IsSupported;

        UseMyLocationCommand = new AsyncRelayCommand(UseMyLocationAsync);
    }

    public IAsyncRelayCommand UseMyLocationCommand { get; }

    public bool IsSupported
    {
        get => _isSupported;
        private set
        {
            if (SetProperty(ref _isSupported, value))
            {
                OnPropertyChanged(nameof(CanOfferAutoSelect));
            }
        }
    }

    public NearestCityStatus Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(StatusMessage));
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    public bool IsBusy => Status == NearestCityStatus.Searching;

    public bool HasStatusMessage => Status is not (NearestCityStatus.None or NearestCityStatus.Searching);

    public string StatusMessage => Status switch
    {
        NearestCityStatus.Found => string.Format(_localizer.NearestCityFound, _foundCity?.Name),
        NearestCityStatus.NoneNearby => _localizer.NearestCityNoneNearby,
        NearestCityStatus.Denied => _localizer.NearestCityDenied,
        NearestCityStatus.Unavailable => _localizer.NearestCityUnavailable,
        _ => "",
    };

    public bool CanOfferAutoSelect => IsSupported && _appSettings.HasUsedNearestCity;

    public bool AutoSelectOnLaunch
    {
        get => _appSettings.AutoSelectNearestCity;
        set
        {
            if (_appSettings.AutoSelectNearestCity != value)
            {
                _appSettings.AutoSelectNearestCity = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>User-initiated lookup, may show the system permission prompt.</summary>
    public async Task UseMyLocationAsync()
    {
        if (IsBusy)
        {
            return;
        }

        Status = NearestCityStatus.Searching;
        var result = await GetLocationAsync(allowPrompt: true);

        if (result.Status == LocationStatus.Unsupported)
        {
            IsSupported = false;
            Status = NearestCityStatus.None;
            return;
        }

        if (result is not { Status: LocationStatus.Success, Position: { } position })
        {
            Status = result.Status == LocationStatus.Denied ? NearestCityStatus.Denied : NearestCityStatus.Unavailable;
            return;
        }

        if (NearestCityFinder.FindNearest(_cities, position) is not { } city)
        {
            Status = NearestCityStatus.NoneNearby;
            return;
        }

        _foundCity = city;
        _selectCity(city);
        _appSettings.HasUsedNearestCity = true;
        OnPropertyChanged(nameof(CanOfferAutoSelect));
        Status = NearestCityStatus.Found;
    }

    public void ClearStatus()
    {
        if (!IsBusy)
        {
            Status = NearestCityStatus.None;
        }
    }

    /// <summary>Silent launch-time lookup: never prompts and keeps the current city on any failure.</summary>
    public async Task AutoSelectOnLaunchAsync()
    {
        if (!AutoSelectOnLaunch || !IsSupported)
        {
            return;
        }

        var cityAtStart = _getSelectedCity();
        var result = await GetLocationAsync(allowPrompt: false);

        if (result is { Status: LocationStatus.Success, Position: { } position } &&
            NearestCityFinder.FindNearest(_cities, position) is { } city &&
            ReferenceEquals(_getSelectedCity(), cityAtStart))
        {
            _selectCity(city);
        }
    }

    private async Task<LocationResult> GetLocationAsync(bool allowPrompt)
    {
        using CancellationTokenSource cts = new(_timeout);
        try
        {
            return await _locationProvider.GetLocationAsync(allowPrompt, cts.Token).WaitAsync(cts.Token);
        }
        catch (Exception)
        {
            // Timeouts and platform failures are all "no location" to the user
            return LocationResult.Unavailable;
        }
    }
}
