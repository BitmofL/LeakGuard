using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LeakGuard.App.Services;
using LeakGuard.Core.Models;
using LeakGuard.Core.Services;

namespace LeakGuard.App.Views;

/// <summary>
/// Interaction logic for ScanView.xaml
/// </summary>
public partial class ScanView : UserControl
{
    private readonly ScanMode _mode;
    private readonly AppSettings _settings;
    private readonly LoggingService _logging;
    private readonly ReportStorage _reportStorage;
    private readonly FileScanner _scanner;
    private readonly CancellationTokenSource _cts = new();
    private bool _isScanning = false;
    private bool _isUnloaded = false;
    private ObservableCollection<ScanResult> _recentFindings = new();
    private int _lastProgressUpdate = -1;

    public ScanView(ScanMode mode, AppSettings settings, LoggingService logging, ReportStorage reportStorage)
    {
        InitializeComponent();
        _mode = mode;
        _settings = settings;
        _logging = logging;
        _reportStorage = reportStorage;
        _scanner = new FileScanner();

        _recentFindings = new ObservableCollection<ScanResult>();
        LstRecentFindings.ItemsSource = _recentFindings;

        Unloaded += ScanView_Unloaded;

        TxtScanTitle.Text = _mode switch
        {
            ScanMode.Quick => "⚡ Быстрое сканирование",
            ScanMode.Full => "🔍 Полное сканирование",
            ScanMode.Custom => "⚙ Настраиваемое сканирование",
            _ => "Сканирование"
        };

        SetupScannerEvents();
        StartScanAsync();
    }

    private void SetupScannerEvents()
    {
        _scanner.ProgressChanged += OnProgressChanged;
    }

    private async void StartScanAsync()
    {
        _isScanning = true;
        BtnStop.IsEnabled = true;
        TxtStatus.Text = "Запуск сканирования...";

        var paths = _mode switch
        {
            ScanMode.Quick => await GetQuickScanPathsAsync(),
            ScanMode.Full => await GetFullScanPathsAsync(),
            ScanMode.Custom => await GetCustomScanPathsAsync(),
            _ => new List<string> { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) }
        };

        if (!paths.Any())
        {
            TxtStatus.Text = "Не выбрано ни одной области для сканирования.";
            BtnStop.IsEnabled = false;
            _isScanning = false;
            return;
        }

        try
        {
            var results = await Task.Run(async () =>
                await _scanner.ScanAsync(paths, _mode, _cts.Token), _cts.Token);

            if (_isScanning)
            {
                OnScanCompleted(results);
            }
        }
        catch (OperationCanceledException)
        {
            TxtStatus.Text = "Сканирование остановлено пользователем.";
            _logging.LogWarning("Сканирование остановлено");
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Ошибка: {ex.Message}";
            _logging.LogError("Ошибка сканирования", ex);
        }
    }

    private async Task<List<string>> GetQuickScanPathsAsync()
    {
        TxtStatus.Text = "Подготовка областей быстрого сканирования...";
        var paths = new List<string>();

        paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
        paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));

        // Облачные папки
        var cloudPaths = new[] { "OneDrive", "Google Drive", "YandexDisk", "Dropbox" };
        foreach (var cloud in cloudPaths)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), cloud);
            if (Directory.Exists(path))
                paths.Add(path);
        }

        // Автозагрузка
        var startup = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs", "Startup");
        if (Directory.Exists(startup))
            paths.Add(startup);

        return paths;
    }

    private async Task<List<string>> GetFullScanPathsAsync()
    {
        TxtStatus.Text = "Выбор областей для полного сканирования...";

        // Запрашиваем согласие
        var consent = new ConsentManager();
        var consentResult = consent.RequestConsent(
            "LeakGuard — Полное сканирование",
            "Полное сканирование проверит все файлы в выбранных областях. Это может занять от нескольких минут до часов.",
            "Данные не отправляются в интернет. Нагрузка на диск будет минимальной.",
            requiresUac: false);

        if (consentResult != ConsentResult.Granted)
        {
            TxtStatus.Text = "Пользователь отменил полное сканирование.";
            return new List<string>();
        }

        return new List<string> { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) };
    }

    private Task<List<string>> GetCustomScanPathsAsync()
    {
        TxtStatus.Text = "Используются настройки по умолчанию. В полной версии — выбор пользовательских областей.";
        return Task.FromResult(new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        });
    }

    private void OnProgressChanged(ScanProgress progress)
    {
        if (_isUnloaded) return;

        // Throttle: обновляем UI не чаще чем каждые 5% прогресса
        if (progress.ProgressPercent == _lastProgressUpdate) return;
        _lastProgressUpdate = (int)progress.ProgressPercent;

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            if (_isUnloaded) return;
            TxtStatus.Text = progress.StatusMessage;
            TxtProgressText.Text = $"{progress.FilesScanned:N0} / {progress.TotalFiles:N0} файлов";
            ScanProgressBar.Value = progress.ProgressPercent;
            TxtCurrentFolder.Text = $"Текущая папка: {progress.CurrentFolder}";
        });
    }

    private void OnResultFound(ScanResult result)
    {
        // Убрано: больше не вызывается из FileScanner
    }

    private void OnScanCompleted(List<ScanResult> results)
    {
        _isScanning = false;
        BtnStop.IsEnabled = false;

        if (_isUnloaded) return;

        // Показываем последние 50 результатов в ленте
        var recent = results.Take(50).ToList();
        _recentFindings = new ObservableCollection<ScanResult>(recent);
        UpdateCounts();

        TxtStatus.Text = $"Сканирование завершено! Найдено {results.Count} результатов.";
        _settings.LastScanDate = DateTime.Now;
        _reportStorage.SaveSettingsAsync().ConfigureAwait(false);
        _logging.LogInformation($"Сканирование завершено. Найдено {results.Count} результатов.");

        // Переход к результатам
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                if (_isUnloaded) return;
                var view = new Views.ResultsView(_settings, _logging, _reportStorage);
                view.UpdateResults(results);
                var mainWindow = Window.GetWindow(this);
                if (mainWindow is System.Windows.Window win)
                {
                    var contentFrame = win.FindName("ContentFrame") as System.Windows.Controls.Frame;
                    contentFrame?.Navigate(view);
                    var statusText = win.FindName("StatusText") as TextBlock;
                    statusText?.SetText($"Сканирование завершено: {results.Count} результатов");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScanView] Ошибка навигации к ResultsView: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        });
    }

    private void UpdateCounts()
    {
        var low = _recentFindings.Count(r => r.RiskLevel == RiskLevel.Low);
        var medium = _recentFindings.Count(r => r.RiskLevel == RiskLevel.Medium);
        var high = _recentFindings.Count(r => r.RiskLevel == RiskLevel.High);

        TxtTotalFound.Text = _recentFindings.Count.ToString();
        TxtLowFound.Text = low.ToString();
        TxtMediumFound.Text = medium.ToString();
        TxtHighFound.Text = high.ToString();
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        _cts.Cancel();
        BtnStop.IsEnabled = false;
    }

    private void LstRecentFindings_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstRecentFindings.SelectedItem is ScanResult result)
        {
            DetailPanel.Visibility = Visibility.Visible;
            BtnShowPath.Visibility = Visibility.Collapsed;
            TxbDetailPath.Text = string.Empty;
        }
        else
        {
            DetailPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnShowPath_Click(object sender, RoutedEventArgs e)
    {
        if (LstRecentFindings.SelectedItem is ScanResult result)
        {
            TxbDetailPath.Text = result.FilePath;
            BtnShowPath.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnOpenLocation_Click(object sender, RoutedEventArgs e)
    {
        if (LstRecentFindings.SelectedItem is ScanResult result && !string.IsNullOrEmpty(result.FilePath))
        {
            var dir = Path.GetDirectoryName(result.FilePath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{result.FilePath}\""
                });
            }
        }
    }

    private void BtnIgnore_Click(object sender, RoutedEventArgs e)
    {
        if (LstRecentFindings.SelectedItem is ScanResult result)
        {
            result.IsIgnored = true;
            _recentFindings.Remove(result);
            UpdateCounts();
            DetailPanel.Visibility = Visibility.Collapsed;
            _logging.LogInformation($"Результат проигнорирован: {result.Id}");
        }
    }

    private void BtnCloseDetail_Click(object sender, RoutedEventArgs e)
    {
        DetailPanel.Visibility = Visibility.Collapsed;
        LstRecentFindings.SelectedItem = null;
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        _cts.Cancel();
        _isScanning = false;

        var mainWindow = Window.GetWindow(this);
        if (mainWindow is System.Windows.Window win)
        {
            var contentFrame = win.FindName("ContentFrame") as System.Windows.Controls.Frame;
            contentFrame?.Navigate(new Views.DashboardView(_settings, _logging, _reportStorage));
            var statusText = win.FindName("StatusText") as TextBlock;
            statusText?.SetText("Готово к работе");
        }
    }

    private void ScanView_Unloaded(object sender, RoutedEventArgs e)
    {
        _isUnloaded = true;
        _cts.Cancel();
        _cts.Dispose();
    }
}
