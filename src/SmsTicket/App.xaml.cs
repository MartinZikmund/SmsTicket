using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Windows.AppLifecycle;
using Uno.AdMob;
using Uno.AdMob.Configuration;
using SmsTicket.Configuration;
using SmsTicket.Data;
using SmsTicket.Services.AppSettings;
using SmsTicket.Services.Settings;
using SmsTicket.Services.Sms;
using Uno.Resizetizer;
using AppInstance = Microsoft.Windows.AppLifecycle.AppInstance;

namespace SmsTicket;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        AppInstance.GetCurrent().Activated += OnAppInstanceActivated;
    }

    public static AppConfig AppConfig { get; private set; } = new();

    public static bool AreAdsEnabled { get; private set; }

    protected Window? MainWindow { get; private set; }

    /// <summary>
    /// Language the strings were loaded in; changing it needs a restart.
    /// </summary>
    public static LanguagePreference StartupLanguage { get; private set; }

    private static Window? _window;

    public static void ApplyTheme(ThemePreference theme)
    {
        // Application.RequestedTheme can't change at runtime, the root element's theme can
        if (_window?.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme.ToElementTheme();
        }

#if !HAS_UNO
        if (_window is not null)
        {
            _window.AppWindow.TitleBar.PreferredTheme = theme switch
            {
                ThemePreference.Light => Microsoft.UI.Windowing.TitleBarTheme.Light,
                ThemePreference.Dark => Microsoft.UI.Windowing.TitleBarTheme.Dark,
                _ => Microsoft.UI.Windowing.TitleBarTheme.UseDefaultAppMode,
            };
        }
#endif
    }

    public static void ApplyLanguage(string languageTag) =>
        Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = languageTag;

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppSettings appSettings = new(new SettingsService());
        StartupLanguage = appSettings.Language;
        // Before any page loads its strings
        ApplyLanguage(StartupLanguage.ToLanguageTag());

        (var host, MainWindow) = BuildHost();
        _window = MainWindow;

        AppConfig = host.Services.GetRequiredService<IOptions<AppConfig>>().Value;

        InitializeAds(host.Services);

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (MainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new Frame();

            // Place the frame in the current Window
            MainWindow.Content = rootFrame;

            rootFrame.NavigationFailed += OnNavigationFailed;
        }

        ApplyTheme(appSettings.Theme);

        if (rootFrame.Content == null)
        {
            // When the navigation stack isn't restored navigate to the first page,
            // configuring the new page by passing required information as a navigation
            // parameter
            rootFrame.Navigate(typeof(Views.ShellPage), args.Arguments);
        }

#if !HAS_UNO
        ResizeWindow(MainWindow, 1000, 820);
#endif

        // Ensure the current window is active
        MainWindow.Activate();

        // Fresh tariffs are cached for the next launch, see DataSource
        _ = Task.Run(DataSource.RefreshAsync);

        await HandleTicketShortcutAsync(args.Arguments);
    }

#if !HAS_UNO
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    private static void ResizeWindow(Window window, int width, int height)
    {
        // AppWindow.Resize takes physical pixels
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var scale = GetDpiForWindow(hwnd) / 96.0;
        window.AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(width * scale), (int)(height * scale)));
    }
#endif

    private static void InitializeAds(IServiceProvider services)
    {
#if __ANDROID__ || __IOS__
#if __IOS__
        // Only builds given an AdMobIosAppId have one, and the SDK crashes on start without it
        if (Foundation.NSBundle.MainBundle.ObjectForInfoDictionary("GADApplicationIdentifier") is null)
        {
            return;
        }

        var adUnitId = AppConfig.IosAdUnitId;
        var testDeviceId = AppConfig.IosTestDeviceId;
#else
        var adUnitId = AppConfig.AndroidAdUnitId;
        var testDeviceId = AppConfig.AndroidTestDeviceId;
#endif

        if (!string.IsNullOrEmpty(adUnitId))
        {
            AdConfig.DefaultBannerAdUnitId = adUnitId;
        }
        else
        {
#if DEBUG
            AdConfig.UseTestAdUnitIds = true;
#else
            return;
#endif
        }

        if (!string.IsNullOrEmpty(testDeviceId))
        {
            AdConfig.AddTestDevice(testDeviceId);
        }

        AdMobPlatform.Initialize(services);
        AreAdsEnabled = true;
#endif
    }

    private void OnAppInstanceActivated(object? sender, AppActivationArguments args)
    {
        // Shortcut tapped while the app is already running
        if (args.Kind == ExtendedActivationKind.Launch &&
            args.Data is Windows.ApplicationModel.Activation.LaunchActivatedEventArgs launch)
        {
            MainWindow?.DispatcherQueue.TryEnqueue(async () => await HandleTicketShortcutAsync(launch.Arguments));
        }
    }

    private static async Task HandleTicketShortcutAsync(string? arguments)
    {
        if (string.IsNullOrEmpty(arguments))
        {
            return;
        }

        // Arguments are untrusted input - only act on known ticket ids
        if (DataSource.FindTicket(arguments) is { Ticket: var ticket })
        {
            SmsService smsService = new();
            await smsService.PrepareSmsAsync(ticket.PhoneNumber, ticket.SmsText);
        }
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
    }

    private (IHost host, Window window) BuildHost()
    {
        var builder = this
            .CreateBuilder(null!)
            .Configure(host => host
#if DEBUG
            // Switch to Development environment when running in DEBUG
            .UseEnvironment(Environments.Development)
#endif
            .UseConfiguration(configure: configBuilder =>
                configBuilder
                    .EmbeddedSource<App>()
                    .Section<AppConfig>())
            .ConfigureServices((_, services) => services.AddAdMob()));

        return (builder.Build(), builder.Window);
    }

    /// <summary>
    /// Configures global Uno Platform logging
    /// </summary>
    public static void InitializeLogging()
    {
#if DEBUG
        // Logging is disabled by default for release builds, as it incurs a significant
        // initialization cost from Microsoft.Extensions.Logging setup. If startup performance
        // is a concern for your application, keep this disabled. If you're running on the web or
        // desktop targets, you can use URL or command line parameters to enable it.
        //
        // For more performance documentation: https://platform.uno/docs/articles/Uno-UI-Performance.html

        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());

            // Log to the Visual Studio Debug console
            builder.AddConsole();
#else
            builder.AddConsole();
#endif

            // Exclude logs below this level
            builder.SetMinimumLevel(LogLevel.Information);

            // Default filters for Uno Platform namespaces
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);

            // Generic Xaml events
            // builder.AddFilter("Microsoft.UI.Xaml", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", LogLevel.Trace );

            // Layouter specific messages
            // builder.AddFilter("Microsoft.UI.Xaml.Controls", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug );

            // builder.AddFilter("Windows.Storage", LogLevel.Debug );

            // Binding related messages
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );

            // Binder memory references tracking
            // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

            // DevServer and HotReload related
            // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

            // Debug JS interop
            // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
    }
}
