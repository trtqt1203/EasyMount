using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EasyMount;

public sealed class UpdatesWindow : Window
{
    private readonly TextBlock status = new() { Text = L.T("Kiểm tra phiên bản EasyMount mới nhất."), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 18, 0, 20) };
    private readonly Button check = new() { Content = L.T("Kiểm tra ngay"), HorizontalAlignment = HorizontalAlignment.Left };
    private readonly Button download = new() { Content = L.T("Mở bản tải chính thức ↗"), Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 12, 0, 0) };
    private Uri? downloadUrl;
    public UpdatesWindow()
    {
        Style = (Style)FindResource(typeof(Window));
        Title = L.T("Cập nhật · EasyMount"); Width = 560; Height = 390; ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(30) }; Content = panel;
        panel.Children.Add(new TextBlock { Text = L.T("Luôn sẵn sàng cho bản mới."), FontSize = 25, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = L.T("Phiên bản đang dùng: ") + Assembly.GetExecutingAssembly().GetName().Version?.ToString(3), Foreground = (Brush)FindResource("Muted"), Margin = new Thickness(0, 10, 0, 0) });
        panel.Children.Add(status); panel.Children.Add(check); panel.Children.Add(download);
        panel.Children.Add(new TextBlock { Text = L.T("Chỉ đọc thông tin bản phát hành. Việc tải và cài cập nhật do bạn chủ động thực hiện."), Foreground = (Brush)FindResource("Muted"), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 20, 0, 0) });
        check.Click += async (_, _) =>
        {
            check.IsEnabled = false; download.Visibility = Visibility.Collapsed; status.Text = L.T("Đang kiểm tra…");
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
                var info = await new ReleaseClient(http).CheckAsync();
                var current = Assembly.GetExecutingAssembly().GetName().Version!;
                status.Text = !info.IsPublished ? L.T("Chưa có bản phát hành công khai. Bạn đang dùng bản phát triển.")
                    : info.Version > new Version(current.ToString(3)) ? L.T("Có bản mới: ") + info.Version : L.T("Bạn đang dùng phiên bản mới nhất hoặc bản phát triển mới hơn.");
                downloadUrl = info.DownloadUrl; download.Visibility = info.IsPublished ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { status.Text = L.T("Chưa kết nối được dịch vụ cập nhật. Kiểm tra mạng hoặc thử lại sau; các ổ đĩa vẫn hoạt động bình thường."); }
            finally { check.IsEnabled = true; }
        };
        download.Click += (_, _) => { if (downloadUrl is not null) Process.Start(new ProcessStartInfo(downloadUrl.AbsoluteUri) { UseShellExecute = true }); };
    }
}
