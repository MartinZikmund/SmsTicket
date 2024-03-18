using System.Threading.Tasks;

namespace SmsTicket.Services.Sms;

public interface ISmsService
{
    Task PrepareSmsAsync( string recipientNumber, string text );
}
