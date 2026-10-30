using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace EasyMount;

public sealed class SetupWindow : Window
{
    private readonly DriveService service;
    private AccountSetupSession? session;
    private ConfigStep? step;
    private readonly ComboBox providers = new();
    private readonly TextBox name = new();
    private readonly CheckBox write = new() { Content = "Cho phép tải lên và chỉnh sửa file", IsChecked = true };
    private readonly StackPanel content = new();
    private readonly TextBlock status = new() { Foreground = Brushes.LightSlateGray, Margin = new Thickness(0, 12, 0, 0) };
    private readonly Button next = new() { Content = "Đăng nhập trên trình duyệt", IsEnabled = false };
    private readonly Button cancel = new() { Content = "Hủy", Margin = new Thickness(0, 0, 9, 0) };
    private readonly Button browser = new() { Content = "Mở lại trang đăng nhập ↗", Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 14, 0, 0) };
    private readonly TextBox answer = new();
    private readonly PasswordBox password = new() { Padding = new Thickness(11, 10, 11, 10), Background = new SolidColorBrush(Color.FromRgb(17, 25, 35)), Foreground = Brushes.White, BorderBrush = Brushes.SlateGray };
    private readonly ComboBox choices = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(600) };
    private Uri? browserUri;
    private bool busy, closing, closed, linked, polling;
    private readonly string providerHint;
    private string remoteName = "";

    public SetupWindow(DriveService service, string provider)
    {
        this.service = service; providerHint = provider;
        Style = (Style)FindResource(typeof(Window));
        Title = "Thêm tài khoản cloud · EasyMount";
        Width = 700; Height = Math.Min(790, SystemParameters.WorkArea.Height - 40); MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var layout = new DockPanel { Margin = new Thickness(30, 26, 30, 24) };
        Content = layout;
        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
        var heading = new StackPanel { Orientation = Orientation.Horizontal };
        heading.Children.Add(new System.Windows.Controls.Image { Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/easymount.png")), Width = 42, Height = 42, Margin = new Thickness(0, 0, 14, 0) });
        heading.Children.Add(new TextBlock { Text = "Thêm tài khoản cloud", FontSize = 26, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        header.Children.Add(heading);
        header.Children.Add(new TextBlock { Text = "Chọn dịch vụ  →  Đăng nhập  →  Sẵn sàng sử dụng", Foreground = Brushes.LightSlateGray, Margin = new Thickness(0, 13, 0, 0) });
        DockPanel.SetDock(header, Dock.Top); layout.Children.Add(header);
        var footer = new StackPanel();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
        next.Style = (Style)FindResource("Primary");
        buttons.Children.Add(cancel); buttons.Children.Add(next);
        footer.Children.Add(status); footer.Children.Add(browser); footer.Children.Add(buttons);
        DockPanel.SetDock(footer, Dock.Bottom); layout.Children.Add(footer);
        layout.Children.Add(new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        status.Text = "Đang chuẩn bị danh sách dịch vụ…";
        next.Click += async (_, _) => await NextAsync();
        cancel.Click += (_, _) => Close();
        browser.Click += (_, _) => { if (browserUri is not null) Process.Start(new ProcessStartInfo(browserUri.AbsoluteUri) { UseShellExecute = true }); };
        timer.Tick += async (_, _) =>
        {
            if (!busy || session is null || polling || closed) return;
            polling = true;
            try
            {
                browserUri = await session.BrowserUrlAsync();
                browser.Visibility = browserUri is null ? Visibility.Collapsed : Visibility.Visible;
                if (browserUri is not null) status.Text = "Đang chờ bạn đăng nhập trên trình duyệt. Hoàn tất rồi quay lại đây.";
            }
            catch { /* The main request reports connection errors. */ }
            finally { polling = false; }
        };
        Loaded += async (_, _) => await LoadAsync();
        Closing += async (_, e) =>
        {
            if (closed) return;
            e.Cancel = true;
            if (closing) return;
            closing = true; timer.Stop(); next.IsEnabled = false; cancel.IsEnabled = false;
            status.Text = linked ? "Đang hoàn tất…" : "Đang hủy bước kết nối…";
            if (session is not null) await session.DisposeAsync();
            closed = true; DialogResult = linked;
        };
    }

    private async Task LoadAsync()
    {
        try
        {
            session = new AccountSetupSession(service);
            var all = await session.ProvidersAsync();
            if (closing) return;
            var popular = new[] { "drive", "onedrive", "dropbox", "s3", "webdav", "sftp" };
            foreach (var p in all.OrderBy(p => Array.IndexOf(popular, p.Name) is var rank && rank >= 0 ? rank : 100).ThenBy(p => p.Description)) providers.Items.Add(p);
            Field("Dịch vụ lưu trữ", providers);
            Field("Tên tài khoản trong EasyMount", name);
            content.Children.Add(new TextBlock { Text = "Để trống để lấy email sau khi đăng nhập. Bạn có thể tự đặt hoặc sửa tên.", Foreground = Brushes.LightSlateGray, FontSize = 12, Margin = new Thickness(0, 7, 0, 17) });
            content.Children.Add(write);
            content.Children.Add(new TextBlock { Text = "Mật khẩu Google/Microsoft được nhập trên trang của nhà cung cấp. EasyMount tự nhận kết quả; bạn không cần nhập lệnh hay sao chép token.", Foreground = Brushes.LightSlateGray, FontSize = 13, Margin = new Thickness(0, 14, 0, 0) });
            providers.SelectionChanged += (_, _) =>
            {
                if (providers.SelectedItem is not CloudProvider p) return;
                // Keep an optional custom label when changing providers.
                write.Visibility = p.Name == "drive" ? Visibility.Visible : Visibility.Collapsed;
                next.Content = IsBrowserProvider(p.Name) ? "Đăng nhập trên trình duyệt ↗" : "Tiếp tục";
            };
            var selected = providerHint == "OneDrive" ? "onedrive" : providerHint == "Google Drive" ? "drive" : "drive";
            providers.SelectedItem = providers.Items.Cast<CloudProvider>().First(p => p.Name == selected);
            status.Text = "Chọn dịch vụ; tên mặc định sẽ lấy theo email sau đăng nhập.";
            next.IsEnabled = true;
        }
        catch (Exception ex) { status.Text = ex.Message; }
    }

    private static bool IsBrowserProvider(string type) => type is "drive" or "onedrive" or "dropbox" or "box" or "pcloud";
    private void Field(string label, Control control)
    {
        content.Children.Add(new TextBlock { Text = label, Foreground = Brushes.LightSlateGray, FontSize = 12, Margin = new Thickness(0, 12, 0, 7) });
        System.Windows.Automation.AutomationProperties.SetName(control, label);
        content.Children.Add(control);
    }

    private async Task NextAsync()
    {
        if (busy || session is null || closing) return;
        if (linked) { try { service.SaveAccountLabel(remoteName, name.Text); Close(); } catch (Exception ex) { status.Text = ex.Message; } return; }
        try
        {
            busy = true; next.IsEnabled = false; content.IsEnabled = false; timer.Start();
            status.Text = "Đang chuẩn bị kết nối. Nếu có yêu cầu đăng nhập, trình duyệt sẽ mở…";
            if (step is null)
            {
                if (providers.SelectedItem is not CloudProvider p) throw new ArgumentException("Chọn dịch vụ lưu trữ.");
                var parameters = new Dictionary<string, string> { ["config_is_local"] = "true" };
                try { foreach (var pair in AccountSetupSession.OAuthParameters(Path.Combine(AppContext.BaseDirectory, "oauth-clients.json"), p.Name)) parameters[pair.Key] = pair.Value; }
                catch { throw new InvalidOperationException("Không đọc được thiết lập đăng nhập của bản EasyMount này. Hãy kiểm tra tệp oauth-clients.json."); }
                if (p.Name == "drive") parameters["scope"] = write.IsChecked == true ? "drive" : "drive.readonly";
                remoteName = "account_" + Guid.NewGuid().ToString("N");
                step = await session.BeginAsync(remoteName, p.Name, parameters, all: !IsBrowserProvider(p.Name));
            }
            else if (step.State != "")
            {
                var q = step.Option ?? throw new InvalidOperationException("Không có bước cấu hình để tiếp tục.");
                var value = IsSensitive(q) ? password.Password : q.Exclusive ? (choices.SelectedItem as ConfigChoice)?.Value ?? "" : answer.Text;
                if (string.IsNullOrEmpty(value) && !IsSensitive(q)) value = q.DefaultStr;
                if (q.Required && string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Vui lòng nhập hoặc chọn giá trị để tiếp tục.");
                step = await session.ContinueAsync(step, value);
                password.Clear(); answer.Clear();
            }
            while (!closing && step.State != "" && (step.Option?.Name is "config_is_local" or "config_advanced" or "config_fs_advanced"))
                step = await session.ContinueAsync(step, step.Option.Name == "config_is_local" ? "true" : "false");
            if (closing) return;
            if (step.State == "")
            {
                status.Text = "Đang kiểm tra quyền truy cập tài khoản…";
                await session.CompleteAsync();
                if (closing) return;
                if (string.IsNullOrWhiteSpace(name.Text)) name.Text = await session.AccountEmailAsync() ?? providers.Text;
                if (closing) return;
                service.SaveAccountLabel(remoteName, name.Text);
                linked = true; content.Children.Clear();
                content.Children.Add(new TextBlock { Text = "✓  Tài khoản đã sẵn sàng", Foreground = (Brush)FindResource("Accent"), FontSize = 25, Margin = new Thickness(0, 25, 0, 14) });
                content.Children.Add(new TextBlock { Text = "Bạn có thể gắn tài khoản thành ổ riêng hoặc thêm vào ổ gộp.", FontSize = 15 });
                Field("Tên tài khoản (có thể chỉnh sửa)", name);
                status.Text = "Đã kiểm tra truy cập. Bạn có thể sửa tên trước khi hoàn tất.";
                next.Content = "Hoàn tất"; cancel.Visibility = Visibility.Collapsed;
            }
            else ShowQuestion();
        }
        catch (Exception ex) { if (!closing) { status.Text = ex.Message; if (step?.State == "") next.Content = "Thử lại kết nối"; } }
        finally { busy = false; timer.Stop(); if (!closing) { next.IsEnabled = true; content.IsEnabled = true; browser.Visibility = Visibility.Collapsed; } }
    }

    private void ShowQuestion()
    {
        var q = step?.Option ?? throw new InvalidOperationException("Dịch vụ chưa cung cấp bước tiếp theo. Hủy và thử lại.");
        content.Children.Clear();
        var label = q.Name switch { "url" => "Địa chỉ máy chủ", "vendor" => "Loại dịch vụ WebDAV", "user" => "Tên đăng nhập", "pass" => "Mật khẩu", "client_id" => "OAuth Client ID", "client_secret" => "OAuth Client Secret", "config_shared_client_id" => "Thiết lập kết nối Google Drive", "config_type" => "Loại tài khoản OneDrive", "config_driveid" => "Chọn ổ OneDrive", "config_change_team_drive" => "Sử dụng Shared Drive?", "config_team_drive" => "Chọn Shared Drive", _ => q.Name.Replace('_', ' ') };
        content.Children.Add(new TextBlock { Text = label, FontSize = 23, FontWeight = FontWeights.SemiBold });
        var help = q.Name == "config_shared_client_id" ? "Kết nối Google Drive dùng chung của rclone đang ngừng hoạt động trong năm 2026. Nên chọn Không để nhập OAuth client riêng. Bản EasyMount dành cho cộng đồng có thể cấu hình sẵn thông tin này để người dùng chỉ cần đăng nhập.\n\nBạn vẫn muốn thử kết nối dùng chung cũ?" : q.Help;
        content.Children.Add(new TextBlock { Text = help, Foreground = Brushes.LightSlateGray, Margin = new Thickness(0, 12, 0, 15) });
        if (q.Name is "client_id" or "client_secret" or "config_shared_client_id")
        {
            var guide = new Button { Content = "Hướng dẫn thiết lập Google Drive ↗", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) };
            guide.Click += (_, _) => Process.Start(new ProcessStartInfo("https://rclone.org/drive/#making-your-own-client-id") { UseShellExecute = true });
            content.Children.Add(guide);
        }
        choices.Items.Clear(); answer.Text = IsSensitive(q) ? "" : q.DefaultStr; password.Clear();
        if (q.Examples is not null && q.Examples.Count > 0)
        {
            foreach (var option in q.Examples) choices.Items.Add(option);
            choices.SelectedItem = q.Examples.FirstOrDefault(e => e.Value == q.DefaultStr);
            if (choices.SelectedIndex < 0 && q.Exclusive) choices.SelectedIndex = 0;
            Field(q.Exclusive ? "Chọn một mục" : "Giá trị gợi ý (tùy chọn)", choices);
        }
        if (IsSensitive(q)) Field("Nhập thông tin bảo mật", password);
        else if (!q.Exclusive) Field(q.Required ? "Giá trị (bắt buộc)" : "Giá trị (có thể để mặc định)", answer);
        choices.SelectionChanged -= ApplyExample;
        choices.SelectionChanged += ApplyExample;
        next.Content = "Tiếp tục";
        status.Text = string.IsNullOrWhiteSpace(step!.Error) ? "Chọn hoặc điền thông tin, sau đó bấm Tiếp tục." : step.Error;
    }
    private static bool IsSensitive(ConfigQuestion q) => q.IsPassword || q.Name.Contains("secret", StringComparison.OrdinalIgnoreCase) || q.Name.Contains("token", StringComparison.OrdinalIgnoreCase);
    private void ApplyExample(object sender, SelectionChangedEventArgs e) { if (choices.SelectedItem is ConfigChoice option) answer.Text = option.Value; }
}
