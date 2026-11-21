using Microsoft.UI.Xaml.Media.Imaging;
using SmsTicket.Data.Models;
using SmsTicket.Services.Localizer;
using SmsTicket.Services.Settings;
using SmsTicket.Services.Sms;
using SmsTicket.ViewModels;
using Uno.AdMob;

namespace SmsTicket;

public sealed partial class MainPage : Page
{
    private static readonly LocalizationService _localizer = new();

    private bool _isAdLoaded;

    public MainPage()
    {
        InitializeComponent();
        ViewModel = new MainViewModel(new SettingsService(), new SmsService());
        Loaded += OnLoaded;
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MainViewModel.SelectedTicketType))
            {
                // ItemsSource swaps on city change; select once the new items are realized
                DispatcherQueue.TryEnqueue(SyncTicketSelection);
            }
        };
    }

    public MainViewModel ViewModel { get; }

    public LocalizationService Localizer => _localizer;

    public static string DuplicateText => _localizer.Duplicate;

    public static ImageSource? ToImageSource(string? uri) => uri is null ? null : new BitmapImage(new Uri(uri));

    public static Uri? ToUri(string? uri) => Uri.TryCreate(uri, UriKind.Absolute, out var result) ? result : null;

    public static Visibility Collapsed(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility VisibleIfNotNull(object? value) => value is null ? Visibility.Collapsed : Visibility.Visible;

    public static string PhotoAuthorText(PhotoCredit? photo) => photo is null ? "" : $"{_localizer.PhotoBy}: {photo.Author}";

    public static string PriceText(string price) => $"{price} {_localizer["CzechPriceTag"]}";

    public static string SmsPriceSuffix(bool smsPriceAdditional) => smsPriceAdditional ? " " + _localizer.PlusPriceOfSms : "";

    public static string PinGlyph(bool isPinned) => isPinned ? "" : "";

    public string PinLabel(bool isPinned) => isPinned ? _localizer.Unpin : _localizer.Pin;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.XamlRoot = XamlRoot;

        if (!_isAdLoaded && App.AreAdsEnabled)
        {
            _isAdLoaded = true;
            BannerAd banner = new() { HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetRow(banner, 3);
            ContentRoot.Children.Add(banner);
        }

        SyncTicketSelection();
        await ViewModel.InitAsync();
    }


    private void OnCityFlyoutOpened(object? sender, object e) => CityList.ScrollIntoView(ViewModel.SelectedCity);

    private void OnCityClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is City city)
        {
            ViewModel.SelectedCity = city;
        }

        CityFlyout.Hide();
    }

    private void OnTicketSelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem is TicketType ticket)
        {
            ViewModel.SelectedTicketType = ticket;
        }
    }

    private void SyncTicketSelection()
    {
        if (ViewModel.SelectedTicketType is { } ticket &&
            ViewModel.SelectedCity.TicketTypes.IndexOf(ticket) is var index and >= 0 &&
            !ReferenceEquals(TicketsView.SelectedItem, ticket))
        {
            TicketsView.Select(index);
        }
    }
}
