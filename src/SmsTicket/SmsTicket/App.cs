using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SmsTicket.Configuration;

namespace SmsTicket;

public class App : Application
{
    protected Window? MainWindow { get; private set; }

    public static AppConfig AppConfig { get; private set; }

    public App()
    {
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        (var host, MainWindow) = BuildHost();

        AppConfig = host.Services.GetRequiredService<IConfiguration>().Get<AppConfig>()!;

#if __ANDROID__
        Android.Gms.Ads.MobileAds.Initialize(Android.App.Application.Context);
#endif

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

        if (rootFrame.Content == null)
        {
            // When the navigation stack isn't restored navigate to the first page,
            // configuring the new page by passing required information as a navigation
            // parameter
            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }

        // Ensure the current window is active
        MainWindow.Activate();
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
                        .Section<AppConfig>()
                )
            );
        
        return (builder.Build(), builder.Window);
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
}
