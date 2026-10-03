using System.Windows;
using System.Windows.Controls;

namespace LeakGuard.App.Views;

/// <summary>
/// Interaction logic for GuideView.xaml
/// </summary>
public partial class GuideView : UserControl
{
    public GuideView()
    {
        InitializeComponent();
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
}
