namespace SmsTicket.Data.Models;

public class BrnoTicketType : TicketType
{
    public BrnoTicketType(string id, string description, string price, string timeText, string smsText) :
        base(id, description, price, timeText, "90206", smsText, true)
    {
    }
}
