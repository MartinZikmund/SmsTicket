namespace SmsTicket.Views;

/// <summary>A stored ticket SMS with what was read from it.</summary>
public sealed partial class TicketSmsView : UserControl
{
    public TicketSmsView(string inspectionNote, string heading, string validity, string? code, string smsText)
    {
        InspectionNote = inspectionNote;
        Heading = heading;
        Validity = validity;
        Code = code;
        SmsText = smsText;
        InitializeComponent();
    }

    public string InspectionNote { get; }

    public string Heading { get; }

    public string Validity { get; }

    public string? Code { get; }

    public string SmsText { get; }
}
