using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EasyMount;

public sealed class AccountsWindow : Window
{
    private readonly DriveService service;
    private readonly StackPanel list = new();
    private readonly TextBlock status = new();
    private readonly Button add = new() { Content = L.T("+ Thêm tài khoản") };

    public AccountsWindow(DriveService service)
    {
        this.service = service;
        Style = (Style)FindResource(typeof(Window));
        Title = L.T("Tài khoản cloud · EasyMount");
        Width = 720; Height = 580; MinWidth = 530; MinHeight = 350;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var layout = new DockPanel { Margin = new Thickness(28) }; Content = layout;
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 18) };
        add.Style = (Style)FindResource("Primary");
        DockPanel.SetDock(add, Dock.Right); header.Children.Add(add);
        var heading = new StackPanel();
        heading.Children.Add(new TextBlock { Text = L.T("Tài khoản cloud"), FontSize = 24, FontWeight = FontWeights.SemiBold });
        heading.Children.Add(new TextBlock { Text = L.T("Quản lý tài khoản đã liên kết với EasyMount."), Foreground = (Brush)FindResource("Muted"), Margin = new Thickness(0, 6, 10, 0), TextWrapping = TextWrapping.Wrap });
        header.Children.Add(heading); DockPanel.SetDock(header, Dock.Top); layout.Children.Add(header);
        status.Foreground = (Brush)FindResource("Muted"); status.TextWrapping = TextWrapping.Wrap; status.Margin = new Thickness(0, 12, 0, 0);
        DockPanel.SetDock(status, Dock.Bottom); layout.Children.Add(status);
        var scroll = new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        scroll.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = FindResource("AccountScrollBar");
        layout.Children.Add(scroll);
        add.Click += async (_, _) => { var setup = new SetupWindow(service, "Google Drive / OneDrive") { Owner = this }; setup.ShowDialog(); await RefreshAsync(); };
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        list.Children.Clear(); status.Text = L.T("Đang tải tài khoản…"); add.IsEnabled = false;
        try
        {
            var accounts = await service.ListRemotesAsync();
            if (accounts.Count == 0) list.Children.Add(new TextBlock { Text = L.T("Chưa có tài khoản. Chọn Thêm tài khoản để bắt đầu."), Margin = new Thickness(0, 20, 0, 0), TextWrapping = TextWrapping.Wrap });
            foreach (var account in accounts)
            {
                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 1), LastChildFill = true };
                var remove = new Button { Content = L.T("Xóa"), Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
                System.Windows.Automation.AutomationProperties.SetName(remove, L.T("Xóa tài khoản") + " " + account.DisplayName);
                DockPanel.SetDock(remove, Dock.Right); row.Children.Add(remove);
                var info = new StackPanel { Margin = new Thickness(12, 12, 0, 12) };
                info.Children.Add(new TextBlock { Text = account.DisplayName, FontSize = 14, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
                var provider = account.Type switch { "drive" => "Google Drive", "onedrive" => "OneDrive", "dropbox" => "Dropbox", _ => account.Type };
                info.Children.Add(new TextBlock { Text = provider + "  ·  " + (account.Email ?? L.T("Chưa lấy được email")), Foreground = (Brush)FindResource("Muted"), Margin = new Thickness(0, 5, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis });
                row.Children.Add(info);
                list.Children.Add(new Border { Child = row, BorderBrush = Theme.Brush("#1E3D35"), BorderThickness = new Thickness(0, 0, 0, 1) });
                remove.Click += async (_, _) => {
                    var affected = service.LoadProfiles().Where(p => DriveService.UsesAccount(p, account.Name)).ToArray();
                    var details = string.Join("\n", affected.Select(p => "• " + p.Name + (p.IsPool ? (p.Sources.Count > 2 ? L.T(" (gỡ nguồn khỏi ổ gộp)") : L.T(" (xóa ổ gộp còn dưới 2 nguồn)")) : L.T(" (xóa ổ đĩa)"))));
                    var message = L.T("Xóa tài khoản này? EasyMount sẽ ngắt kết nối an toàn và cập nhật các ổ liên quan. File trên cloud được giữ nguyên.") + "\n\n" + account.DisplayName + (details.Length > 0 ? "\n\n" + details : "");
                    if (MessageBox.Show(this, message, "EasyMount", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                    list.IsEnabled = false; add.IsEnabled = false; status.Text = L.T("Đang ngắt kết nối và xóa tài khoản…");
                    try { await service.RemoveAccountAsync(account.Name); await RefreshAsync(); }
                    catch (Exception ex) { MessageBox.Show(this, ex.Message, "EasyMount", MessageBoxButton.OK, MessageBoxImage.Information); }
                    finally { list.IsEnabled = true; add.IsEnabled = true; }
                };
            }
            status.Text = accounts.Count + " " + L.T("tài khoản đã liên kết");
        }
        catch (Exception ex) { status.Text = ex.Message; }
        finally { add.IsEnabled = true; }
    }
}
