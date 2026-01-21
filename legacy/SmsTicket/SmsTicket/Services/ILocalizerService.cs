namespace SmsTicket.Services.Localizer;

public interface ILocalizerService
{
    string this[string key ] { get; }

    string QuickActionDescriptionFormatString { get; }

    string PinnedTitle { get; }

    string PinnedDescription { get; }

    string PrepareSmsNote { get; }

    string PrepareSmsNoteTitle { get; }
}
