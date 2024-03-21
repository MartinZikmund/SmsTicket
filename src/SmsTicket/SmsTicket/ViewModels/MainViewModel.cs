using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmsTicket;
using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.Localizer;
using SmsTicket.Services.Settings;
using SmsTicket.Services.Sms;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Microsoft.UI.Xaml.Controls;
using SmsTicket.Services.AppSettings;

namespace SmsTicket.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ISmsService _smsService;
    private readonly IAppSettings _appSettings;
    private readonly LocalizationService _localizer;
    private JumpList _jumpList;
    public MainViewModel(ISettingsService settings, ISmsService smsService)
    {
        _settings = settings;
        _localizer = new LocalizationService();
        _smsService = smsService;
        _appSettings = new AppSettings(_settings);
    }

    public async void Init()
    {
        Cities = new ObservableCollection<City>(DataSource.Cities);
        var lastCitySelected = _settings.GetSetting("LastCitySelected", () => "PHA", false);
        var targetCity = (from c in Cities where c.Id == lastCitySelected select c).SingleOrDefault();
        if (targetCity != null)
        {
            SelectedCity = targetCity;
        }

        if (JumpList.IsSupported())
        {
            _jumpList = await JumpList.LoadCurrentAsync();
            _jumpList.SystemGroupKind = JumpListSystemGroupKind.None;
            OnPropertyChanged(nameof(IsPinned));
        }
    }

    #region Cities

    private ObservableCollection<City> _cities;

    public ObservableCollection<City> Cities
    {
        get { return _cities; }
        set
        {
            _cities = value;
            OnPropertyChanged(nameof(Cities));
        }
    }

    #endregion

    #region Selected city

    private City _selectedCity;

    public City SelectedCity
    {
        get { return _selectedCity; }
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

    #endregion

    #region Selected ticket type

    private TicketType _selectedTicketType;

    public TicketType SelectedTicketType
    {
        get { return _selectedTicketType; }
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

    #endregion

    #region Prepare for send command

    private RelayCommand _prepareForSendCommand;

    public ICommand PrepareForSendCommand
    {
        get
        {
            _prepareForSendCommand = _prepareForSendCommand ?? new RelayCommand(DoPrepareForSendCommand);
            return _prepareForSendCommand;
        }
    }

    private async void DoPrepareForSendCommand()
    {
        if (SelectedTicketType != null)
        {
            if (_appSettings.FirstTimeUse)
            {
                var dialog = new ContentDialog
                {
                    Content = _localizer.PrepareSmsNote,
                    Title = _localizer.PrepareSmsNoteTitle
                };
                dialog.CloseButtonText = "OK";
                await dialog.ShowAsync();
                _appSettings.FirstTimeUse = false;
            }
            await _smsService.PrepareSmsAsync(SelectedTicketType.PhoneNumber, SelectedTicketType.SmsText);
        }
    }

    #endregion

    private bool _useGps = false;

    public bool UseGps
    {
        get => _useGps;
        set
        {
            _useGps = value;
            OnPropertyChanged();
        }
    }

    private ICommand _togglePinCommand;

    public ICommand TogglePinCommand => _togglePinCommand ?? (_togglePinCommand = new RelayCommand(DoTogglePin));

    private async void DoTogglePin()
    {
        var localizer = new LocalizationService();
        if (!IsPinned)
        {
            JumpListItem newItem;
            if (SelectedTicketType.TimeText == String.Empty)
            {
                newItem = JumpListItem.CreateWithArguments(SelectedTicketType.Id, $"{SelectedCity.Name} - duplicate");
                newItem.Logo = new Uri("ms-appx:///Assets/ticket_dupe.png");
            }
            else
            {
                newItem = JumpListItem.CreateWithArguments(SelectedTicketType.Id, $"{SelectedCity.Name} - {SelectedTicketType.TimeText}");
                newItem.Logo = new Uri("ms-appx:///Assets/ticket.png");
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
            MessageDialog dlg = new MessageDialog(localizer.PinnedDescription, localizer.PinnedTitle);
            await dlg.ShowAsync();
        }
    }
}
