using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EasyMount;

public sealed class AboutWindow : Window
{
    public AboutWindow()
    {
        Style = (Style)FindResource(typeof(Window));
        Title = "Giới thiệu · EasyMount"; Width = 535; Height = 350;
        ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(28) };
        Content = panel;
        panel.Children.Add(new TextBlock { Text = "EasyMount 0.3.0", FontSize = 26, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "Ứng dụng quản lý ổ đĩa cloud cho Windows.", Foreground = Brushes.LightSlateGray, Margin = new Thickness(0, 9, 0, 23) });
        panel.Children.Add(new TextBlock { Text = "WinFsp - Windows File System Proxy,\nCopyright (C) Bill Zissimopoulos", Margin = new Thickness(0, 0, 0, 10) });
        var source = new Button { Content = "github.com/winfsp/winfsp ↗", HorizontalAlignment = HorizontalAlignment.Left };
        source.Click += (_, _) => Process.Start(new ProcessStartInfo("https://github.com/winfsp/winfsp") { UseShellExecute = true });
        panel.Children.Add(source);
        panel.Children.Add(new TextBlock { Text = "Rclone: MIT. Giấy phép và mã nguồn WinFsp đi kèm bản cài đặt trong thư mục third-party.", Foreground = Brushes.LightSlateGray, FontSize = 12, Margin = new Thickness(0, 19, 0, 0) });
    }
}
