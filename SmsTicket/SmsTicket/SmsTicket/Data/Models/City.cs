using System.Collections.Generic;

namespace SmsTicket.Data.Models;

public class City
{        
    public string Id { get; set; }
    public string Name { get; set; }
    public string Abbreviation { get; set; }

    public string DefaultTicketId { get; set; }

    public List<TicketType> TicketTypes { get; set; }
}
