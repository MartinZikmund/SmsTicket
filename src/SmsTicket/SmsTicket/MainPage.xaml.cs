using SmsTicket.Services.Localizer;
using SmsTicket.Services.Settings;
using SmsTicket.Services.Sms;
using SmsTicket.ViewModels;
using Windows.System;

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
        //if (AnalyticsInfo.VersionInfo.DeviceFamily.ToLowerInvariant().Contains("mobile"))
        //{
        //    MobileAd.Visibility = Visibility.Visible;
        //}
        //else
        //{
        //    DesktopAd.Visibility = Visibility.Visible;
        //}
        //(App.Current as App).UpdateTheme();
    }

    public MainViewModel ViewModel { get; }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.XamlRoot = XamlRoot;
//#if __ANDROID__ || __IOS__
//            if (!_adControlLoaded)
//            {
//                _adControlLoaded = true;
//                var adControl = new AdControl() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom };
//                ContentRoot.Children.Add(adControl);
//                adControl.SetValue(Grid.RowProperty, 2);
//            }
//#endif
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

    //private void ThemeFlyout_OnOpening(object sender, object e)
    //{
    //    var settingsService = Mvx.Resolve<ISettingsService>();
    //    MenuFlyout flyout = (MenuFlyout)sender;
    //    var currentTheme = settingsService.GetSetting("Theme", () => "light", true);
    //    foreach (var item in flyout.Items.OfType<ToggleMenuFlyoutItem>())
    //    {
    //        item.IsChecked = false;
    //    }
    //    CheckThemeOption(currentTheme);
    //}

    //public void CheckThemeOption(string selectedOption)
    //{
    //    switch (selectedOption.ToLowerInvariant())
    //    {
    //        //case "system":
    //        //    {
    //        //        SystemThemeOption.IsChecked = true;
    //        //        break;
    //        //    }
    //        case "light":
    //            {
    //                LightThemeOption.IsChecked = true;
    //                break;
    //            }
    //        case "dark":
    //            {
    //                DarkThemeOption.IsChecked = true;
    //                break;
    //            }
    //    }
    //}

    //        private void ThemeOption_Click(object sender, RoutedEventArgs e)
    //        {
    //=            
    //        }

    //private void ThemeOption_Click(object sender, RoutedEventArgs e)
    //{
    //    ToggleMenuFlyoutItem item = (ToggleMenuFlyoutItem)sender;
    //    var selectedTheme = "light";
    //    if (item == LightThemeOption)
    //    {
    //        selectedTheme = "light";
    //    }
    //    else if (item == DarkThemeOption)
    //    {
    //        selectedTheme = "dark";
    //    }
    //    SetTheme(selectedTheme);
    //}

    //private void SetTheme(string selectedTheme)
    //{
    //    var settingsService = Mvx.Resolve<ISettingsService>();
    //    settingsService.SetSetting("Theme", selectedTheme, true);
    //    (App.Current as App).UpdateTheme();
    //}

    private async void LearnMoreClick(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://mzikmund.dev/", UriKind.Absolute));
    }
}
