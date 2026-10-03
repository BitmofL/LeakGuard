using System.Windows;
using System.Windows.Controls;
using LeakGuard.Core.Models;
using LeakGuard.Core.Services;

namespace LeakGuard.App.Views;

/// <summary>
/// Interaction logic for DashboardView.xaml
/// </summary>
public partial class DashboardView : UserControl
{
    private readonly AppSettings _settings;
    private readonly LoggingService _logging;
    private readonly ReportStorage _reportStorage;

    public DashboardView(AppSettings settings, LoggingService logging, ReportStorage reportStorage)
    {
        InitializeComponent();
        _settings = settings;
        _logging = logging;
        _reportStorage = reportStorage;

        LoadLastScanInfo();
    }

    private void LoadLastScanInfo()
    {
        if (_settings.LastScanDate.HasValue)
        {
            TxtLastScanDate.Text = _settings.LastScanDate.Value.ToString("dd.MM.yyyy HH:mm");
        }
    }

    private void BtnQuickScan_Click(object sender, RoutedEventArgs e)
    {
        NavigateToScan(ScanMode.Quick);
    }

    private void BtnFullScan_Click(object sender, RoutedEventArgs e)
    {
        NavigateToScan(ScanMode.Full);
    }

    private void BtnCustomScan_Click(object sender, RoutedEventArgs e)
    {
        NavigateToScan(ScanMode.Custom);
    }

    private void BtnOpenReports_Click(object sender, RoutedEventArgs e)
    {
        var view = new Views.ResultsView(_settings, _logging, _reportStorage);
        var mainWindow = Window.GetWindow(this);
        if (mainWindow is System.Windows.Window win)
        {
            var contentFrame = win.FindName("ContentFrame") as System.Windows.Controls.Frame;
            contentFrame?.Navigate(view);
            var statusText = win.FindName("StatusText") as TextBlock;
            statusText?.SetText("Отчёты сканирования");
        }
    }

    private void NavigateToScan(ScanMode mode)
    {
        var view = new Views.ScanView(mode, _settings, _logging, _reportStorage);
        var mainWindow = Window.GetWindow(this);
        if (mainWindow is System.Windows.Window win)
        {
            var contentFrame = win.FindName("ContentFrame") as System.Windows.Controls.Frame;
            contentFrame?.Navigate(view);
            var statusText = win.FindName("StatusText") as TextBlock;
            var modeText = mode switch
            {
                ScanMode.Quick => "Быстрое",
                ScanMode.Full => "Полное",
                _ => "Настраиваемое"
            };
            statusText?.SetText($"Режим: {modeText} сканирование");
        }
    }
}

// Extension for TextBlock
internal static class TextBlockExtensions
{
    public static void SetText(this TextBlock tb, string text)
    {
        tb.Text = text;
    }
}
