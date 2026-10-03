using System.Windows;
using System.Windows.Controls;
using LeakGuard.App.Services;
using LeakGuard.Core.Models;
using LeakGuard.Core.Services;

namespace LeakGuard.App.Views;

/// <summary>
/// Interaction logic for SettingsView.xaml
/// </summary>
public partial class SettingsView : UserControl
{
    private readonly AppSettings _settings;
    private readonly LoggingService _logging;
    private readonly ReportStorage _reportStorage;
    private readonly StartupManager _startupManager;

    public SettingsView(AppSettings settings, LoggingService logging, ReportStorage reportStorage)
    {
        InitializeComponent();
        _settings = settings;
        _logging = logging;
        _reportStorage = reportStorage;
        _startupManager = new StartupManager();

        LoadSettings();
    }

    private void LoadSettings()
    {
        ChkInternetAccess.IsChecked = _settings.InternetAccessEnabled;
        ChkAutoStart.IsChecked = _settings.AutoStartEnabled;
        ChkIncludePaths.IsChecked = _settings.IncludeFilePathsInReport;
        ChkIncludeMasked.IsChecked = _settings.IncludeMaskedExamplesInReport;
        ChkIncludeTech.IsChecked = _settings.IncludeTechnicalInfoInReport;
        ChkEncrypt.IsChecked = _settings.EncryptReports;

        // Автоудаление
        CmbAutoDelete.SelectedIndex = _settings.AutoDeleteReportDays switch
        {
            1 => 1,
            7 => 2,
            30 => 3,
            _ => 0
        };

        // Статус автозапуска
        var isEnabled = _startupManager.IsEnabled();
        ChkAutoStart.IsChecked = isEnabled;
        _settings.AutoStartEnabled = isEnabled;

        TxtStartupStatus.Text = isEnabled
            ? "Автозапуск включён через Планировщик задач Windows."
            : "Автозапуск отключён.";
    }

    private void ChkAutoStart_CheckedChanged(object sender, RoutedEventArgs e)
    {
        var consent = new ConsentManager();
        var consentResult = consent.RequestAutoStartConsent();

        if (consentResult == ConsentResult.Granted)
        {
            var enabled = ChkAutoStart.IsChecked == true;
            var success = enabled ? _startupManager.Enable() : _startupManager.Disable();

            if (success)
            {
                _settings.AutoStartEnabled = enabled;
                TxtStartupStatus.Text = enabled
                    ? "Автозапуск включён через Планировщик задач Windows."
                    : "Автозапуск отключён.";
                _logging.LogInformation($"Автозапуск {(enabled ? "включён" : "отключён")}");
            }
            else
            {
                MessageBox.Show(
                    "Не удалось изменить автозапуск. Возможно, требуются права администратора.",
                    "LeakGuard",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ChkAutoStart.IsChecked = !_settings.AutoStartEnabled;
            }
        }
        else
        {
            ChkAutoStart.IsChecked = _settings.AutoStartEnabled;
        }
    }

    private void BtnRemoveStartup_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Удалить задачу автозапуска LeakGuard из Планировщика задач Windows?",
            "LeakGuard",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            var success = _startupManager.Disable();
            if (success)
            {
                _settings.AutoStartEnabled = false;
                ChkAutoStart.IsChecked = false;
                TxtStartupStatus.Text = "Автозапуск отключён.";
                _logging.LogInformation("Автозапуск удалён");
                MessageBox.Show("Задача автозапуска удалена.", "LeakGuard",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Не удалось удалить задачу. Попробуйте от руки через taskschd.msc.",
                    "LeakGuard", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void BtnDeleteAll_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Вы уверены, что хотите удалить ВСЕ локальные отчёты и кэш? Это действие нельзя отменить.",
            "LeakGuard — Удаление данных",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            _reportStorage.DeleteAllReports();
            _logging.LogInformation("Все отчёты и кэш удалены");
            MessageBox.Show("Все отчёты и кэш удалены.", "LeakGuard",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
    {
        _settings.InternetAccessEnabled = ChkInternetAccess.IsChecked == true;
        _settings.IncludeFilePathsInReport = ChkIncludePaths.IsChecked == true;
        _settings.IncludeMaskedExamplesInReport = ChkIncludeMasked.IsChecked == true;
        _settings.IncludeTechnicalInfoInReport = ChkIncludeTech.IsChecked == true;
        _settings.EncryptReports = ChkEncrypt.IsChecked == true;

        _settings.AutoDeleteReportDays = CmbAutoDelete.SelectedIndex switch
        {
            1 => 1,
            2 => 7,
            3 => 30,
            _ => 0
        };

        _reportStorage.SaveSettingsAsync().Wait();
        _logging.LogInformation("Настройки сохранены");
        MessageBox.Show("Настройки сохранены.", "LeakGuard",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = Window.GetWindow(this);
        if (mainWindow is System.Windows.Window win)
        {
            var contentFrame = win.FindName("ContentFrame") as System.Windows.Controls.Frame;
            contentFrame?.Navigate(new Views.DashboardView(_settings, _logging, _reportStorage));
            var statusText = win.FindName("StatusText") as TextBlock;
            statusText?.SetText("Готово к работе");
        }
    }
}
