using SmsTicket.Services.Localizer;
using SmsTicket.Services.PurchaseHistory;
using SmsTicket.ViewModels;

namespace SmsTicket.Views;

public sealed partial class HistoryPage : Page
{
    private static readonly LocalizationService _localizer = (LocalizationService)Application.Current.Resources["Localizer"];

    private readonly IPurchaseHistoryService _history = PurchaseHistoryService.Default;

    public HistoryPage()
    {
        ViewModel = new HistoryViewModel(_history, _localizer);
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public HistoryViewModel ViewModel { get; }

    public LocalizationService Localizer => _localizer;

    public static string DeleteText => _localizer.Delete;

    public static Visibility Collapsed(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _history.Changed += OnHistoryChanged;
        await ViewModel.LoadAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => _history.Changed -= OnHistoryChanged;

    private void OnHistoryChanged(object? sender, EventArgs e) =>
        DispatcherQueue.TryEnqueue(async () => await ViewModel.LoadAsync());

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PurchaseHistoryItem item })
        {
            await ViewModel.DeleteAsync(item);
        }
    }

    private async void OnClearClick(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new()
        {
            Title = _localizer.ClearHistoryConfirmTitle,
            Content = _localizer.ClearHistoryConfirmText,
            PrimaryButtonText = _localizer.ClearHistory,
            CloseButtonText = _localizer.Cancel,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.ClearAsync();
        }
    }
}
