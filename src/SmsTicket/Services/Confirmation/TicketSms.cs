namespace SmsTicket.Services.Confirmation;

/// <summary>What could be read from an operator's ticket SMS. Any part may be missing.</summary>
public sealed record ParsedTicketSms(
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    int? Price,
    string? Code,
    string? CityId)
{
    public static ParsedTicketSms Empty { get; } = new(null, null, null, null, null);

    public bool HasValidity => ValidFrom is not null || ValidUntil is not null;
}

public enum ConfirmationOutcome
{
    /// <summary>Validity read from the SMS and stored as confirmed.</summary>
    Confirmed,

    /// <summary>The SMS was stored, but its validity could not be read; the estimate stays.</summary>
    Unrecognized,

    /// <summary>The SMS belongs to an earlier ticket; nothing was stored.</summary>
    Stale,

    /// <summary>The ticket no longer exists.</summary>
    NotFound,
}

public sealed record ConfirmationResult(ConfirmationOutcome Outcome, ParsedTicketSms Parsed);
