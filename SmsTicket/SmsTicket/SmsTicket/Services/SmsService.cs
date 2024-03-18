using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace SmsTicket.Services.Sms;

public class SmsService : ISmsService
{
    public async Task PrepareSmsAsync(string recipientNumber, string text)
    {
#if __IOS__
        if (!UIKit.UIApplication.SharedApplication.CanOpenUrl(new Foundation.NSUrl("tel://")))
        {
            MessageDialog dlg = new MessageDialog("Your device does not support telephony");
            await dlg.ShowAsync();
            return;
        }
#endif
        var chatMessage = new Windows.ApplicationModel.Chat.ChatMessage();
        chatMessage.Body = text;
        chatMessage.Recipients.Add(recipientNumber);
        await Windows.ApplicationModel.Chat.ChatMessageManager.ShowComposeSmsMessageAsync(chatMessage);
    }
}
