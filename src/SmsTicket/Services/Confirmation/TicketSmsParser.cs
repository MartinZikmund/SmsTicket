using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SmsTicket.Data;

namespace SmsTicket.Services.Confirmation;

/// <summary>
/// Reads validity, price and code from an operator's ticket SMS. Built from the templates operators publish
/// (Tarif PID, DPMB, DPMO, DSZO - see TicketSmsParserTests); tolerant of missing diacritics and spacing slips.
/// </summary>
public static partial class TicketSmsParser
{
    private enum Bound
    {
        None,
        From,
        Until,
    }

    private readonly record struct Token(Bound Bound, DateOnly? Date, TimeOnly Time);

    private static readonly (string CityId, Regex Pattern)[] _cities =
    [
        ("PHA", new(@"dp hl\.? ?m\.? ?prahy|\bdpp\b|\bpid\b|\bprah|\bprague")),
        ("BR", new(@"\bdpmb\b|\bids ?jmk\b|\bbrno\b")),
        ("OL", new(@"dp m\. ?olomouce|\bdpmo\b|\bolomouc")),
        ("ZL", new(@"\bdszo\b|\bzlin|\botrokovic")),
        ("OST", new(@"\bdpo\b|\bostrav|\bodis\b")),
        ("PIL", new(@"\bpmdp\b|\bplzen|\bplzn")),
        ("LBC", new(@"\bdpmlj\b|\bliberec|\bjablonec")),
        ("CB", new(@"\bdpmcb\b|budejovic")),
        ("HK", new(@"\bdpmhk\b|\bhradec")),
        ("PAR", new(@"\bdpmp\b|\bpardubic")),
        ("UL", new(@"\bdpmul\b|\bdpmdj\b|\busti nad labem")),
        ("CHEB", new(@"\bcheb\b")),
        ("TB", new(@"\bcomett\b|\btabor")),
    ];

    public static ParsedTicketSms Parse(string text, DateTimeOffset reference, TimeZoneInfo zone)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ParsedTicketSms.Empty;
        }

        var ascii = StripDiacritics(text);
        var normalized = ascii.ToLowerInvariant();
        var (from, until) = LooksLikeTicket(normalized) ? ReadValidity(normalized, reference, zone) : (null, null);
        return new(from, until, ReadPrice(normalized), ReadCode(ascii), ReadCity(normalized));
    }

    private static bool LooksLikeTicket(string normalized) =>
        normalized.Contains("jizdenk") || normalized.Contains("ticket") || normalized.Contains("platnost") || normalized.Contains("valid");

    private static (DateTimeOffset? From, DateTimeOffset? Until) ReadValidity(string normalized, DateTimeOffset reference, TimeZoneInfo zone)
    {
        List<Token> tokens = new();
        foreach (Match match in DateTimeRegex().Matches(normalized))
        {
            if (ToToken(match, reference, zone) is { } token)
            {
                tokens.Add(token);
            }
        }

        Token? fromToken = tokens.FirstOrDefault(t => t.Bound == Bound.From) is { Bound: Bound.From } f ? f : null;
        Token? untilToken = tokens.FirstOrDefault(t => t.Bound == Bound.Until) is { Bound: Bound.Until } u ? u : null;
        if (fromToken is null && untilToken is null && tokens.Count >= 2)
        {
            // "platnost 21.11.26 14:22 - 21.11.26 14:52"
            (fromToken, untilToken) = (tokens[0], tokens[1]);
        }

        DateTimeOffset? from = fromToken is { } ft ? ResolveFrom(ft, untilToken, reference, zone) : null;
        DateTimeOffset? until = untilToken is { } ut ? ResolveUntil(ut, from ?? reference, zone) : null;
        if (from is not null && until is not null && until <= from)
        {
            until = null;
        }

        return (from, until);
    }

    private static Token? ToToken(Match match, DateTimeOffset reference, TimeZoneInfo zone)
    {
        var groups = match.Groups;
        var hour = int.Parse(groups["h"].Value, CultureInfo.InvariantCulture);
        var minute = int.Parse(groups["min"].Value, CultureInfo.InvariantCulture);
        var second = groups["s"].Success ? int.Parse(groups["s"].Value, CultureInfo.InvariantCulture) : 0;
        if (hour > 23 || minute > 59 || second > 59)
        {
            return null;
        }

        DateOnly? date = null;
        if (groups["d"].Success)
        {
            var day = int.Parse(groups["d"].Value, CultureInfo.InvariantCulture);
            var month = int.Parse(groups["m"].Value, CultureInfo.InvariantCulture);
            if (month is < 1 or > 12 || day < 1)
            {
                return null;
            }

            var referenceYear = TimeZoneInfo.ConvertTime(reference, zone).Year;
            var year = groups["y"].Success ? int.Parse(groups["y"].Value, CultureInfo.InvariantCulture) : referenceYear;
            if (year < 100)
            {
                // The century that puts the year nearest to the reference
                year += referenceYear / 100 * 100;
                year += year - referenceYear > 50 ? -100 : referenceYear - year > 50 ? 100 : 0;
            }

            if (day > DateTime.DaysInMonth(year, month))
            {
                return null;
            }

            date = new DateOnly(year, month, day);
        }

        var bound = groups["label"].Value switch
        {
            "od" or "from" => Bound.From,
            "do" or "to" or "until" => Bound.Until,
            _ => Bound.None,
        };
        return new(bound, date, new TimeOnly(hour, minute, second));
    }

    private static DateTimeOffset ResolveFrom(Token from, Token? until, DateTimeOffset reference, TimeZoneInfo zone)
    {
        if (from.Date is { } date)
        {
            return PragueTime.At(date, from.Time, zone);
        }

        if (until?.Date is { } untilDate)
        {
            var sameDay = PragueTime.At(untilDate, from.Time, zone);
            return sameDay > PragueTime.At(untilDate, until.Value.Time, zone) ? PragueTime.At(untilDate.AddDays(-1), from.Time, zone) : sameDay;
        }

        return ActiveTickets.ActiveTickets.ResolveStartTime(reference, from.Time.ToTimeSpan(), zone);
    }

    /// <summary>A time without a date is its first occurrence at or after <paramref name="after"/>.</summary>
    private static DateTimeOffset ResolveUntil(Token until, DateTimeOffset after, TimeZoneInfo zone)
    {
        if (until.Date is { } date)
        {
            return PragueTime.At(date, until.Time, zone);
        }

        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(after, zone).DateTime);
        var candidate = PragueTime.At(day, until.Time, zone);
        return candidate >= after ? candidate : PragueTime.At(day.AddDays(1), until.Time, zone);
    }

    private static int? ReadPrice(string normalized) =>
        PriceRegex().Match(normalized) is { Success: true } match &&
        int.TryParse(match.Groups["price"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var price)
            ? price
            : null;

    private static string? ReadCode(string ascii)
    {
        // Brno, Olomouc and Zlín end with "<hash>/<control code>"
        if (HashCodeRegex().Match(ascii) is { Success: true } hash)
        {
            return $"{hash.Groups["hash"].Value}/{hash.Groups["control"].Value}";
        }

        if (LabeledCodeRegex().Match(ascii) is { Success: true } labeled)
        {
            return labeled.Groups["code"].Value;
        }

        return CodeLineRegex().Matches(ascii).LastOrDefault()?.Groups["code"].Value;
    }

    private static string? ReadCity(string normalized) =>
        _cities
            .Select(c => (c.CityId, Match: c.Pattern.Match(normalized)))
            .Where(c => c.Match.Success)
            .OrderBy(c => c.Match.Index)
            .Select(c => c.CityId)
            .FirstOrDefault();

    private static string StripDiacritics(string text)
    {
        StringBuilder builder = new(text.Length);
        foreach (var c in text)
        {
            builder.Append(c switch
            {
                'á' => 'a',
                'č' => 'c',
                'ď' => 'd',
                'é' or 'ě' => 'e',
                'í' => 'i',
                'ň' => 'n',
                'ó' => 'o',
                'ř' => 'r',
                'š' => 's',
                'ť' => 't',
                'ú' or 'ů' => 'u',
                'ý' => 'y',
                'ž' => 'z',
                'Á' => 'A',
                'Č' => 'C',
                'Ď' => 'D',
                'É' or 'Ě' => 'E',
                'Í' => 'I',
                'Ň' => 'N',
                'Ó' => 'O',
                'Ř' => 'R',
                'Š' => 'S',
                'Ť' => 'T',
                'Ú' or 'Ů' => 'U',
                'Ý' => 'Y',
                'Ž' => 'Z',
                ' ' => ' ',
                _ => c,
            });
        }

        return builder.ToString();
    }

    // "od 2.5.26. 13:20", "Do: 3.9.23 15:53", "do: 25.11.202216:15", "od 14:22"
    [GeneratedRegex(@"(?:\b(?<label>od|do|from|to|until)\b\s*[:.]?\s*)?(?:(?<d>\d{1,2})\.\s?(?<m>\d{1,2})\.\s?(?<y>\d{4}|\d{2})?\.?\s*(?:v\s+)?|(?<!\d))(?<h>\d{1,2}):(?<min>\d{2})(?::(?<s>\d{2}))?(?!\d)")]
    private static partial Regex DateTimeRegex();

    // "55,- kc", "29 kc", "29 czk"
    [GeneratedRegex(@"(?<!\d)(?<price>\d{1,4})(?:[,.]\d{1,2})?\s*(?:,\s*-)?\s*(?:kc|czk)\b")]
    private static partial Regex PriceRegex();

    [GeneratedRegex(@"(?<![A-Za-z0-9])(?<hash>[A-Za-z0-9]{3,12})\s*/\s*(?<control>\d{5,8})(?!\d)")]
    private static partial Regex HashCodeRegex();

    // "kód pro kontrolu platnosti" followed by the code; a code always holds a digit
    [GeneratedRegex(@"\bkod(?: pro kontrolu platnosti)?\s*[:\-]?\s*(?=[A-Za-z]*\d)(?<code>[A-Za-z0-9]{4,24})\b", RegexOptions.IgnoreCase)]
    private static partial Regex LabeledCodeRegex();

    [GeneratedRegex(@"^\s*(?=[A-Za-z]*\d)(?=\d*[A-Za-z])(?<code>[A-Za-z0-9]{6,16})\s*$", RegexOptions.Multiline)]
    private static partial Regex CodeLineRegex();
}
