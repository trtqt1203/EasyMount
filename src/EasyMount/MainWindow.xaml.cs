using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Input;
using Forms = System.Windows.Forms;

namespace EasyMount;

public partial class MainWindow : Window
{
    private readonly DriveService service;
    private readonly Forms.NotifyIcon tray;
    private readonly UninstallSupport uninstall;
    private bool busy, exiting, closeAfterOperation, canSave = true;
    private bool gridView;
    private WorkspaceStore workspaces = new();
    private DesktopSettings settings = new();
    private readonly HashSet<string> wantedConnections = new();
    private readonly HashSet<string> capacityWarnings = new();
    private readonly Dictionary<string, DateTime> retryAfter = new();
    private readonly System.Windows.Threading.DispatcherTimer reconnectTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private string WorkspacePath => Path.Combine(service.DataDirectory, "workspaces.json");
    private string SettingsPath => Path.Combine(service.DataDirectory, "desktop-settings.json");
    private double preferredRowHeight = 62;
    private readonly HashSet<string> capacityRequests = new();
    private readonly System.Windows.Threading.DispatcherTimer relativeTimer = new() { Interval = TimeSpan.FromMinutes(1) };
    public ObservableCollection<DriveRow> Rows { get; } = new();

    public MainWindow(string? dataDirectory = null)
    {
        service = new DriveService(dataDirectory);
        Theme.Apply("dark");
        InitializeComponent();
        DataContext = this;
        L.Current.PropertyChanged += OnLanguageChanged;
        Theme.Changed += OnThemeChanged;
        Closed += (_, _) => { L.Current.PropertyChanged -= OnLanguageChanged; Theme.Changed -= OnThemeChanged; };
        tray = new Forms.NotifyIcon { Text = "EasyMount", Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? System.Drawing.SystemIcons.Application, Visible = true };
        tray.DoubleClick += (_, _) => Restore();
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(L.T("Mở EasyMount"), null, (_, _) => Restore());
        menu.Items.Add(L.T("Thoát"), null, async (_, _) => await QuitAsync());
        tray.ContextMenuStrip = menu;
        service.StateChanged += s => Dispatcher.BeginInvoke(new Action(async () =>
        {
            var row = Rows.FirstOrDefault(r => r.Profile.Id == s.ProfileId);
            var wasConnected = row?.IsConnected == true;
            row?.Update(s);
            if (s.Status == "Connected") { wantedConnections.Add(s.ProfileId); if (!wasConnected && settings.NotifyConnected) Notify(L.T("Đã kết nối"), row?.Name ?? L.T("Ổ đĩa")); }
            if (s.Status == "Error" && wantedConnections.Contains(s.ProfileId)) { retryAfter[s.ProfileId] = DateTime.UtcNow.AddSeconds(settings.RetrySeconds); if (settings.NotifyDisconnected) Notify(L.T("Mất kết nối"), row?.Name ?? L.T("Ổ đĩa")); }
            RefreshSummary();
            if (s.Status == "Connected" && row is not null) await RefreshCapacityAsync(row);
        }));
        try { foreach (var p in service.LoadProfiles()) Rows.Add(new DriveRow(p, () => Rows.Select(r => r.Profile))); }
        catch (Exception ex) { canSave = false; StatusText.Text = L.T("Không thể đọc cấu hình: ") + ex.Message; }
        try { workspaces = WorkspaceStore.Load(WorkspacePath); settings = DesktopSettings.Load(SettingsPath); }
        catch (Exception ex) { canSave = false; StatusText.Text = L.T("Không đọc được tùy chọn: ") + ex.Message; }
        ApplyAppearance();
        service.MountTimeoutSeconds = settings.TimeoutSeconds;
        service.MountLogLevel = settings.Logging ? settings.LogLevel : "ERROR";
        LoadUiPreferences();
        UpdateWorkspaceLabel();
        RefreshSummary();
        uninstall = new UninstallSupport(
            () => Dispatcher.InvokeAsync(PrepareForUninstallAsync).Task.Unwrap(),
            () => Dispatcher.BeginInvoke(new Action(FinishExit)));
        relativeTimer.Tick += (_, _) => { foreach (var row in Rows) row.RefreshTime(); }; relativeTimer.Start();
        reconnectTimer.Tick += async (_, _) => await RetryConnectionsAsync(); reconnectTimer.Start();
        Closed += (_, _) => { reconnectTimer.Stop(); relativeTimer.Stop(); uninstall.Dispose(); };
        Loaded += async (_, _) =>
        {
            ApplyDarkTitle();
            await RefreshAccountLabelsAsync();
            await RunAsync(() => service.RecoverMountsAsync(), L.T("Đã kiểm tra và khôi phục các phiên ổ còn chạy."));
            DriveItems.SelectedItem = Rows.FirstOrDefault(r => r.IsConnected) ?? Rows.FirstOrDefault();
            if (!service.HasRclone) StatusText.Text = L.T("Thiếu thành phần kết nối. Chạy lại bộ cài EasyMount để khôi phục.");
            if (settings.AutoConnect && canSave && service.HasWinFsp && Rows.Any(r => r.Profile.AutoConnect))
                await RunAsync(async () => { foreach (var row in Rows.Where(r => r.Profile.AutoConnect)) { try { wantedConnections.Add(row.Profile.Id); await service.ConnectAsync(row.Profile); } catch (Exception ex) { row.Update(new(row.Profile.Id, "Error", ex.Message)); } } }, L.T("Đã xử lý các ổ tự kết nối."));
        };
    }

    private void ApplyAppearance()
    {
        L.SetLanguage(settings.Language == "vi" ? "vi" : "en");
        Theme.Apply(settings.Theme == "light" ? "light" : "dark");
    }
    private void OnThemeChanged(object? sender, EventArgs e)
    {
        foreach (var row in Rows) row.RefreshLanguage();
        ApplyDarkTitle();
    }
    private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
    {
        foreach (var row in Rows) row.RefreshLanguage();
        UpdateWorkspaceLabel(); RefreshSummary();
        RefreshButtonText.Text = L.T(RefreshButton.IsEnabled ? "Làm mới" : "Đang làm mới...");
        StatusText.Text = L.T("Đã cập nhật kết nối.");
        tray.ContextMenuStrip!.Items[0].Text = L.T("Mở EasyMount");
        tray.ContextMenuStrip.Items[1].Text = L.T("Thoát");
    }

    private void RefreshSummary()
    {
        EmptyPanel.Visibility = Rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        SummaryText.Text = Rows.Count(r => workspaces.Contains(r.Profile.Id)).ToString();
        NavCountText.Text = Rows.Count(r => workspaces.Contains(r.Profile.Id)).ToString();
        DriveCountText.Text = Rows.Count(r => workspaces.Contains(r.Profile.Id)).ToString();
        ConnectedCountText.Text = Rows.Count(r => service.IsConnected(r.Profile.Id)).ToString();
        ApplyFilter();
        DriverBanner.Visibility = service.HasWinFsp ? Visibility.Collapsed : Visibility.Visible;
        EngineText.Text = L.CopyrightVersionLabel;
        OnlineStatusText.Text = L.T($"{Rows.Count(r => r.IsConnected)} / {Rows.Count} ổ đĩa online  |");
        RefreshStorageStats();
    }

    private async Task RefreshAccountLabelsAsync()
    {
        try { var accounts = await service.ListRemotesAsync(); foreach (var row in Rows) row.SetAccounts(accounts); AccountCountText.Text = accounts.Count.ToString(); ApplyFilter(); }
        catch { /* A label lookup must not prevent session recovery. */ }
    }
    private void SearchChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void FilterChanged(object sender, RoutedEventArgs e) => ApplyFilter();
    private void ApplyFilter()
    {
        if (SearchBox is null || DriveItems is null || NoResultsText is null || DropboxOnly is null || DisconnectedOnly is null) return;
        var query = SearchBox.Text.Trim();
        var provider = GoogleDriveOnly.IsChecked == true ? "drive" : OneDriveOnly.IsChecked == true ? "onedrive" : DropboxOnly.IsChecked == true ? "dropbox" : "";
        var view = CollectionViewSource.GetDefaultView(Rows);
        view.Filter = item => item is DriveRow row && workspaces.Contains(row.Profile.Id) && (ConnectedOnly.IsChecked != true || row.IsConnected)
            && (DisconnectedOnly.IsChecked != true || !row.IsConnected)
            && (provider.Length == 0 || row.MatchesProvider(provider))
            && (query.Length == 0 || row.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || row.Letter.Contains(query, StringComparison.OrdinalIgnoreCase) || row.Remote.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        NoResultsText.Visibility = Rows.Count > 0 && view.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
    }
    private void UpdatesClick(object sender, RoutedEventArgs e) => new UpdatesWindow { Owner = this }.ShowDialog();
    private void ProviderFilterChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private async void DriveSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshStorageStats();
        if (DriveItems.SelectedItem is DriveRow row && row.IsConnected && (row.Capacity is null || !settings.CacheMetadata)) await RefreshCapacityAsync(row);
    }
    private async Task RefreshCapacityAsync(DriveRow row)
    {
        if (!row.IsConnected || !capacityRequests.Add(row.Profile.Id)) return;
        try { var capacity = await service.GetCapacityAsync(row.Profile); row.SetCapacity(row.IsConnected ? capacity : null);
            if (capacity is not null && row.CapacityPercent >= settings.CapacityThreshold && settings.NotifyCapacity && capacityWarnings.Add(row.Profile.Id)) Notify(L.T("Dung lượng gần đầy"), row.Name + " · " + row.CapacityPercent.ToString("0") + "%");
            if (row.CapacityPercent < settings.CapacityThreshold) capacityWarnings.Remove(row.Profile.Id);
        }
        catch { row.SetCapacity(null); }
        finally { capacityRequests.Remove(row.Profile.Id); RefreshStorageStats(); }
    }
    private void RefreshStorageStats()
    {
        if (UsedStorageText is null) return;
        var capacity = DrivePresentation.WorkspaceCapacity(Rows.Where(r => workspaces.Contains(r.Profile.Id)).Select(r => (r.Profile, r.Capacity)));
        UsedStorageText.Text = capacity is not null ? DriveRow.FormatBytes(capacity.UsedBytes) : "—";
        var percent = capacity is not null ? 100.0 * capacity.UsedBytes / capacity.TotalBytes : 0;
        StorageDetailsText.Text = capacity is not null ? "/ " + DriveRow.FormatBytes(capacity.TotalBytes) + "  ·  " + percent.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%" : L.T("Chưa đủ dữ liệu dung lượng");
        WorkspaceStorageBar.Value = percent;
        WorkspaceStorageBar.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(percent > 90 ? "#F87171" : percent >= 70 ? "#F59E0B" : "#00E5B4")!;
    }
    private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DriveInspector is not null) DriveInspector.Visibility = ActualWidth >= 1560 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void ApplyView()
    {
        DriveItems.Visibility = gridView ? Visibility.Collapsed : Visibility.Visible;
        CardScroll.Visibility = gridView ? Visibility.Visible : Visibility.Collapsed;
        DriveItems.MinRowHeight = preferredRowHeight;
        ListViewButton.Style = (Style)FindResource(gridView ? "Ghost" : "Tonal");
        GridViewButton.Style = (Style)FindResource(gridView ? "Tonal" : "Ghost");
    }
    private void ListViewClick(object sender, RoutedEventArgs e) { gridView = false; ApplyView(); SaveUiPreferences(); }
    private void GridViewClick(object sender, RoutedEventArgs e) { gridView = true; ApplyView(); SaveUiPreferences(); }
    private string UiPreferencesPath => Path.Combine(service.DataDirectory, "ui-settings.json");
    private sealed record UiPreferences(bool GridView, bool Compact);
    private void LoadUiPreferences()
    {
        try { if (File.Exists(UiPreferencesPath) && JsonSerializer.Deserialize<UiPreferences>(File.ReadAllText(UiPreferencesPath)) is { } value) { gridView = value.GridView; preferredRowHeight = value.Compact ? 62 : 76; } }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { StatusText.Text = L.T("Không đọc được tùy chọn giao diện; đang dùng mặc định."); }
        ApplyView();
    }
    private void SaveUiPreferences()
    {
        try { Directory.CreateDirectory(service.DataDirectory); File.WriteAllText(UiPreferencesPath, JsonSerializer.Serialize(new UiPreferences(gridView, preferredRowHeight == 62))); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { StatusText.Text = L.T("Giao diện đã đổi nhưng chưa lưu được tùy chọn."); }
    }
    private void SettingsClick(object sender, RoutedEventArgs e)
    {
        new SettingsWindow(settings, () => { settings.Save(SettingsPath); ApplyAppearance(); service.MountTimeoutSeconds = settings.TimeoutSeconds; service.MountLogLevel = settings.Logging ? settings.LogLevel : "ERROR"; }, () => { foreach (var row in Rows) row.SetCapacity(null); RefreshStorageStats(); }) { Owner = this }.ShowDialog();
    }
    private void Notify(string title, string message)
    {
        tray.ShowBalloonTip(4000, title, message, Forms.ToolTipIcon.Info);
        if (settings.Sound) System.Media.SystemSounds.Asterisk.Play();
    }
    private async Task RetryConnectionsAsync()
    {
        if (!settings.Retry || busy || exiting || !canSave || OwnedWindows.Count > 0) return;
        var row = Rows.FirstOrDefault(r => wantedConnections.Contains(r.Profile.Id) && !r.IsConnected && retryAfter.TryGetValue(r.Profile.Id, out var at) && at <= DateTime.UtcNow);
        if (row is null) return;
        busy = true;
        try { retryAfter[row.Profile.Id] = DateTime.UtcNow.AddSeconds(settings.RetrySeconds); await service.ConnectAsync(row.Profile); }
        catch { StatusText.Text = L.T("Chưa kết nối lại được “") + row.Name + L.T("”. Sẽ thử lại sau."); }
        finally { busy = false; RefreshSummary(); }
    }
    private void UpdateWorkspaceLabel()
    {
        var active = workspaces.Items.FirstOrDefault(w => w.Id == workspaces.Active) ?? new WorkspaceInfo { Id = "", Name = L.T("Tất cả ổ đĩa"), Color = "#00E5B4" };
        WorkspaceNameText.Text = workspaces.Items.Count == 0 ? L.T("Chưa có workspace") : active.Name;
        WorkspaceBreadcrumb.Text = L.T("Workspace   ›   ") + active.Name;
        WorkspaceDot.Visibility = workspaces.Active == "" ? Visibility.Collapsed : Visibility.Visible;
        WorkspaceDot.Fill = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(active.Color)!;
    }
    private void WorkspaceClick(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        foreach (var workspace in new[] { new WorkspaceInfo { Id = "", Name = L.T("Tất cả ổ đĩa"), Color = "#00E5B4" } }.Concat(workspaces.Items))
        {
            var item = new MenuItem { Header = workspace.Name, Icon = new System.Windows.Shapes.Ellipse { Width=8, Height=8, Fill=(System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(workspace.Color)! }, IsCheckable = true, IsChecked = workspace.Id == workspaces.Active };
            item.Click += (_, _) => { var old = workspaces.Active; try { workspaces.Active = workspace.Id; workspaces.Save(WorkspacePath); UpdateWorkspaceLabel(); RefreshSummary(); DriveItems.SelectedItem = Rows.FirstOrDefault(r => workspaces.Contains(r.Profile.Id)); } catch(Exception ex) { workspaces.Active = old; StatusText.Text = ex.Message; } }; menu.Items.Add(item);
        }
        menu.Items.Add(new Separator()); var create = new MenuItem { Header = L.T("＋ Tạo workspace mới"), IsEnabled = canSave };
        create.Click += (_, _) =>
        {
            var dialog = new Window { Title = L.T("Tạo workspace · EasyMount"), Width = 390, Height = 210, ResizeMode = ResizeMode.NoResize, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, Style = (Style)FindResource(typeof(Window)) };
            var panel = new StackPanel { Margin = new Thickness(24) }; var name = new TextBox { MaxLength = 40 }; System.Windows.Automation.AutomationProperties.SetName(name,L.T("Tên workspace"));
            panel.Children.Add(new TextBlock { Text = L.T("Tên workspace"), Margin = new Thickness(0,0,0,10) }); panel.Children.Add(name); var save = new Button { Content = L.T("Tạo workspace"), IsDefault = true, Margin = new Thickness(0,14,0,0), Style = (Style)FindResource("Primary") }; panel.Children.Add(save);dialog.Content=panel;
            dialog.Loaded += (_, _) => name.Focus();
            save.Click += (_, _) => { try { var rollback = JsonSerializer.Serialize(workspaces); try { workspaces.Create(name.Text); workspaces.Save(WorkspacePath); } catch { workspaces = JsonSerializer.Deserialize<WorkspaceStore>(rollback)!; throw; } UpdateWorkspaceLabel(); RefreshSummary(); dialog.Close(); } catch(Exception ex) { MessageBox.Show(dialog,ex.Message,"EasyMount"); } }; dialog.ShowDialog();
        }; menu.Items.Add(create);
        if (workspaces.Items.FirstOrDefault(w => w.Id == workspaces.Active) is { } selected)
        {
            menu.Items.Add(new Separator());
            var delete = new MenuItem { Header = L.T("Xóa workspace đang chọn"), IsEnabled = canSave };
            delete.Click += (_, _) =>
            {
                if (MessageBox.Show(this, selected.Name + "\n\n" + L.T("Xóa workspace này? Các ổ đĩa và file được giữ nguyên."), "EasyMount", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                var rollback = JsonSerializer.Serialize(workspaces);
                try
                {
                    File.Copy(WorkspacePath, WorkspacePath + ".before-delete-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"), false);
                    workspaces.Delete(selected.Id); workspaces.Save(WorkspacePath);
                    UpdateWorkspaceLabel(); RefreshSummary();
                    DriveItems.SelectedItem = Rows.FirstOrDefault();
                }
                catch (Exception ex) { workspaces = JsonSerializer.Deserialize<WorkspaceStore>(rollback)!; StatusText.Text = ex.Message; }
            };
            menu.Items.Add(delete);
        }
        menu.PlacementTarget=WorkspaceButton;menu.Placement=System.Windows.Controls.Primitives.PlacementMode.Bottom;menu.MinWidth=WorkspaceButton.ActualWidth;menu.IsOpen=true;
    }
    private void PrimaryActionClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is not DriveRow row) return;
        DriveItems.SelectedItem = row;
        if (row.IsConnected) OpenClick(sender, e); else ToggleClick(sender, e);
    }
    private void MoreClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not DriveRow row) return;
        var menu = new ContextMenu();
        var toggle = new MenuItem { Header = row.IsConnected ? L.T("Ngắt kết nối an toàn") : L.T("Kết nối ổ đĩa"), Tag = row };
        toggle.Click += ToggleClick; menu.Items.Add(toggle);
        var edit = new MenuItem { Header = L.T("Sửa cấu hình"), Tag = row }; edit.Click += EditClick; menu.Items.Add(edit);
        var remove = new MenuItem { Header = L.T("Xóa cấu hình ổ đĩa"), Tag = row }; remove.Click += RemoveClick; menu.Items.Add(remove);
        var move = new MenuItem { Header = L.T("Chuyển đến workspace"), IsEnabled = canSave };
        foreach (var w in new[] { new WorkspaceInfo { Id = "", Name = L.T("Không thuộc workspace") } }.Concat(workspaces.Items)) { var target = new MenuItem { Header = w.Name }; target.Click += (_, _) => { var old = workspaces.Assignments.TryGetValue(row.Profile.Id,out var assigned) ? assigned : null; try { if (w.Id == "") workspaces.Assignments.Remove(row.Profile.Id); else workspaces.Assignments[row.Profile.Id] = w.Id; workspaces.Save(WorkspacePath); RefreshSummary(); } catch(Exception ex) { if(old is null) workspaces.Assignments.Remove(row.Profile.Id); else workspaces.Assignments[row.Profile.Id]=old; StatusText.Text=ex.Message; } }; move.Items.Add(target); } menu.Items.Add(move);
        menu.PlacementTarget = button; menu.IsOpen = true;
    }

    private async Task RunAsync(Func<Task> action, string done)
    {
        if (busy) return;
        busy = true;
        IsEnabled = false;
        StatusText.Text = L.T("Đang xử lý…");
        try { await action(); StatusText.Text = done; }
        catch (Exception ex) { StatusText.Text = ex.Message; MessageBox.Show(this, ex.Message, "EasyMount", MessageBoxButton.OK, MessageBoxImage.Information); }
        finally
        {
            busy = false; IsEnabled = true; RefreshSummary();
            if (closeAfterOperation && !exiting)
            {
                closeAfterOperation = false;
                _ = Dispatcher.BeginInvoke(new Action(async () => await QuitAsync()));
            }
        }
    }

    private async void AddClick(object sender, RoutedEventArgs e) => await EditAsync(null);
    private async void EditClick(object sender, RoutedEventArgs e) => await EditAsync(((FrameworkElement)sender).Tag as DriveRow);
    private async Task EditAsync(DriveRow? row)
    {
        if (busy || !canSave) return;
        if (row is not null && service.IsConnected(row.Profile.Id)) { MessageBox.Show(this, L.T("Ngắt kết nối ổ trước khi sửa."), "EasyMount"); return; }
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
                if (row is null) { Rows.Add(new DriveRow(next, () => Rows.Select(r => r.Profile))); if (workspaces.Active != "") workspaces.Assignments[next.Id] = workspaces.Active; workspaces.Save(WorkspacePath); } else Rows[Rows.IndexOf(row)] = new DriveRow(next, () => Rows.Select(r => r.Profile));
                await RefreshAccountLabelsAsync();
            }
        }, L.T("Cấu hình ổ đĩa đã sẵn sàng."));
    }
    private async void ToggleClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is not DriveRow row) return;
        await RunAsync(async () => { if (service.IsConnected(row.Profile.Id)) { await service.DisconnectAsync(row.Profile.Id); wantedConnections.Remove(row.Profile.Id); retryAfter.Remove(row.Profile.Id); } else { wantedConnections.Add(row.Profile.Id); await service.ConnectAsync(row.Profile); } }, L.T("Đã cập nhật kết nối."));
    }
    private void OpenClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is not DriveRow row) return;
        if (!service.IsConnected(row.Profile.Id)) { MessageBox.Show(this, L.T("Kết nối ổ đĩa trước khi mở trong Explorer."), "EasyMount"); return; }
        try { Process.Start(new ProcessStartInfo("explorer.exe") { ArgumentList = { row.Profile.Letter + "\\" }, UseShellExecute = true }); }
        catch (Exception ex) { StatusText.Text = ex.Message; }
    }
    private void RemoveClick(object sender, RoutedEventArgs e)
    {
        if (busy || ((FrameworkElement)sender).Tag is not DriveRow row) return;
        if (service.IsConnected(row.Profile.Id)) { MessageBox.Show(this, L.T("Ngắt kết nối ổ trước khi xóa cấu hình."), "EasyMount"); return; }
        if (MessageBox.Show(this, L.T($"Xóa cấu hình “{row.Name}”? File trên cloud được giữ nguyên."), "EasyMount", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { service.SaveProfiles(Rows.Where(r => r != row).Select(r => r.Profile)); Rows.Remove(row); RefreshSummary(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "EasyMount"); }
    }
    private async void RefreshClick(object sender, RoutedEventArgs e) => await RefreshAllAsync();
    private async void WindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.R || (Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        e.Handled = true;
        if (busy) return;
        _ = FlashRefreshAsync();
        await RefreshAllAsync();
    }
    private async Task FlashRefreshAsync()
    {
        var previous = RefreshButton.Background;
        RefreshButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(18,59,49));
        await Task.Delay(300);
        RefreshButton.Background = previous;
    }
    private async Task RefreshAllAsync()
    {
        if (busy) return;
        RefreshSpinner.Visibility = Visibility.Visible; RefreshButtonText.Text = L.T("Đang làm mới..."); RefreshButton.IsEnabled = false;
        try
        {
            await RunAsync(async () => {
                var minimumFeedback = Task.Delay(1500);
                await service.RecoverMountsAsync();
                await RefreshAccountLabelsAsync();
                var rows = Rows.Where(r => workspaces.Contains(r.Profile.Id)).ToArray();
                foreach (var row in rows) { row.Update(new(row.Profile.Id, service.IsConnected(row.Profile.Id) ? "Connected" : "Disconnected")); if (!row.IsConnected) row.SetCapacity(null); }
                await Task.WhenAll(rows.Select(RefreshCapacityAsync));
                await minimumFeedback;
            }, L.T("Đã làm mới tất cả ổ đĩa · vừa xong"));
        }
        finally { RefreshSpinner.Visibility = Visibility.Collapsed; RefreshButtonText.Text = L.T("Làm mới"); RefreshButton.IsEnabled = true; }
    }
    private async void AccountsClick(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        new AccountsWindow(service) { Owner = this }.ShowDialog();
        var saved = service.LoadProfiles();
        var validIds = saved.Select(p => p.Id).ToHashSet();
        var changedIds = Rows.Where(row => { var next = saved.FirstOrDefault(p => p.Id == row.Profile.Id); return next is null || !row.Profile.Sources.Select(s => s.Remote).SequenceEqual(next.Sources.Select(s => s.Remote)); }).Select(row => row.Profile.Id).ToHashSet();
        wantedConnections.RemoveWhere(id => !validIds.Contains(id) || changedIds.Contains(id));
        foreach (var id in retryAfter.Keys.Where(id => !validIds.Contains(id)).ToArray()) retryAfter.Remove(id);
        Rows.Clear();
        foreach (var profile in saved) { var row = new DriveRow(profile, () => Rows.Select(r => r.Profile)); row.Update(new(profile.Id, service.IsConnected(profile.Id) ? "Connected" : "Disconnected")); Rows.Add(row); }
        RefreshSummary();
        await RefreshAccountLabelsAsync();
    }
    private void GoogleClick(object sender, RoutedEventArgs e) => ShowSetup("Google Drive");
    private void OneDriveClick(object sender, RoutedEventArgs e) => ShowSetup("OneDrive");
    private bool ShowSetup(string provider)
    {
        var dialog = new SetupWindow(service, provider) { Owner = this };
        var linked = dialog.ShowDialog() == true;
        if (linked) _ = RefreshAccountLabelsAsync();
        return linked;
    }
    private void HelpClick(object sender, RoutedEventArgs e) => new HelpWindow { Owner = this }.ShowDialog();
    private void AboutClick(object sender, RoutedEventArgs e) => new AboutWindow { Owner = this }.ShowDialog();
    private void LogsClick(object sender, RoutedEventArgs e) { try { service.OpenLogs(); } catch (Exception ex) { StatusText.Text = ex.Message; } }
    private void WinFspClick(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("https://winfsp.dev/rel/") { UseShellExecute = true });
    private void Restore() { Dispatcher.Invoke(() => { Show(); WindowState = WindowState.Normal; Activate(); }); }
    private void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (exiting) return;
        e.Cancel = true;
        if (settings.KeepMountedOnClose) { Hide(); return; }
        Dispatcher.BeginInvoke(new Action(async () => await QuitAsync()));
    }
    private async void QuitClick(object sender, RoutedEventArgs e) => await QuitAsync();
    private async Task QuitAsync()
    {
        if (busy) { closeAfterOperation = true; Restore(); return; }
        reconnectTimer.Stop();
        Restore();
        await RunAsync(async () =>
        {
            wantedConnections.Clear();
            // Also recover/verify surviving leases before exiting; never abandon an unverified worker.
            await service.PrepareUninstallAsync();
            FinishExit();
        }, L.T("Đã đóng."));
        if (!exiting) reconnectTimer.Start(); // A blocked exit leaves the app available for upload recovery.
    }
    private async Task<string?> PrepareForUninstallAsync()
    {
        if (busy || OwnedWindows.Count > 0) return L.T("Đóng cửa sổ cấu hình và chờ thao tác hiện tại hoàn tất trước khi gỡ.");
        busy = true; IsEnabled = false;
        StatusText.Text = L.T("Đang ngắt ổ an toàn trước khi gỡ ứng dụng…");
        try { await service.PrepareUninstallAsync(); return null; }
        catch (Exception ex) { StatusText.Text = ex.Message; return ex.Message; }
        finally { busy = false; IsEnabled = true; RefreshSummary(); }
    }
    private void FinishExit()
    {
        exiting = true; tray.Visible = false; tray.Dispose(); service.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
    private void ApplyDarkTitle() { int enabled = Theme.IsLight ? 0 : 1; try { DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 20, ref enabled, sizeof(int)); } catch { } }
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}

public sealed class DriveRow : INotifyPropertyChanged
{
    public DriveProfile Profile { get; }
    public string Name => Profile.Name;
    public string Letter => Profile.Letter;
    public string KindLabel => Profile.IsPool ? L.T($"Ổ gộp · {Profile.Sources.Count} nguồn") : L.T("Ổ cloud");
    public bool IsConnected => status == "Connected";
    private IReadOnlyList<RemoteAccount> accounts = Array.Empty<RemoteAccount>();
    private string Describe(string remote)
    {
        var colon = remote.IndexOf(':');
        var account = accounts.FirstOrDefault(a => a.Name == remote[..colon]);
        var folder = remote[(colon + 1)..];
        return (account?.DisplayName ?? L.T("Tài khoản cloud")) + (folder == "" ? "" : " / " + folder);
    }
    public string AccountEmails => string.Join(" · ", (Profile.IsPool ? Profile.Sources.Select(s => s.Remote) : new[] { Profile.Remote })
        .Select(remote => accounts.FirstOrDefault(a => a.Name == remote.Split(':')[0])?.Email ?? L.T("Chưa lấy được email"))
        .Distinct(StringComparer.OrdinalIgnoreCase));
    public string Remote => Profile.IsPool ? L.T($"Ổ gộp · {Profile.Sources.Count} tài khoản: ") + string.Join(" → ", Profile.Sources.Select(s => Describe(s.Remote))) : Describe(Profile.Remote);
    public void SetAccounts(IReadOnlyList<RemoteAccount> value) { accounts = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); }
    private string RemoteType(string remote) => accounts.FirstOrDefault(a => a.Name == remote.Split(':')[0])?.Type ?? "";
    public bool MatchesProvider(string provider) => provider == "pool" ? Profile.IsPool : Profile.IsPool
        ? Profile.Sources.Any(s => RemoteType(s.Remote) == provider) : RemoteType(Profile.Remote) == provider;
    public string ProviderLabel => Profile.IsPool ? L.T($"Ổ gộp · {Profile.Sources.Count}") : RemoteType(Profile.Remote) switch
    { "drive" => "Google Drive", "onedrive" => "OneDrive", "dropbox" => "Dropbox", "s3" => "Amazon S3", "local" => "Local", "webdav" => "WebDAV", "sftp" => "SFTP", _ => "Cloud" };
    public string PermissionLabel => Profile.ReadOnly ? "R only" : "R+W";
    public string AvatarColor => Letter[0] switch { 'S' => "#3B82F6", 'T' => "#14B8A6", 'D' => "#F43F5E", 'E' => "#F87171", 'F' => "#6366F1", 'P' => "#F59E0B", _ => "#00E5B4" };
    public DriveCapacity? Capacity { get; private set; }
    public string CapacityLabel => Capacity is { } c ? $"{FormatBytes(c.UsedBytes)} / {FormatBytes(c.TotalBytes)}" : L.T("Chưa có dữ liệu");
    public string CapacityPercentLabel => Capacity is null ? "—" : CapacityPercent.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + L.T("% đã sử dụng");
    public double CapacityPercent => Capacity is { TotalBytes: > 0 } c ? 100.0 * c.UsedBytes / c.TotalBytes : 0;
    public string CapacityColor => Theme.Hex(CapacityPercent > 90 ? "#F87171" : CapacityPercent >= 70 ? "#F59E0B" : "#00E5B4", true);
    public string CheckedLabel => Capacity is { } c ? RelativeTime(c.CheckedAt) : L.T("Chưa kiểm tra");
    public static string RelativeTime(DateTime value)
    {
        var age = DateTime.Now - value;
        return age.TotalMinutes < 1 ? L.T("Vừa xong") : age.TotalHours < 1 ? L.T($"{(int)age.TotalMinutes} phút trước") : age.TotalDays < 1 ? L.T($"{(int)age.TotalHours} giờ trước") : L.T($"{(int)age.TotalDays} ngày trước");
    }
    public string ProviderColor => ProviderLabel switch { "Google Drive" => "#4285F4", "OneDrive" => "#0078D4", "Dropbox" => "#0061FF", _ => "#00E5B4" };
    public string ProviderTint => "#24" + ProviderColor[1..];
    public string ProviderGlyph => ProviderLabel switch { "Google Drive" => "G", "OneDrive" => "O", "Dropbox" => "D", _ => "P" };
    public static string FormatBytes(long bytes) => bytes >= 1099511627776 ? $"{bytes / 1099511627776.0:0.#} TiB" : bytes >= 1073741824 ? $"{bytes / 1073741824.0:0.#} GiB" : bytes >= 1048576 ? $"{bytes / 1048576.0:0.#} MiB" : $"{bytes / 1024.0:0.#} KiB";
    public void SetCapacity(DriveCapacity? value) { Capacity = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); }
    public string ModeLabel => (Profile.ReadOnly ? L.T("Chỉ đọc") : L.T("Đọc & ghi")) + (Profile.AutoConnect ? L.T("  ·  Tự kết nối") : "");
    private string status = "Disconnected";
    public string? Error { get; private set; }
    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    public string WarningLabel => L.T($"⚠  {Name} · {Error}");
    public void RefreshLanguage() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    public void RefreshTime() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CheckedLabel)));
    public string StatusLabel => status switch { "Connected" => L.T("●  Đã kết nối"), "Connecting" => L.T("◌  Đang kết nối…"), "Disconnecting" => L.T("◌  Đang ngắt…"), "Error" => L.T("●  Cần kiểm tra"), _ => L.T("○  Chưa kết nối") };
    public string StatusColor => Theme.Hex(status == "Connected" ? "#00E5B4" : status == "Error" ? "#F87171" : "#7AADA0", true);
    public string ShortStatus => status switch { "Connected" => "Online", "Connecting" => L.T("Đang nối…"), "Disconnecting" => L.T("Đang ngắt…"), "Error" => L.T("Cần kiểm tra"), _ => L.T("Chưa kết nối") };
    public string PrimaryActionLabel => IsConnected ? L.T("Mở") : status == "Error" ? L.T("Thử lại") : L.T("Kết nối");
    public string ToggleLabel => status == "Connected" ? L.T("Ngắt kết nối") : L.T("Kết nối");
    private readonly Func<IEnumerable<DriveProfile>> profiles;
    public DriveRow(DriveProfile profile, Func<IEnumerable<DriveProfile>> profiles) { Profile = profile; this.profiles = profiles; }
    public string SecondaryText => Profile.IsPool ? string.Join(" · ", DrivePresentation.ChildNames(Profile, profiles(), accounts)) : AccountEmails;
    public string? RowTooltip => Profile.IsPool ? string.Join(Environment.NewLine, DrivePresentation.ChildNames(Profile, profiles(), accounts)) : null;
    public string SecondaryTooltip => RowTooltip ?? AccountEmails;
    public double SecondarySize => Profile.IsPool ? 11 : 12;
    public string SecondaryColor => Theme.Hex(Profile.IsPool ? "#91B2A7" : "#A5C4BA",true);
    public event PropertyChangedEventHandler? PropertyChanged;
    public void Update(DriveSnapshot snapshot) { status = snapshot.Status; Error = snapshot.Error; if (!IsConnected) Capacity = null; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); }
}
