using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;

namespace EasyMount;

public partial class App : System.Windows.Application
{
    private Mutex? singleInstance;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length == 2 && e.Args[0] == "--prepare-uninstall" && int.TryParse(e.Args[1], out var uiLevel))
        {
            var result = await UninstallSupport.RunAsync(Environment.ProcessPath!, uiLevel >= 3 ? message => MessageBox.Show(message, "EasyMount", MessageBoxButton.OK, MessageBoxImage.Information) : null);
            Shutdown(result); return;
        }
        try { UninstallSupport.EnsureNotUninstalling(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "EasyMount"); Shutdown(1); return; }
        string? dataDirectory = null;
        if (e.Args.Length > 0)
        {
            if (e.Args.Length != 2 || e.Args[0] != "--data-dir" || !Path.IsPathFullyQualified(e.Args[1]))
            {
                MessageBox.Show("Sử dụng: EasyMount.exe [--data-dir <đường-dẫn-tuyệt-đối>]", "EasyMount");
                Shutdown(1); return;
            }
            dataDirectory = Path.GetFullPath(e.Args[1]);
        }
        var instanceSuffix = dataDirectory is null ? "Desktop" : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dataDirectory.ToUpperInvariant())))[..16];
        singleInstance = new Mutex(true, @"Local\EasyMount." + instanceSuffix, out bool first);
        if (!first)
        {
            MessageBox.Show("EasyMount đang chạy. Mở ứng dụng từ biểu tượng ở khay hệ thống.", "EasyMount");
            Shutdown();
            return;
        }
        try
        {
            UninstallSupport.RegisterRoot(dataDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyMount"));
            MainWindow = new MainWindow(dataDirectory); MainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể mở EasyMount: " + ex.Message, "EasyMount");
            Shutdown(1);
        }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        singleInstance?.Dispose();
        base.OnExit(e);
    }
}
