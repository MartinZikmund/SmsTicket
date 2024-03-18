namespace SmsTicket.Data.Models;

public class TicketType
{
    public TicketType( string id, string description, string price, string timeText,
        string phoneNumber, string smsText, bool smsPriceAdditional )
    {
        Id = id;           
        Description = description;
        Price = price;
        TimeText = timeText;
        PhoneNumber = phoneNumber;
        SmsText = smsText;
        SmsPriceAdditional = smsPriceAdditional;
    }

    public string Id { get; set; }
    public string Price { get; set; }
    public string TimeText { get; set; }        

    public bool SmsPriceAdditional { get; set; }

    public string Description { get; set; }

    public string PhoneNumber { get; set; }
    public string SmsText { get; set; }
}
