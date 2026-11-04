using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EasyMount;

public sealed class AboutWindow : Window
{
    public AboutWindow()
    {
        Style = (Style)FindResource(typeof(Window));
        Title = L.T("Giới thiệu · EasyMount"); Width = 400; Height = 320;
        WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize; ShowInTaskbar = false; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var border = new Border { Background = (Brush)FindResource("Surface"), BorderBrush = Theme.Brush("#1E3D35"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(16) };
        var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; border.Child = panel; Content = border;
        panel.Children.Add(new Border { Width = 48, Height = 48, CornerRadius = new CornerRadius(12), Background = (Brush)FindResource("Accent"), Child = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/EasyMount;component/Assets/easymount.png")), Width = 48, Height = 48 } });
        void Text(string value, double size, Brush? color = null, FontWeight? weight = null, double top = 0) => panel.Children.Add(new TextBlock { Text = value, FontSize = size, Foreground = color ?? Foreground, FontWeight = weight ?? FontWeights.Normal, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,top,0,0) });
        void Divider() => panel.Children.Add(new Border { Height = 1, Background = border.BorderBrush, Margin = new Thickness(0,8,0,8) });
        Text("EasyMount",18,weight:FontWeights.SemiBold,top:4);
        Text("v" + Assembly.GetExecutingAssembly().GetName().Version?.ToString(3),10,(Brush)FindResource("Muted"));
        Text("YOUR CLOUD. YOUR DRIVE.",11,(Brush)FindResource("Muted"),top:3);
        Divider();
        Text(L.T("© 2026 TravisTRTQT"),13,weight:FontWeights.SemiBold);
        Text("All rights reserved.",10,(Brush)FindResource("Muted"),top:2);
        Text(L.T("Phần mềm quản lý ổ đĩa đám mây dành cho\ndesigner và người dùng chuyên nghiệp."),11,(Brush)FindResource("Muted"),top:7);
        Divider();
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        var update = new Button { Content = L.T("Kiểm tra cập nhật"), FontSize = 11, MinHeight = 30, Padding = new Thickness(10,4,10,4), Margin = new Thickness(0,0,8,0) };
        update.Click += (_,_) => new UpdatesWindow { Owner = this }.ShowDialog();
        var close = new Button { Content = L.T("Đóng"), FontSize = 11, MinHeight = 30, Padding = new Thickness(14,4,14,4), IsCancel = true };
        close.Click += (_,_) => Close(); actions.Children.Add(update); actions.Children.Add(close); panel.Children.Add(actions);
    }
}
