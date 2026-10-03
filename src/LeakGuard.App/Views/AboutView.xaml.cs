using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace LeakGuard.App.Views;

/// <summary>
/// Interaction logic for AboutView.xaml
/// </summary>
public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
    }

    private void BtnOpenGitHub_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/BitmofL");
    }

    private void BtnOpenReadme_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/BitmofL/Win-scanner/blob/main/README.md");
    }

    private void BtnOpenLicense_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/BitmofL/Win-scanner/blob/main/LICENSE");
    }

    private void BtnOpenPrivacy_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/BitmofL/Win-scanner/blob/main/PRIVACY.md");
    }

    private void BtnOpenAgreement_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/BitmofL/Win-scanner/blob/main/USER_AGREEMENT.md");
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = Window.GetWindow(this);
        if (mainWindow is System.Windows.Window win)
        {
            var contentFrame = win.FindName("ContentFrame") as System.Windows.Controls.Frame;
            contentFrame?.Navigate(new Views.DashboardView(
                LeakGuard.Core.Services.ReportStorage.LoadSettings(),
                new LeakGuard.Core.Services.LoggingService(),
                new LeakGuard.Core.Services.ReportStorage(LeakGuard.Core.Services.ReportStorage.LoadSettings())));
            var statusText = win.FindName("StatusText") as TextBlock;
            statusText?.SetText("Готово к работе");
        }
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось открыть: {url}\n{ex.Message}", "LeakGuard",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
