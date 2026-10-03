using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using LeakGuard.Core.Models;
using LeakGuard.Core.Rules;

namespace LeakGuard.Core.Services;

/// <summary>
/// Результаты обратного вызова сканирования.
/// </summary>
public class ScanProgress
{
    public string CurrentFolder { get; set; } = string.Empty;
    public long FilesScanned { get; set; }
    public long TotalFiles { get; set; }
    public List<ScanResult> NewResults { get; set; } = new();
    public string StatusMessage { get; set; } = string.Empty;
    public double ProgressPercent { get; set; }
}

/// <summary>
/// Интерфейс сканера.
/// </summary>
public interface IScanner
{
    event Action<ScanProgress>? ProgressChanged;
    event Action<ScanResult>? ResultFound;

    Task<List<ScanResult>> ScanAsync(
        IEnumerable<string> paths,
        ScanMode mode,
        CancellationToken cancellationToken = default);

    Task<List<ScanResult>> ScanQuickAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Основной сканер файлов и настроек.
/// </summary>
public class FileScanner : IScanner
{
    private readonly PatternMatcher _patternMatcher;
    private readonly HashSet<string> _skippedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "$Recycle.Bin", "System Volume Information", "Recovery",
        "$Windows.~WS", "$Windows.~BT"
    };

    public event Action<ScanProgress>? ProgressChanged;
    public event Action<ScanResult>? ResultFound;

    public FileScanner()
    {
        _patternMatcher = new PatternMatcher();
    }

    public async Task<List<ScanResult>> ScanAsync(
        IEnumerable<string> paths,
        ScanMode mode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = new ConcurrentBag<ScanResult>();
            var filesScanned = 0L;
            var sw = Stopwatch.StartNew();

            var pathList = paths.ToList();
            Console.WriteLine($"[FileScanner] Сканирование {pathList.Count} путей, режим {mode}");

            var totalFiles = CountFiles(pathList, cancellationToken);
            Console.WriteLine($"[FileScanner] Всего файлов: {totalFiles}");

            foreach (var path in pathList)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    Console.WriteLine($"[FileScanner] Обработка: {path}");

                    if (Directory.Exists(path))
                    {
                        try
                        {
                            var folderResults = await ScanDirectoryAsync(
                                path, results, cancellationToken, totalFiles);
                            filesScanned += folderResults;
                            Console.WriteLine($"[FileScanner] Папка {path}: {folderResults} файлов");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[FileScanner] Ошибка папки {path}: {ex.GetType().Name}: {ex.Message}");
                        }
                    }
                    else if (File.Exists(path))
                    {
                        try
                        {
                            var singleResult = await ScanFileAsync(path, cancellationToken);
                            foreach (var r in singleResult)
                                results.Add(r);
                            filesScanned++;
                            Console.WriteLine($"[FileScanner] Файл {path}: OK");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[FileScanner] Ошибка файла {path}: {ex.GetType().Name}: {ex.Message}");
                        }
                    }

                    var progress = new ScanProgress
                    {
                        CurrentFolder = path,
                        FilesScanned = filesScanned,
                        TotalFiles = totalFiles,
                        StatusMessage = $"Проверка: {path}",
                        ProgressPercent = totalFiles > 0 ? (double)filesScanned / totalFiles * 100 : 0
                    };

                    ProgressChanged?.Invoke(progress);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FileScanner] Ошибка обработки пути {path}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            sw.Stop();
            Console.WriteLine($"[FileScanner] Сканирование завершено за {sw.Elapsed}. Найдено {results.Count} результатов.");

            return results.ToList();
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[FileScanner] Сканирование отменено");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FileScanner] Критическая ошибка: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            return new List<ScanResult>();
        }
    }

    public async Task<List<ScanResult>> ScanQuickAsync(CancellationToken cancellationToken = default)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var paths = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads",
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        };

        // Облачная синхронизация
        var cloudPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive",
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Google Drive",
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\YandexDisk",
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Dropbox",
        };

        foreach (var p in cloudPaths)
        {
            if (Directory.Exists(p))
                paths.Add(p);
        }

        // Автозагрузка
        var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\Programs\Startup";
        if (Directory.Exists(startupPath))
            paths.Add(startupPath);

        return await ScanAsync(paths, ScanMode.Quick, cancellationToken);
    }

    private async Task<long> ScanDirectoryAsync(
        string directory,
        ConcurrentBag<ScanResult> results,
        CancellationToken cancellationToken,
        long totalFiles,
        int depth = 0)
    {
        var count = 0L;

        try
        {
            Console.WriteLine($"[FileScanner] ScanDirectoryAsync: {directory} (depth={depth})");

            // Ограничиваем глубину рекурсии 15 уровнями
            if (depth > 15)
            {
                Console.WriteLine($"[FileScanner] Пропуск глубины: {directory}");
                return count;
            }

            // Сканируем файлы в текущей директории
            try
            {
                var files = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly).ToList();
                Console.WriteLine($"[FileScanner] Найдено {files.Count} файлов в {directory}");

                foreach (var file in files)
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        count++;
                        var fileResults = await ScanFileAsync(file, cancellationToken);
                        foreach (var r in fileResults)
                        {
                            results.Add(r);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[FileScanner] Ошибка файла {file}: {ex.GetType().Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileScanner] Ошибка EnumerateFiles {directory}: {ex.GetType().Name}");
            }

            // Рекурсивно сканируем поддиректории
            try
            {
                var subDirs = Directory.EnumerateDirectories(directory).ToList();
                Console.WriteLine($"[FileScanner] Найдено {subDirs.Count} подпапок в {directory}");

                foreach (var subDir in subDirs)
                {
                    try
                    {
                        var dirName = Path.GetFileName(subDir);
                        if (_skippedFolders.Contains(dirName))
                            continue;

                        if (cancellationToken.IsCancellationRequested) break;

                        count += await ScanDirectoryAsync(subDir, results, cancellationToken, totalFiles, depth + 1);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[FileScanner] Ошибка подпапки {subDir}: {ex.GetType().Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileScanner] Ошибка EnumerateDirectories {directory}: {ex.GetType().Name}");
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Пропускаем папки без прав
        }
        catch (DirectoryNotFoundException)
        {
            // Папка могла быть удалена
        }
        catch (IOException)
        {
            // IO error — пропускаем
        }

        return count;
    }

    private async Task<List<ScanResult>> ScanFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"[FileScanner] ScanFileAsync: {filePath}");
            var results = await ScanFileInternalAsync(filePath, cancellationToken);
            Console.WriteLine($"[FileScanner] ScanFileAsync OK: {filePath} ({results.Count} результатов)");
            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FileScanner] ScanFileAsync ERROR: {filePath} - {ex.GetType().Name}: {ex.Message}");
            return new List<ScanResult>();
        }
    }

    private async Task<List<ScanResult>> ScanFileInternalAsync(string filePath, CancellationToken cancellationToken)
    {
        var results = new List<ScanResult>();
        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        // 1. Проверка имени файла
        var nameCheck = _patternMatcher.CheckFileName(fileName);
        if (nameCheck.IsSensitive)
        {
            results.Add(new ScanResult
            {
                FilePath = filePath,
                Category = ScanCategory.DeveloperFiles,
                RiskLevel = RiskLevel.Medium,
                Reason = nameCheck.Reason!,
                Confidence = 0.7,
                Recommendation = "Проверьте содержимое файла. Если это конфиденциальные данные, переместите в защищённое хранилище или удалите.",
                FileType = extension,
                FileModifiedDate = GetSafeFileTime(filePath),
                FileSize = GetSafeFileSize(filePath)
            });
        }

        // 2. Проверка расширений
        if (PatternLibrary.SensitiveFileExtensions.Contains(extension))
        {
            results.Add(new ScanResult
            {
                FilePath = filePath,
                Category = ScanCategory.DeveloperFiles,
                RiskLevel = RiskLevel.High,
                Reason = $"Файл с чувствительным расширением: {extension}",
                Confidence = 0.95,
                Recommendation = "Этот файл может содержать ключи, сертификаты или секреты. Убедитесь, что он защищён и не хранится в открытых папках.",
                FileType = extension,
                FileModifiedDate = GetSafeFileTime(filePath),
                FileSize = GetSafeFileSize(filePath)
            });
        }

        if (PatternLibrary.ArchiveExtensions.Contains(extension))
        {
            results.Add(new ScanResult
            {
                FilePath = filePath,
                Category = ScanCategory.Archives,
                RiskLevel = RiskLevel.Low,
                Reason = $"Обнаружен архив: {fileName}",
                Confidence = 0.5,
                Recommendation = "Архивы могут содержать конфиденциальные данные. Проверьте содержимое при необходимости.",
                FileType = extension,
                FileModifiedDate = GetSafeFileTime(filePath),
                FileSize = GetSafeFileSize(filePath)
            });
        }

        if (PatternLibrary.BackupExtensions.Contains(extension))
        {
            var sensitiveKeywordsCheck = _patternMatcher.CheckFileName(fileName);
            if (sensitiveKeywordsCheck.IsSensitive)
            {
                results.Add(new ScanResult
                {
                    FilePath = filePath,
                    Category = ScanCategory.BackupFiles,
                    RiskLevel = RiskLevel.Medium,
                    Reason = $"Резервная копия с чувствительным именем: {fileName}",
                    Confidence = 0.65,
                    Recommendation = "Резервные копии могут содержать устаревшие конфиденциальные данные. Рассмотрите безопасное удаление.",
                    FileType = extension,
                    FileModifiedDate = GetSafeFileTime(filePath),
                    FileSize = GetSafeFileSize(filePath)
                });
            }
        }

        // 3. Сканирование содержимого текстовых файлов
        if (PatternLibrary.DocumentExtensions.Contains(extension))
        {
            var contentResults = await ScanTextContentAsync(filePath, cancellationToken);
            results.AddRange(contentResults);
        }

        // 4. Метаданные изображений (EXIF) — отключено: GDI+ Bitmap вызывает вылеты
        // if (PatternLibrary.ImageExtensions.Contains(extension))
        // {
        //     var exifResults = await ScanImageMetadataAsync(filePath, cancellationToken);
        //     results.AddRange(exifResults);
        // }

        return results;
    }

    private async Task<List<ScanResult>> ScanTextContentAsync(string filePath, CancellationToken cancellationToken)
    {
        var results = new List<ScanResult>();

        try
        {
            Console.WriteLine($"[FileScanner] ScanTextContentAsync: {filePath}");

            await using var stream = File.OpenRead(filePath);
            if (stream.Length > 5 * 1024 * 1024) // > 5 MB — пропускаем
            {
                Console.WriteLine($"[FileScanner] Пропуск большого файла: {filePath}");
                return results;
            }

            using var reader = new StreamReader(stream, Encoding.UTF8, true, 8192, leaveOpen: true);
            var content = await reader.ReadToEndAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(content))
                return results;

            // Ищем паттерны
            var patterns = _patternMatcher.FindPatterns(content);
            var sensitiveKeywords = _patternMatcher.FindSensitiveKeywords(content);

            if (patterns.Any() || sensitiveKeywords.Any())
            {
                var maxRisk = patterns.Any() ? patterns.Max(p => p.Risk) : RiskLevel.Low;
                var hasSensitiveKeyword = sensitiveKeywords.Count > 0;

                if (hasSensitiveKeyword && maxRisk < RiskLevel.Medium)
                    maxRisk = RiskLevel.Medium;

                results.Add(new ScanResult
                {
                    FilePath = filePath,
                    Category = ScanCategory.PersonalData,
                    RiskLevel = maxRisk,
                    Reason = $"Найдено {patterns.Count} паттернов ПДН и {sensitiveKeywords.Count} чувствительных ключевых слов",
                    Confidence = patterns.Any() ? 0.75 : 0.5,
                    Recommendation = "Файл содержит персональные данные или чувствительные ключевые слова. Проверьте необходимость хранения этих данных.",
                    MaskedExample = DataMasker.MaskContent(content, 200),
                    FileType = Path.GetExtension(filePath),
                    FileModifiedDate = GetSafeFileTime(filePath),
                    FileSize = GetSafeFileSize(filePath)
                });
            }
        }
        catch (IOException)
        {
            // Файл заблокирован
        }
        catch (UnauthorizedAccessException)
        {
            // Нет прав
        }
        catch (ArgumentException)
        {
            // Бинарный файл, не текст
        }

        return results;
    }

    private async Task<List<ScanResult>> ScanImageMetadataAsync(string filePath, CancellationToken cancellationToken)
    {
        var results = new List<ScanResult>();

        try
        {
            // Пропускаем файлы больше 10 МБ
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > 10 * 1024 * 1024)
                return results;

            using var bitmap = new System.Drawing.Bitmap(filePath);
            var propertyIds = bitmap.PropertyIdList ?? Array.Empty<int>();

            // Проверяем GPS-координаты (property tag 0x8825)
            var gpsPropId = 0x8825;
            if (propertyIds.Contains(gpsPropId))
            {
                var gpsValue = bitmap.GetPropertyItem(gpsPropId);
                if (gpsValue != null && gpsValue.Value.Length > 0)
                {
                    results.Add(new ScanResult
                    {
                        FilePath = filePath,
                        Category = ScanCategory.ImageMetadata,
                        RiskLevel = RiskLevel.Medium,
                        Reason = "Изображение содержит GPS-координаты в EXIF",
                        Confidence = 0.95,
                        Recommendation = "GPS-координаты в изображении могут раскрыть ваше местоположение. Используйте инструменты удаления EXIF перед публикацией.",
                        TechnicalInfo = "GPS-данные обнаружены в EXIF (тег 0x8825)",
                        FileType = Path.GetExtension(filePath),
                        FileModifiedDate = File.GetLastWriteTime(filePath),
                        FileSize = new FileInfo(filePath).Length
                    });
                }
            }

            // Проверяем производителя камеры (property tag 0x010F)
            var makerPropId = 0x010F;
            if (propertyIds.Contains(makerPropId))
            {
                var makerValue = bitmap.GetPropertyItem(makerPropId);
                if (makerValue != null)
                {
                    var maker = System.Text.Encoding.ASCII.GetString(makerValue.Value).TrimEnd('\0');
                    if (!string.IsNullOrEmpty(maker))
                    {
                        results.Add(new ScanResult
                        {
                            FilePath = filePath,
                            Category = ScanCategory.ImageMetadata,
                            RiskLevel = RiskLevel.Info,
                            Reason = $"Изображение содержит метаданные: производитель камеры — {maker}",
                            Confidence = 0.8,
                            Recommendation = "Метаданные изображения обычно безобидны, но могут содержать дополнительную информацию.",
                            TechnicalInfo = $"Производитель: {maker}",
                            FileType = Path.GetExtension(filePath),
                            FileModifiedDate = File.GetLastWriteTime(filePath),
                            FileSize = new FileInfo(filePath).Length
                        });
                    }
                }
            }

            // Проверяем модель камеры (property tag 0x0110)
            var modelPropId = 0x0110;
            if (propertyIds.Contains(modelPropId))
            {
                var modelValue = bitmap.GetPropertyItem(modelPropId);
                if (modelValue != null)
                {
                    var model = System.Text.Encoding.ASCII.GetString(modelValue.Value).TrimEnd('\0');
                    if (!string.IsNullOrEmpty(model))
                    {
                        results.Add(new ScanResult
                        {
                            FilePath = filePath,
                            Category = ScanCategory.ImageMetadata,
                            RiskLevel = RiskLevel.Info,
                            Reason = $"Изображение содержит метаданные: модель камеры — {model}",
                            Confidence = 0.8,
                            Recommendation = "Метаданные изображения обычно безобидны, но могут содержать дополнительную информацию.",
                            TechnicalInfo = $"Модель: {model}",
                            FileType = Path.GetExtension(filePath),
                            FileModifiedDate = File.GetLastWriteTime(filePath),
                            FileSize = new FileInfo(filePath).Length
                        });
                    }
                }
            }

            // Проверяем дату съёмки (property tag 0x9003)
            var datePropId = 0x9003;
            if (propertyIds.Contains(datePropId))
            {
                var dateValue = bitmap.GetPropertyItem(datePropId);
                if (dateValue != null)
                {
                    var dateStr = System.Text.Encoding.ASCII.GetString(dateValue.Value).TrimEnd('\0');
                    if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var dateTaken))
                    {
                        results.Add(new ScanResult
                        {
                            FilePath = filePath,
                            Category = ScanCategory.ImageMetadata,
                            RiskLevel = RiskLevel.Info,
                            Reason = $"Изображение содержит метаданные: дата съёмки — {dateStr}",
                            Confidence = 0.85,
                            Recommendation = "Дата съёмки в EXIF может раскрыть информацию о времени нахождения в определённом месте.",
                            TechnicalInfo = $"Дата съёмки: {dateStr}",
                            FileType = Path.GetExtension(filePath),
                            FileModifiedDate = File.GetLastWriteTime(filePath),
                            FileSize = new FileInfo(filePath).Length
                        });
                    }
                }
            }
        }
        catch (Exception ex) when (ex is FileNotFoundException || ex is ArgumentException || ex is NotSupportedException || ex is OutOfMemoryException || ex is IOException || ex is System.Runtime.InteropServices.ExternalException || ex is ObjectDisposedException)
        {
            // Не поддерживаемый формат, повреждённый файл, нет прав или GDI+ ошибка
        }
        catch
        {
            // Любое другое исключение — пропускаем файл
        }

        return results;
    }

    private long CountFiles(IEnumerable<string> paths, CancellationToken cancellationToken)
    {
        var count = 0L;
        foreach (var path in paths)
        {
            if (cancellationToken.IsCancellationRequested) break;
            try
            {
                if (Directory.Exists(path))
                    count += CountDirectoryFiles(path);
            }
            catch
            {
                // Пропускаем недоступные папки
            }
        }
        return Math.Max(count, 1); // избежим деления на ноль
    }

    private long CountDirectoryFiles(string directory)
    {
        var count = 0L;
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
                count++;
        }
        catch
        {
            // Пропускаем
        }
        return count;
    }

    private static DateTime? GetSafeFileTime(string filePath)
    {
        try
        {
            return File.GetLastWriteTime(filePath);
        }
        catch
        {
            return null;
        }
    }

    private static long? GetSafeFileSize(string filePath)
    {
        try
        {
            return new FileInfo(filePath).Length;
        }
        catch
        {
            return null;
        }
    }
}
