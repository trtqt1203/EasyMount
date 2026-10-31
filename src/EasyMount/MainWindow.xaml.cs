using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace EasyMount;

public partial class MainWindow : Window
{
    private readonly DriveService service;
    private readonly Forms.NotifyIcon tray;
    private readonly UninstallSupport uninstall;
    private bool busy, exiting, canSave = true;
    public ObservableCollection<DriveRow> Rows { get; } = new();

    public MainWindow(string? dataDirectory = null)
    {
        service = new DriveService(dataDirectory);
        InitializeComponent();
        DataContext = this;
        tray = new Forms.NotifyIcon { Text = "EasyMount", Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? System.Drawing.SystemIcons.Application, Visible = true };
        tray.DoubleClick += (_, _) => Restore();
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Mở EasyMount", null, (_, _) => Restore());
        menu.Items.Add("Thoát", null, async (_, _) => await QuitAsync());
        tray.ContextMenuStrip = menu;
        service.StateChanged += s => Dispatcher.BeginInvoke(new Action(() =>
        {
            var row = Rows.FirstOrDefault(r => r.Profile.Id == s.ProfileId);
            row?.Update(s);
            RefreshSummary();
        }));
        try { foreach (var p in service.LoadProfiles()) Rows.Add(new DriveRow(p)); }
        catch (Exception ex) { canSave = false; StatusText.Text = "Không thể đọc cấu hình: " + ex.Message; }
        RefreshSummary();
        uninstall = new UninstallSupport(
            () => Dispatcher.InvokeAsync(PrepareForUninstallAsync).Task.Unwrap(),
            () => Dispatcher.BeginInvoke(new Action(FinishExit)));
        Closed += (_, _) => uninstall.Dispose();
        Loaded += async (_, _) =>
        {
            ApplyDarkTitle();
            await RunAsync(() => service.RecoverMountsAsync(), "Đã kiểm tra và khôi phục các phiên ổ còn chạy.");
            if (!service.HasRclone) StatusText.Text = "Thiếu thành phần kết nối. Chạy lại bộ cài EasyMount để khôi phục.";
            if (canSave && service.HasWinFsp && Rows.Any(r => r.Profile.AutoConnect))
                await RunAsync(async () => { foreach (var row in Rows.Where(r => r.Profile.AutoConnect)) { try { await service.ConnectAsync(row.Profile); } catch (Exception ex) { row.Update(new(row.Profile.Id, "Error", ex.Message)); } } }, "Đã xử lý các ổ tự kết nối.");
        };
    }

    private void RefreshSummary()
    {
        EmptyPanel.Visibility = Rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        SummaryText.Text = $"{Rows.Count} ổ đĩa  /  {Rows.Count(r => service.IsConnected(r.Profile.Id))} đang kết nối";
        DriverBanner.Visibility = service.HasWinFsp ? Visibility.Collapsed : Visibility.Visible;
        EngineText.Text = service.HasRclone ? "●  rclone sẵn sàng" : "○  Thiếu rclone";
    }

    private async Task RunAsync(Func<Task> action, string done)
    {
        if (busy) return;
        busy = true;
        IsEnabled = false;
        StatusText.Text = "Đang xử lý…";
        try { await action(); StatusText.Text = done; }
        catch (Exception ex) { StatusText.Text = ex.Message; MessageBox.Show(this, ex.Message, "EasyMount", MessageBoxButton.OK, MessageBoxImage.Information); }
        finally { busy = false; IsEnabled = true; RefreshSummary(); }
    }

    private async void AddClick(object sender, RoutedEventArgs e) => await EditAsync(null);
    private async void EditClick(object sender, RoutedEventArgs e) => await EditAsync(((FrameworkElement)sender).Tag as DriveRow);
    private async Task EditAsync(DriveRow? row)
    {
        if (busy || !canSave) return;
        if (row is not null && service.IsConnected(row.Profile.Id)) { MessageBox.Show(this, "Ngắt kết nối ổ trước khi sửa.", "EasyMount"); return; }
        await RunAsync(async () =>
        {
            var accounts = await service.ListRemotesAsync();
            if (accounts.Count == 0)
            {
                if (!ShowSetup("Google Drive / OneDrive")) return;
                accounts = await service.ListRemotesAsync();
                if (accounts.Count == 0) return;
            }
            var editor = new DriveEditor(service, accounts, Rows.Select(r => r.Profile).ToList(), row?.Profile) { Owner = this };
            if (editor.ShowDialog() == true && editor.Result is not null)
            {
                var next = editor.Result;
                service.SaveProfiles(Rows.Where(r => r != row).Select(r => r.Profile).Append(next));
                if (row is null) Rows.Add(new DriveRow(next)); else Rows[Rows.IndexOf(row)] = new DriveRow(next);
            }
        }, "Cấu hình ổ đĩa đã sẵn sàng.");
    }
    private async void ToggleClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is not DriveRow row) return;
        await RunAsync(async () => { if (service.IsConnected(row.Profile.Id)) await service.DisconnectAsync(row.Profile.Id); else await service.ConnectAsync(row.Profile); }, "Đã cập nhật kết nối.");
    }
    private void OpenClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is not DriveRow row) return;
        if (!service.IsConnected(row.Profile.Id)) { MessageBox.Show(this, "Kết nối ổ đĩa trước khi mở trong Explorer.", "EasyMount"); return; }
        try { Process.Start(new ProcessStartInfo("explorer.exe") { ArgumentList = { row.Profile.Letter + "\\" }, UseShellExecute = true }); }
        catch (Exception ex) { StatusText.Text = ex.Message; }
    }
    private void RemoveClick(object sender, RoutedEventArgs e)
    {
        if (busy || ((FrameworkElement)sender).Tag is not DriveRow row) return;
        if (service.IsConnected(row.Profile.Id)) { MessageBox.Show(this, "Ngắt kết nối ổ trước khi xóa cấu hình.", "EasyMount"); return; }
        if (MessageBox.Show(this, $"Xóa cấu hình “{row.Name}”? File trên cloud được giữ nguyên.", "EasyMount", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { service.SaveProfiles(Rows.Where(r => r != row).Select(r => r.Profile)); Rows.Remove(row); RefreshSummary(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "EasyMount"); }
    }
    private async void RefreshClick(object sender, RoutedEventArgs e) => await RunAsync(async () => { var remotes = await service.ListRemotesAsync(); StatusText.Text = $"{remotes.Count} tài khoản đã cấu hình."; }, "Đã làm mới. Dùng Thêm ổ đĩa để chọn tài khoản.");
    private void AccountsClick(object sender, RoutedEventArgs e) => ShowSetup("Google Drive / OneDrive");
    private void GoogleClick(object sender, RoutedEventArgs e) => ShowSetup("Google Drive");
    private void OneDriveClick(object sender, RoutedEventArgs e) => ShowSetup("OneDrive");
    private bool ShowSetup(string provider)
    {
        var dialog = new SetupWindow(service, provider) { Owner = this };
        return dialog.ShowDialog() == true;
    }
    private void HelpClick(object sender, RoutedEventArgs e) => ShowSetup("Google Drive / OneDrive");
    private void AboutClick(object sender, RoutedEventArgs e) => new AboutWindow { Owner = this }.ShowDialog();
    private void LogsClick(object sender, RoutedEventArgs e) { try { service.OpenLogs(); } catch (Exception ex) { StatusText.Text = ex.Message; } }
    private void WinFspClick(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("https://winfsp.dev/rel/") { UseShellExecute = true });
    private void Restore() { Dispatcher.Invoke(() => { Show(); WindowState = WindowState.Normal; Activate(); }); }
    private void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (exiting) return;
        e.Cancel = true;
        if (!busy) Dispatcher.BeginInvoke(new Action(async () => await QuitAsync()));
        else Restore();
    }
    private async void QuitClick(object sender, RoutedEventArgs e) => await QuitAsync();
    private async Task QuitAsync()
    {
        if (busy) { Restore(); return; }
        Restore();
        await RunAsync(async () =>
        {
            await service.DisconnectAllAsync();
            FinishExit();
        }, "Đã đóng.");
    }
    private async Task<string?> PrepareForUninstallAsync()
    {
        if (busy || OwnedWindows.Count > 0) return "Đóng cửa sổ cấu hình và chờ thao tác hiện tại hoàn tất trước khi gỡ.";
        busy = true; IsEnabled = false;
        StatusText.Text = "Đang ngắt ổ an toàn trước khi gỡ ứng dụng…";
        try { await service.PrepareUninstallAsync(); return null; }
        catch (Exception ex) { StatusText.Text = ex.Message; return ex.Message; }
        finally { busy = false; IsEnabled = true; RefreshSummary(); }
    }
    private void FinishExit()
    {
        exiting = true; tray.Visible = false; tray.Dispose(); service.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
    private void ApplyDarkTitle() { int enabled = 1; try { DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 20, ref enabled, sizeof(int)); } catch { } }
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}

public sealed class DriveRow : INotifyPropertyChanged
{
    public DriveProfile Profile { get; }
    public string Name => Profile.Name;
    public string Letter => Profile.Letter;
    public string Remote => Profile.IsPool ? $"Ổ gộp · {Profile.Sources.Count} nguồn: " + string.Join(" → ", Profile.Sources.Select(s => s.Remote)) : Profile.Remote;
    public string ModeLabel => (Profile.ReadOnly ? "Chỉ đọc" : "Đọc & ghi") + (Profile.AutoConnect ? "  ·  Tự kết nối" : "");
    private string status = "Disconnected";
    public string? Error { get; private set; }
    public string StatusLabel => status switch { "Connected" => "●  Đã kết nối", "Connecting" => "◌  Đang kết nối…", "Disconnecting" => "◌  Đang ngắt…", "Error" => "●  Cần kiểm tra", _ => "○  Chưa kết nối" };
    public string StatusColor => status == "Connected" ? "#66DDB3" : status == "Error" ? "#F3AC9F" : "#95A4B7";
    public string ToggleLabel => status == "Connected" ? "Ngắt kết nối" : "Kết nối";
    public DriveRow(DriveProfile profile) { Profile = profile; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public void Update(DriveSnapshot snapshot) { status = snapshot.Status; Error = snapshot.Error; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); }
}
