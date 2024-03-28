using System.Collections.ObjectModel;
using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.Localizer;
using SmsTicket.Services.Settings;
using SmsTicket.Services.Sms;
using Windows.UI.StartScreen;
using SmsTicket.Services.AppSettings;
using Windows.System;

namespace SmsTicket.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ISmsService _smsService;
    private readonly IAppSettings _appSettings;
    private readonly LocalizationService _localizer;

    private ICommand _prepareForSendCommand;
    private ICommand _learnMoreCommand;
    private ICommand _togglePinCommand;

    private TicketType _selectedTicketType;
    private JumpList _jumpList;
    private City _selectedCity;

    public MainViewModel(ISettingsService settings, ISmsService smsService)
    {
        _settings = settings;
        _localizer = new LocalizationService();
        _smsService = smsService;
        _appSettings = new AppSettings(_settings);

        Cities = new ObservableCollection<City>(DataSource.Cities);

        var lastCitySelected = _settings.GetSetting("LastCitySelected", () => "PHA", false);
        var targetCity = (from c in Cities where c.Id == lastCitySelected select c).SingleOrDefault();
        if (targetCity != null)
        {
            SelectedCity = targetCity;
        }
    }

    public async void Init()
    {
        if (JumpList.IsSupported())
        {
            IsPinningSupported = true;
            _jumpList = await JumpList.LoadCurrentAsync();
            _jumpList.SystemGroupKind = JumpListSystemGroupKind.None;
            OnPropertyChanged(nameof(IsPinningSupported));
            OnPropertyChanged(nameof(IsPinned));
        }
    }

    public ICommand PrepareForSendCommand => _prepareForSendCommand ??= new AsyncRelayCommand(PrepareForSendAsync);

    public ICommand TogglePinCommand => _togglePinCommand ??= new AsyncRelayCommand(TogglePinAsync);

    public ICommand LearnMoreCommand => _learnMoreCommand ??= new AsyncRelayCommand(async () =>
        await Launcher.LaunchUriAsync(new Uri("https://mzikmund.dev")));

    public ObservableCollection<City> Cities { get; } = new();

    public bool IsPinningSupported { get; private set; } = false;
    
    public XamlRoot? XamlRoot { get; internal set; }

    public City SelectedCity
    {
        get => _selectedCity;
        set
        {
            _selectedCity = value;
            OnPropertyChanged(nameof(SelectedCity));
            if (_selectedCity != null)
            {
                _settings.SetSetting("LastCitySelected", _selectedCity.Id, false);
                var selectedTicketId =
                    _settings.GetSetting<string>("CityTicket" + SelectedCity.Id, () => null, false);
                //select ticket in list
                var ticket =
                    (from t in SelectedCity.TicketTypes
                     where t.Id == (selectedTicketId ?? _selectedCity.DefaultTicketId)
                     select t)
                    .SingleOrDefault();
                if (ticket != null)
                {
                    SelectedTicketType = ticket;
                }
                else
                {
                    SelectedTicketType = _selectedCity.TicketTypes.FirstOrDefault();
                }
            }
        }
    }

    public TicketType SelectedTicketType
    {
        get => _selectedTicketType;
        set
        {
            _selectedTicketType = value;
            OnPropertyChanged(nameof(SelectedTicketType));
            OnPropertyChanged(nameof(IsPinned));
            if (_selectedTicketType != null && SelectedCity != null)
            {
                _settings.SetSetting("CityTicket" + SelectedCity.Id, _selectedTicketType.Id, false);
            }
        }
    }

    public bool IsPinned
    {
        get
        {
            if (_jumpList != null && SelectedTicketType != null)
            {
                return _jumpList.Items.Any(item => item.Arguments == SelectedTicketType.Id);
            }
            return false;
        }
    }


    private async Task PrepareForSendAsync()
    {
        if (SelectedTicketType != null)
        {
            if (_appSettings.FirstTimeUse)
            {
                var dialog = new ContentDialog
                {
                    Content = _localizer.PrepareSmsNote,
                    Title = _localizer.PrepareSmsNoteTitle,
                    XamlRoot = XamlRoot
                };
                dialog.CloseButtonText = "Ok";
                await dialog.ShowAsync();
                _appSettings.FirstTimeUse = false;
            }
            await _smsService.PrepareSmsAsync(SelectedTicketType.PhoneNumber, SelectedTicketType.SmsText);
        }
    }

    private async Task TogglePinAsync()
    {
        var localizer = new LocalizationService();
        if (!IsPinned)
        {
            JumpListItem newItem;
            if (SelectedTicketType.TimeText == String.Empty)
            {
                newItem = JumpListItem.CreateWithArguments(SelectedTicketType.Id, $"{SelectedCity.Name} - duplicate");
                newItem.Logo = new Uri("ms-appx:///SmsTicket/Assets/ticket_dupe.png");
            }
            else
            {
                newItem = JumpListItem.CreateWithArguments(SelectedTicketType.Id, $"{SelectedCity.Name} - {SelectedTicketType.TimeText}");
                newItem.Logo = new Uri("ms-appx:///SmsTicket/Assets/ticket.png");
            }
            newItem.Description = string.Format(localizer.QuickActionDescriptionFormatString, SelectedTicketType.Price);
            _jumpList.Items.Add(newItem);
        }
        else
        {
            var foundItem = _jumpList.Items.FirstOrDefault(item => item.Arguments == SelectedTicketType.Id.ToString());
            if (foundItem != null)
            {
                _jumpList.Items.Remove(foundItem);
            }
        }

        await _jumpList.SaveAsync();

        OnPropertyChanged(nameof(IsPinned));

        if (IsPinned)
        {
            ContentDialog dlg = new()
            {
                Title = localizer.PinnedTitle,
                Content = localizer.PinnedDescription,
                XamlRoot = XamlRoot,
                PrimaryButtonText = "Ok"
            };
            await dlg.ShowAsync();
        }
    }

}
