using SmsTicket.Controls;
using SmsTicket.Services.Localizer;
using SmsTicket.Services.Settings;
using SmsTicket.Services.Sms;
using SmsTicket.ViewModels;
using Windows.System;
using Windows.System.Profile;

namespace SmsTicket;

public sealed partial class MainPage : Page
{
    private bool _adControlLoaded = false;

    public LocalizationService Localizer { get; } = App.Current.Resources["Localizer"] as LocalizationService;

    public MainPage()
    {
        this.InitializeComponent();
        this.DataContextChanged += MainPage_DataContextChanged;

        DataContext = ViewModel = new MainViewModel(new SettingsService(), new SmsService());

        this.Loaded += MainPage_Loaded;
    }

    public MainViewModel ViewModel { get; }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.XamlRoot = XamlRoot;
#if __ANDROID__ || __IOS__
        if (!_adControlLoaded)
        {
            _adControlLoaded = true;
            var adControl = new AdControl()
            {
                Margin = new Thickness(4),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            ContentRoot.Children.Add(adControl);
            adControl.SetValue(Grid.RowProperty, 2);
        }
#endif
    }

    private void MainPage_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
    {
        Model = (MainViewModel)args.NewValue;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        Model.Init();
    }

    public MainViewModel Model { get; private set; }
}
