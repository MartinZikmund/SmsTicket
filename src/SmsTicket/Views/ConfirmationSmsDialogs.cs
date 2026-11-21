using System.Diagnostics;
using System.Globalization;
using Microsoft.UI.Xaml.Automation;
using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.ActiveTickets;
using SmsTicket.Services.Confirmation;
using SmsTicket.Services.Localizer;
using Windows.ApplicationModel.DataTransfer;

namespace SmsTicket.Views;

/// <summary>Dialogs for pasting, sharing and viewing the operator's ticket SMS.</summary>
public static class ConfirmationSmsDialogs
{
    private static readonly LocalizationService _localizer = new();

    // ContentDialog allows one open dialog per XamlRoot
    private static readonly SemaphoreSlim _dialogLock = new(1, 1);

    /// <summary>Asks for the SMS text, prefilled from the clipboard when it holds a ticket.</summary>
    public static async Task<string?> AskForSmsAsync(XamlRoot xamlRoot, ActiveTicketService service)
    {
        TextBox input = new()
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 144,
            MaxHeight = 240,
            PlaceholderText = _localizer.PasteConfirmationSmsPlaceholder,
            Text = await ReadTicketFromClipboardAsync(service) ?? "",
        };
        ScrollViewer.SetVerticalScrollBarVisibility(input, ScrollBarVisibility.Auto);
        AutomationProperties.SetAutomationId(input, "ConfirmationSmsInput");
        AutomationProperties.SetName(input, _localizer.PasteConfirmationSmsPlaceholder);

        ContentDialog dialog = new()
        {
            Title = _localizer.PasteConfirmationSms,
            Content = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = _localizer.PasteConfirmationSmsText, TextWrapping = TextWrapping.Wrap },
                    input,
                },
            },
            PrimaryButtonText = _localizer.Save,
            CloseButtonText = _localizer.Cancel,
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(input.Text),
            XamlRoot = xamlRoot,
        };
        input.TextChanged += (_, _) => dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(input.Text);

        return await ShowAsync(dialog) == ContentDialogResult.Primary ? input.Text.Trim() : null;
    }

    /// <summary>Tells the user what happened, unless the card already shows it.</summary>
    public static async Task ShowResultAsync(XamlRoot xamlRoot, ConfirmationResult result, ActiveTicket? ticket, bool announceSuccess)
    {
        var (title, text) = result.Outcome switch
        {
            ConfirmationOutcome.Unrecognized => (_localizer.ConfirmationUnrecognizedTitle, _localizer.ConfirmationUnrecognizedText),
            ConfirmationOutcome.Stale => (_localizer.ConfirmationStaleTitle, _localizer.ConfirmationStaleText),
            ConfirmationOutcome.Confirmed when announceSuccess && ticket is not null => (_localizer.ConfirmationSavedTitle, SavedText(ticket)),
            _ => (null, null),
        };

        if (title is not null)
        {
            await ShowMessageAsync(xamlRoot, title, text!);
        }
    }

    /// <summary>For a shared SMS that could belong to several tickets.</summary>
    public static async Task<ActiveTicket?> ChooseTicketAsync(XamlRoot xamlRoot, IReadOnlyList<ActiveTicket> candidates, TimeZoneInfo zone)
    {
        RadioButtons options = new() { SelectedIndex = 0 };
        AutomationProperties.SetAutomationId(options, "ConfirmationTicketChoice");
        AutomationProperties.SetName(options, _localizer.ChooseTicketTitle);
        foreach (var candidate in candidates)
        {
            var prepared = TimeZoneInfo.ConvertTime(candidate.Record.PreparedAt, zone).ToString("t", CultureInfo.CurrentCulture);
            options.Items.Add(string.Format(CultureInfo.CurrentCulture, _localizer.ChooseTicketItemFormat, CityName(candidate.Record), ValidityText(candidate.Record), prepared));
        }

        ContentDialog dialog = new()
        {
            Title = _localizer.ChooseTicketTitle,
            Content = options,
            PrimaryButtonText = _localizer.Save,
            CloseButtonText = _localizer.Cancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
        };

        return await ShowAsync(dialog) == ContentDialogResult.Primary && options.SelectedIndex >= 0
            ? candidates[options.SelectedIndex]
            : null;
    }

    public static Task ShowNoTicketAsync(XamlRoot xamlRoot) =>
        ShowMessageAsync(xamlRoot, _localizer.NoTicketForSmsTitle, _localizer.NoTicketForSmsText);

    /// <summary>The stored SMS with what was read from it. The original SMS is what counts at an inspection.</summary>
    public static async Task ShowStoredSmsAsync(XamlRoot xamlRoot, ActiveTicket ticket, TimeZoneInfo zone)
    {
        var record = ticket.Record;
        var validity = ticket.Source == ValiditySource.Confirmed
            ? string.Format(CultureInfo.CurrentCulture, _localizer.TicketSmsValidityFormat, ActiveTicketFormatter.FormatRange(ticket.ValidFrom, ticket.ValidUntil, zone, CultureInfo.CurrentCulture))
            : _localizer.TicketSmsValidityUnknown;

        var code = record.TicketCode is { } c ? string.Format(CultureInfo.CurrentCulture, _localizer.TicketSmsCodeFormat, c) : null;
        TicketSmsView content = new(
            _localizer.TicketSmsInspectionNote,
            $"{CityName(record)} · {ValidityText(record)}",
            validity,
            code,
            record.ConfirmationSms ?? "");

        ContentDialog dialog = new()
        {
            Title = _localizer.TicketSmsTitle,
            Content = content,
            CloseButtonText = _localizer.Close,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot,
        };
        await ShowAsync(dialog);
    }

    private static Task ShowMessageAsync(XamlRoot xamlRoot, string title, string text) =>
        ShowAsync(new ContentDialog
        {
            Title = title,
            Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap },
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot,
        });

    private static async Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
    {
        await _dialogLock.WaitAsync();
        try
        {
            return await dialog.ShowAsync();
        }
        finally
        {
            _dialogLock.Release();
        }
    }

    private static async Task<string?> ReadTicketFromClipboardAsync(ActiveTicketService service)
    {
        try
        {
            var content = Clipboard.GetContent();
            if (content?.Contains(StandardDataFormats.Text) != true)
            {
                return null;
            }

            // Don't show unrelated clipboard content, it may be private
            var text = await content.GetTextAsync();
            return TicketSmsParser.Parse(text, service.Now, service.Zone).HasValidity ? text.Trim() : null;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Clipboard unavailable: {ex.Message}");
            return null;
        }
    }

    private static string SavedText(ActiveTicket ticket) =>
        string.Format(
            CultureInfo.CurrentCulture,
            _localizer.ConfirmationSavedFormat,
            $"{CityName(ticket.Record)} · {ValidityText(ticket.Record)}",
            ActiveTicketFormatter.FormatRange(ticket.ValidFrom, ticket.ValidUntil, PragueTime.Zone, CultureInfo.CurrentCulture));

    private static string CityName(PurchaseRecord record) =>
        DataSource.Cities.FirstOrDefault(c => c.Id == record.CityId)?.Name ?? record.CityName;

    private static string ValidityText(PurchaseRecord record) =>
        DataSource.Cities.FirstOrDefault(c => c.Id == record.CityId)?.TicketTypes.FirstOrDefault(t => t.Id == record.TicketId)?.TimeText
            ?? record.TimeText;
}
