namespace SmsTicket.Services.Sms;

public class SmsService : ISmsService
{
    public async Task PrepareSmsAsync(string recipientNumber, string text)
    {
#if __IOS__
        if (!SmsComposer.CanSendText)
        {
            ContentDialog dlg = new ContentDialog();
            dlg.Title = "Not supported";
            dlg.Content = "Your device does not support telephony";
            dlg.PrimaryButtonText = "Ok";
            await dlg.ShowAsync();
            return;
        }

        await SmsComposer.ComposeAsync(recipientNumber, text);
#else
        var chatMessage = new Windows.ApplicationModel.Chat.ChatMessage();
        chatMessage.Body = text;
        chatMessage.Recipients.Add(recipientNumber);
        await Windows.ApplicationModel.Chat.ChatMessageManager.ShowComposeSmsMessageAsync(chatMessage);
#endif
    }
}
