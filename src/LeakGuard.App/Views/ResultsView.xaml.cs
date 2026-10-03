using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using LeakGuard.App.Services;
using LeakGuard.Core.Models;
using LeakGuard.Core.Services;

namespace LeakGuard.App.Views;

/// <summary>
/// Interaction logic for ResultsView.xaml
/// </summary>
public partial class ResultsView : UserControl
{
    private readonly AppSettings _settings;
    private readonly LoggingService _logging;
    private readonly ReportStorage _reportStorage;
    private ObservableCollection<ScanResult> _allResults = new();
    private ObservableCollection<ScanResult> _filteredResults = new();

    public ResultsView(AppSettings settings, LoggingService logging, ReportStorage reportStorage)
    {
        InitializeComponent();
        _settings = settings;
        _logging = logging;
        _reportStorage = reportStorage;

        // В MVP загружаем пустой список — результаты приходят из ScanView
        // В полной версии здесь будет загрузка из последнего сканирования
        ResultsList.ItemsSource = _filteredResults;
    }

    /// <summary>
    /// Обновляет результаты из сканирования.
    /// </summary>
    public void UpdateResults(List<ScanResult> results)
    {
        _allResults = new ObservableCollection<ScanResult>(results);
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var riskFilter = CmbRiskFilter.SelectedIndex;
        var categoryFilter = CmbCategoryFilter.SelectedIndex;
        var searchText = TxtSearch.Text?.ToLowerInvariant() ?? string.Empty;

        var filtered = _allResults.AsEnumerable();

        // Фильтр по риску
        if (riskFilter > 0)
        {
            filtered = filtered.Where(r => (int)r.RiskLevel == riskFilter);
        }

        // Фильтр по категории
        if (categoryFilter > 0)
        {
            filtered = filtered.Where(r => (int)r.Category == categoryFilter);
        }

        // Поиск
        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(r =>
                r.FilePath.ToLowerInvariant().Contains(searchText) ||
                r.Reason.ToLowerInvariant().Contains(searchText) ||
                r.CategoryDisplay.ToLowerInvariant().Contains(searchText));
        }

        _filteredResults = new ObservableCollection<ScanResult>(filtered);
        ResultsList.ItemsSource = null;
        ResultsList.ItemsSource = _filteredResults;
    }

    private void CmbRiskFilter_Changed(object sender, RoutedEventArgs e)
    {
        ApplyFilters();
    }

    private void CmbCategoryFilter_Changed(object sender, RoutedEventArgs e)
    {
        ApplyFilters();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void BtnResetFilters_Click(object sender, RoutedEventArgs e)
    {
        CmbRiskFilter.SelectedIndex = 0;
        CmbCategoryFilter.SelectedIndex = 0;
        TxtSearch.Text = string.Empty;
        ApplyFilters();
    }

    private async void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        if (_allResults.Count == 0)
        {
            MessageBox.Show("Нет результатов для экспорта.", "LeakGuard",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var consent = new ConsentManager();
        var consentResult = consent.RequestExportConsent("HTML", "Выберите папку...");
        if (consentResult != ConsentResult.Granted)
            return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "HTML-файл (*.html)|*.html|JSON-файл (*.json)|*.json",
            DefaultExt = ".html",
            FileName = $"leakguard_report_{DateTime.Now:yyyyMMdd_HHmmss}.html"
        };

        if (dialog.ShowDialog() == true)
        {
            var format = dialog.FileName.EndsWith(".json")
                ? ReportFormat.Json
                : ReportFormat.Html;

            var btn = (Button)sender;
            btn.IsEnabled = false;

            try
            {
                await _reportStorage.SaveReportAsync(_allResults.ToList(), format, dialog.FileName);
                MessageBox.Show($"Отчёт сохранён:\n{dialog.FileName}", "LeakGuard",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                _logging.LogInformation($"Отчёт экспортирован: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "LeakGuard",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _logging.LogError("Ошибка экспорта отчёта", ex);
            }
            finally
            {
                btn.IsEnabled = true;
            }
        }
    }

    private void BtnOpenLocation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path && !string.IsNullOrEmpty(path))
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
        }
    }

    private void BtnIgnore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id)
        {
            _settings.IgnoredIds.Add(id);
            _allResults = new ObservableCollection<ScanResult>(
                _allResults.Where(r => r.Id != id));
            ApplyFilters();
            _logging.LogInformation($"Результат игнорируется: {id}");
        }
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

    private void BtnShowPath_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            if (btn.FindName("TxbDetailPath") is TextBlock pathText)
            {
                pathText.Visibility = Visibility.Visible;
                btn.Visibility = Visibility.Collapsed;
            }
        }
    }
}
