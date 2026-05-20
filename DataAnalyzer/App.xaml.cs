using System;
using System.IO;
using System.Windows;

namespace DataAnalyzer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_log.txt"),
                    $"[{DateTime.Now:HH:mm:ss.fff}] OnStartup entered\n");
            }
            catch { }

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                var msg = $"未处理异常:\n{ex?.Message}\n\n{ex?.StackTrace}";
                try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), msg); } catch { }
                MessageBox.Show(msg, "致命错误", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            DispatcherUnhandledException += (s, args) =>
            {
                var msg = $"UI线程异常:\n{args.Exception.Message}\n\n{args.Exception.StackTrace}";
                try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), msg); } catch { }
                MessageBox.Show(msg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
            base.OnStartup(e);
        }
    }
}
