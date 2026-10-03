using System.Windows;
using System.Windows.Controls;
using LeakGuard.Core.Models;
using LeakGuard.Core.Services;

namespace LeakGuard.App.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly LoggingService _logging;
    private readonly ReportStorage _reportStorage;

    public MainWindow()
    {
        InitializeComponent();

        _settings = ReportStorage.LoadSettings();
        _logging = new LoggingService();
        _reportStorage = new ReportStorage(_settings);

        // Очистка просроченных отчётов при запуске
        _reportStorage.CleanupExpiredReports();

        // Загрузка начальной страницы
        NavigateToDashboard();

        _logging.LogInformation("LeakGuard запущен");
    }

    private void NavigateToDashboard()
    {
        var view = new Views.DashboardView(_settings, _logging, _reportStorage);
        ContentFrame.Navigate(view);
        StatusText.Text = "Готово к работе";
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        _logging.LogInformation("LeakGuard закрыт");
        _reportStorage.SaveSettingsAsync().ConfigureAwait(false);
        _logging.Dispose();
        Application.Current.Shutdown();
    }

    private void BtnHelp_Click(object sender, RoutedEventArgs e)
    {
        var view = new Views.GuideView();
        ContentFrame.Navigate(view);
        StatusText.Text = "Справка и руководство";
    }
}
