using System.Diagnostics;
using System.Security.Principal;

namespace LeakGuard.Core.Services;

/// <summary>
/// Менеджер управления автозапуском через задачу планировщика Windows.
/// </summary>
public class StartupManager
{
    private const string TaskName = "LeakGuard";
    private const string TaskDescription = "LeakGuard — лёгкий модуль уведомлений";

    /// <summary>
    /// Проверяет, включён ли автозапуск.
    /// </summary>
    public bool IsEnabled()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Query /TN \"{TaskName}\" /XML",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process?.WaitForExit();
            var output = process?.StandardOutput.ReadToEnd() ?? string.Empty;
            return output.Contains(TaskName, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Включает автозапуск через задачу планировщика.
    /// </summary>
    public bool Enable()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                return false;

            // Создаём задачу в планировщике
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Create /TN \"{TaskName}\" /TR \"\\\"{exePath}\\\"\" /SC ONLOGON /RL HIGHEST /F /TR \"\\\"{exePath}\\\"\" /D * /IT",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas" // запрос UAC
            });

            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Отключает автозапуск (удаляет задачу).
    /// </summary>
    public bool Disable()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Delete /TN \"{TaskName}\" /F",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Возвращает описание, как удалить задачу вручную.
    /// </summary>
    public string GetManualUninstallInstructions()
    {
        return @"Для удаления автозапуска вручную:
1. Откройте «Планировщик задач» (Win+R → taskschd.msc)
2. В дереве слева найдите «Библиотека планировщика»
3. Найдите задачу «LeakGuard»
4. Нажмите правой кнопкой → «Удалить»

Или через командную строку (администратор):
  schtasks /Delete /TN ""LeakGuard"" /F";
    }
}
