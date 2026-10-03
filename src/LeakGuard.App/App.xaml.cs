using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LeakGuard.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Обработка необработанных исключений
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        var msg = $"Фоновая ошибка:\n{e.Exception.Message}\n\n{e.Exception.StackTrace}";
        File.AppendAllText("C:\\temp\\leakguard_crash.log", $"[{DateTime.Now}] Task: {msg}\n\n");
        e.SetObserved();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var msg = $"Произошла непредвиденная ошибка:\n{e.Exception.Message}\n\n{e.Exception.StackTrace}";
        MessageBox.Show(msg, "LeakGuard — Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        File.AppendAllText("C:\\temp\\leakguard_crash.log", $"[{DateTime.Now}] Dispatcher: {msg}\n\n");
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var msg = "Критическая ошибка";
        if (e.ExceptionObject is Exception ex)
        {
            msg = $"Критическая ошибка:\n{ex.Message}\n\n{ex.StackTrace}";
            File.AppendAllText("C:\\temp\\leakguard_crash.log", $"[{DateTime.Now}] Unhandled: {msg}\n\n");
        }
        MessageBox.Show(msg, "LeakGuard — Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
