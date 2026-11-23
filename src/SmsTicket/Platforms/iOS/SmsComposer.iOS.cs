using MessageUI;
using UIKit;

namespace SmsTicket.Services.Sms;

/// <summary>
/// Replaces Uno's ChatMessageManager on iOS, which presents from <c>UIApplication.KeyWindow</c>: that is null
/// while a home screen quick action is still bringing the scene to the foreground, and the app crashed.
/// </summary>
internal static class SmsComposer
{
    private static readonly TimeSpan SceneWaitTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan SceneWaitStep = TimeSpan.FromMilliseconds(100);

    public static bool CanSendText => MFMessageComposeViewController.CanSendText;

    /// <summary>Shows the composer and completes once it is dismissed.</summary>
    public static async Task<MessageComposeResult?> ComposeAsync(string recipient, string body)
    {
        if (await WaitForPresenterAsync() is not { } presenter)
        {
            return null;
        }

        TaskCompletionSource<MessageComposeResult> dismissed = new();
        MFMessageComposeViewController composer = new()
        {
            Body = body,
            Recipients = [recipient],
        };
        composer.Finished += (_, e) => composer.DismissViewController(true, () => dismissed.TrySetResult(e.Result));

        presenter.PresentViewController(composer, true, null);
        return await dismissed.Task;
    }

    private static async Task<UIViewController?> WaitForPresenterAsync()
    {
        var deadline = DateTime.UtcNow + SceneWaitTimeout;
        while (FindPresenter() is null && DateTime.UtcNow < deadline)
        {
            await Task.Delay(SceneWaitStep);
        }

        return FindPresenter();
    }

    private static UIViewController? FindPresenter()
    {
        var window = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .Where(scene => scene.ActivationState == UISceneActivationState.ForegroundActive)
            .SelectMany(scene => scene.Windows)
            .FirstOrDefault(w => w.IsKeyWindow);

        var controller = window?.RootViewController;
        while (controller?.PresentedViewController is { IsBeingDismissed: false } presented)
        {
            controller = presented;
        }

        return controller;
    }
}
