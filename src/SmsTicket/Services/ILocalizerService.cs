namespace SmsTicket.Services.Localizer;

public interface ILocalizerService
{
    string this[string key ] { get; }

    string QuickActionDescriptionFormatString { get; }

    string FavoriteAddedTitle { get; }

    string FavoriteAddedDescription { get; }

    string PrepareSmsNote { get; }

    string PrepareSmsNoteTitle { get; }
}
