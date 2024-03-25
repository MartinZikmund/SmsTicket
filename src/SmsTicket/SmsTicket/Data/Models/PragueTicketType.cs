namespace SmsTicket.Data.Models;

public class PragueTicketType : TicketType
{
    public PragueTicketType(string id, string description, string price, string timeText, string smsText) :
        base(id, description, price, timeText, "90206", smsText, true)
    {
    }
}
