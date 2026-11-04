using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EasyMount;

public sealed class HelpWindow : Window
{
    public HelpWindow()
    {
        Style = (Style)FindResource(typeof(Window));
        Title = L.T("Hướng dẫn · EasyMount"); Width = 720; Height = 680;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(28) };
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        panel.Children.Add(new TextBlock { Text = L.T("Sử dụng EasyMount"), FontSize = 26, FontWeight = FontWeights.SemiBold });
        foreach (var (title, text) in new[] {
            (L.T("1. Thêm tài khoản cloud"), L.T("Bấm Tài khoản cloud, chọn dịch vụ rồi đăng nhập trên trình duyệt. Thêm lần lượt từng tài khoản. Tên mặc định lấy theo email và có thể đổi. Với Gmail cá nhân, chọn Không ở bước Shared Drive.")),
            (L.T("2. Tạo ổ riêng hoặc ổ gộp"), L.T("Bấm Thêm ổ đĩa, đặt tên và ký tự. Với ổ gộp, thêm ít nhất 2 tài khoản; dùng Lên / Xuống để xếp ưu tiên. Thứ tự trên trước, dưới sau. File mới được đặt nguyên vẹn vào nguồn đầu tiên còn đủ chỗ; file cũ giữ ở nguồn hiện tại.")),
            (L.T("3. Dung lượng và file trùng tên"), L.T("Để giới hạn bằng 0 để dùng dung lượng cloud báo về. Nếu dịch vụ không báo dung lượng, nhập giới hạn GiB cho nguồn. Ổ gộp hiển thị chung các file; nếu các nguồn có file cùng đường dẫn, bạn có thể chỉ thấy một mục. Nên dùng thư mục riêng để tránh trùng tên.")),
            (L.T("4. Tải lên và tải về"), L.T("Mặc định đọc và ghi. Copy từ máy vào ổ EasyMount để tải lên; copy từ ổ về máy để tải về. Đợi tải lên hoàn tất trước khi ngắt ổ. Đây là ổ cloud trong Explorer, chưa phải đồng bộ tự động một thư mục local.")),
            (L.T("5. Ngắt ổ và cập nhật"), L.T("Đóng file đang mở rồi bấm Ngắt kết nối. Nếu còn upload, ứng dụng giữ ổ để tránh mất file. Thoát hoặc gỡ app cũng kiểm tra upload. Khi sửa nguồn ổ gộp, cần ngắt kết nối và xử lý cache còn chờ phục hồi trước.")),
            (L.T("6. Lỗi đăng nhập"), L.T("Google 403 access_denied: bản OAuth Testing chỉ cho phép email đã được thêm tester. OneDrive: chọn đúng loại tài khoản, ổ lưu trữ và xác nhận ổ. Nếu nhà cung cấp báo lỗi, đọc thông báo ở cuối cửa sổ và chọn lại; không nhập token hoặc mật khẩu vào nhật ký.")) })
        {
            panel.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 22, 0, 8) });
            panel.Children.Add(new TextBlock { Text = text, Foreground = (Brush)System.Windows.Application.Current.FindResource("Muted"), TextWrapping = TextWrapping.Wrap });
        }
    }
}
