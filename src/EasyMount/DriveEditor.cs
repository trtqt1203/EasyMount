using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EasyMount;

public sealed class DriveEditor : Window
{
    public DriveProfile? Result { get; private set; }
    private readonly TextBox name = new(), path = new(), limit = new() { Text = "0" };
    private readonly ComboBox remote = new(), letter = new();
    private readonly CheckBox pool = new() { Content = "Gộp nhiều tài khoản thành một ổ đĩa" };
    private readonly CheckBox readOnly = new() { Content = "Chỉ đọc — không thay đổi file trên cloud", IsChecked = true };
    private readonly CheckBox auto = new() { Content = "Tự kết nối khi mở EasyMount" };
    private readonly ListBox sources = new() { Height = 140, Background = new SolidColorBrush(Color.FromRgb(21, 30, 40)), Foreground = Brushes.White };
    private readonly StackPanel poolPanel = new();
    private readonly TextBlock feedback = new() { Foreground = Brushes.LightSlateGray, Margin = new Thickness(0, 12, 0, 0) };
    private readonly DriveService service;
    private readonly IReadOnlyList<DriveProfile> profiles;
    private readonly DriveProfile? original;
    private bool testing;

    public DriveEditor(DriveService service, IReadOnlyList<RemoteAccount> accounts, IReadOnlyList<DriveProfile> profiles, DriveProfile? original)
    {
        this.service = service; this.profiles = profiles; this.original = original;
        Style = (Style)FindResource(typeof(Window));
        Title = original is null ? "Thêm ổ đĩa · EasyMount" : "Sửa ổ đĩa · EasyMount";
        Width = 670; Height = Math.Min(880, SystemParameters.WorkArea.Height - 40); MinHeight = 580; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(28, 22, 28, 22) };
        var layout = new DockPanel();
        var footer = new StackPanel { Margin = new Thickness(28, 0, 28, 20) };
        DockPanel.SetDock(footer, Dock.Bottom); layout.Children.Add(footer);
        layout.Children.Add(new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Content = layout;
        panel.Children.Add(new TextBlock { Text = original is null ? "Thêm ổ đĩa" : "Cấu hình ổ đĩa", FontSize = 25, FontWeight = FontWeights.SemiBold });
        AddField(panel, "Tên ổ đĩa trong Explorer", name);
        foreach (char c in "RSTUVWXYZDEFGHIJKLMNOPQ") letter.Items.Add(c + ":");
        var used = profiles.Where(p => p.Id != original?.Id).Select(p => p.Letter).ToHashSet(StringComparer.OrdinalIgnoreCase);
        letter.SelectedItem = letter.Items.Cast<string>().FirstOrDefault(l => !used.Contains(l) && !System.IO.DriveInfo.GetDrives().Any(d => d.Name.StartsWith(l, StringComparison.OrdinalIgnoreCase)));
        AddField(panel, "Ký tự ổ đĩa", letter);
        panel.Children.Add(pool);
        foreach (var a in accounts) remote.Items.Add(new ComboBoxItem { Content = $"{a.DisplayName}  ·  {a.Type}", Tag = a.Name });
        remote.SelectedIndex = 0;
        AddField(panel, "Tài khoản cloud", remote);
        AddField(panel, "Thư mục trên cloud (để trống = thư mục gốc)", path);
        AddField(poolPanel, "Giới hạn cho thư mục này (GiB; 0 = tự đọc dung lượng cloud)", limit);
        var add = new Button { Content = "＋ Thêm nguồn vào ổ gộp", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 10, 0, 10) };
        add.Click += (_, _) => { try { sources.Items.Add(ReadSource()); sources.SelectedIndex = sources.Items.Count - 1; } catch (Exception ex) { feedback.Text = ex.Message; } };
        poolPanel.Children.Add(add);
        poolPanel.Children.Add(new TextBlock { Text = "Thứ tự ưu tiên ghi: trên trước, dưới sau", Margin = new Thickness(0, 0, 0, 6) });
        System.Windows.Automation.AutomationProperties.SetName(sources, "Danh sách nguồn cloud theo thứ tự ưu tiên");
        poolPanel.Children.Add(sources);
        var ordering = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 8) };
        foreach (var (caption, offset) in new[] { ("↑ Lên", -1), ("↓ Xuống", 1) })
        {
            var button = new Button { Content = caption, Margin = new Thickness(0, 0, 8, 0) };
            button.Click += (_, _) => { var i = sources.SelectedIndex; var next = i + offset; if (i < 0 || next < 0 || next >= sources.Items.Count) return; var item = sources.Items[i]; sources.Items.RemoveAt(i); sources.Items.Insert(next, item); sources.SelectedIndex = next; };
            ordering.Children.Add(button);
        }
        var remove = new Button { Content = "Bỏ nguồn" };
        remove.Click += (_, _) => { if (sources.SelectedIndex >= 0) sources.Items.RemoveAt(sources.SelectedIndex); };
        ordering.Children.Add(remove); poolPanel.Children.Add(ordering);
        poolPanel.Children.Add(new TextBlock { Text = "File mới được ghi vào nguồn ưu tiên đầu tiên còn đủ chỗ. File không bị chia nhỏ. Muốn đổi giới hạn hoặc thư mục, bỏ nguồn rồi thêm lại.", Foreground = Brushes.LightSlateGray, FontSize = 12 });
        panel.Children.Add(poolPanel);
        pool.Checked += (_, _) => poolPanel.Visibility = Visibility.Visible;
        pool.Unchecked += (_, _) => poolPanel.Visibility = Visibility.Collapsed;
        poolPanel.Visibility = Visibility.Collapsed;
        panel.Children.Add(readOnly); panel.Children.Add(auto);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var test = new Button { Content = "Kiểm tra", Margin = new Thickness(0, 0, 9, 0) };
        test.Click += async (_, _) => await TestAsync(); buttons.Children.Add(test);
        var save = new Button { Content = "Lưu ổ đĩa", Style = (Style)FindResource("Primary") };
        save.Click += (_, _) => { try { Result = ReadProfile(); DialogResult = true; } catch (Exception ex) { feedback.Text = ex.Message; } };
        buttons.Children.Add(save); footer.Children.Add(buttons); footer.Children.Add(feedback);
        Closing += (_, e) => { if (testing) e.Cancel = true; };
        if (original is not null)
        {
            name.Text = original.Name;
            if (original.IsPool) { pool.IsChecked = true; foreach (var source in original.Sources) sources.Items.Add(new PoolSource { Remote = source.Remote, LimitBytes = source.LimitBytes }); }
            else
            {
                int colon = original.Remote.IndexOf(':');
                string account = colon >= 0 ? original.Remote[..colon] : original.Remote;
                remote.SelectedItem = remote.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == account);
                path.Text = colon >= 0 ? original.Remote[(colon + 1)..] : "";
            }
            letter.SelectedItem = original.Letter; readOnly.IsChecked = original.ReadOnly; auto.IsChecked = original.AutoConnect;
        }
    }
    private PoolSource ReadSource()
    {
        if (remote.SelectedItem is not ComboBoxItem account) throw new InvalidOperationException("Chọn tài khoản cloud.");
        if (!decimal.TryParse(limit.Text.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var gib) || gib < 0 || gib > long.MaxValue / 1073741824m)
            throw new ArgumentException("Giới hạn phải là số GiB không âm.");
        if (gib > 0 && gib * 1073741824m < 1) throw new ArgumentException("Giới hạn cần ít nhất 1 byte.");
        if (sources.Items.Cast<PoolSource>().Any(s => s.Remote[..s.Remote.IndexOf(':')].Equals((string)account.Tag, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Tài khoản này đã có trong ổ gộp.");
        return new PoolSource { Remote = account.Tag + ":" + path.Text.Trim().TrimStart('/'), LimitBytes = (long)(gib * 1073741824m) };
    }
    private static void AddField(StackPanel panel, string label, Control input)
    {
        panel.Children.Add(new TextBlock { Text = label, FontSize = 12, Margin = new Thickness(0, 12, 0, 6), Foreground = Brushes.LightSlateGray });
        System.Windows.Automation.AutomationProperties.SetName(input, label);
        panel.Children.Add(input);
    }
    private DriveProfile ReadProfile()
    {
        if (letter.SelectedItem is not string drive) throw new InvalidOperationException("Chọn ký tự ổ đĩa còn trống.");
        if (pool.IsChecked != true && remote.SelectedItem is not ComboBoxItem) throw new InvalidOperationException("Chọn tài khoản cloud.");
        var result = new DriveProfile { Id = original?.Id ?? Guid.NewGuid().ToString("N"), Name = name.Text.Trim(),
            Remote = pool.IsChecked == true ? "" : ((ComboBoxItem)remote.SelectedItem).Tag + ":" + path.Text.Trim().TrimStart('/'),
            Sources = pool.IsChecked == true ? sources.Items.Cast<PoolSource>().ToList() : new(),
            Letter = drive, ReadOnly = readOnly.IsChecked == true, AutoConnect = auto.IsChecked == true };
        DriveService.ValidateProfile(result);
        if (profiles.Any(p => p.Id != result.Id && p.Letter.Equals(result.Letter, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Ký tự ổ đĩa đã được dùng trong cấu hình khác.");
        return result;
    }
    private async Task TestAsync()
    {
        if (testing) return;
        try { var p = ReadProfile(); testing = true; IsEnabled = false; feedback.Text = "Đang kiểm tra các nguồn cloud…"; await service.TestProfileAsync(p); feedback.Text = "✓ Các tài khoản và thư mục truy cập được."; }
        catch (Exception ex) { feedback.Text = ex.Message; }
        finally { testing = false; IsEnabled = true; }
    }
}

