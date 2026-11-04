using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;

namespace EasyMount;

public sealed class DriveEditor : Window
{
    public DriveProfile? Result { get; private set; }
    private readonly TextBox name = new(), path = new(), limit = new() { Text = "0" };
    private readonly ComboBox remote = new(), letter = new();
    private readonly CheckBox pool = new() { Content = L.T("Gộp nhiều tài khoản thành một ổ đĩa") };
    private readonly CheckBox readOnly = new() { Content = L.T("Chỉ đọc — không thay đổi file trên cloud"), IsChecked = false };
    private readonly CheckBox auto = new() { Content = L.T("Tự kết nối khi mở EasyMount") };
    private readonly ListBox sources = new() { Height = 140, Background = (Brush)System.Windows.Application.Current.FindResource("Surface"), Foreground = Theme.Brush("#E8FBF5",true) };
    private readonly StackPanel poolPanel = new();
    private readonly TextBlock feedback = new() { Foreground = (Brush)System.Windows.Application.Current.FindResource("Muted"), Margin = new Thickness(0, 12, 0, 0) };
    private readonly DriveService service;
    private readonly IReadOnlyList<DriveProfile> profiles;
    private readonly DriveProfile? original;
    private readonly IReadOnlyList<RemoteAccount> accounts;
    private bool testing;

    public DriveEditor(DriveService service, IReadOnlyList<RemoteAccount> accounts, IReadOnlyList<DriveProfile> profiles, DriveProfile? original)
    {
        this.service = service; this.profiles = profiles; this.original = original; this.accounts = accounts;
        Style = (Style)FindResource(typeof(Window));
        Title = original is null ? L.T("Thêm ổ đĩa · EasyMount") : L.T("Sửa ổ đĩa · EasyMount");
        Width = 770; Height = Math.Min(940, SystemParameters.WorkArea.Height - 40); MinHeight = 580; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(28, 22, 28, 22) };
        var layout = new DockPanel();
        var footer = new StackPanel { Margin = new Thickness(28, 0, 28, 20) };
        DockPanel.SetDock(footer, Dock.Bottom); layout.Children.Add(footer);
        layout.Children.Add(new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Content = layout;
        panel.Children.Add(new TextBlock { Text = original is null ? L.T("Thêm ổ đĩa") : L.T("Cấu hình ổ đĩa"), FontSize = 25, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = L.T("Đặt tên, chọn nguồn cloud và kết nối với File Explorer."), Foreground = (Brush)FindResource("Muted"), FontSize = 13, Margin = new Thickness(0, 8, 0, 16) });
        AddField(panel, L.T("Tên ổ đĩa trong Explorer"), name);
        panel.Children.Add(new TextBlock { Text = L.T("Tên do bạn đặt. Để trống để tự đặt Drive1, Drive2…"), Foreground = (Brush)FindResource("Muted"), FontSize = 11, Margin = new Thickness(0, 5, 0, 0) });
        foreach (char c in "RSTUVWXYZDEFGHIJKLMNOPQ") letter.Items.Add(c + ":");
        var used = profiles.Where(p => p.Id != original?.Id).Select(p => p.Letter).ToHashSet(StringComparer.OrdinalIgnoreCase);
        letter.SelectedItem = letter.Items.Cast<string>().FirstOrDefault(l => !used.Contains(l) && !System.IO.DriveInfo.GetDrives().Any(d => d.Name.StartsWith(l, StringComparison.OrdinalIgnoreCase)));
        AddField(panel, L.T("Ký tự ổ đĩa"), letter);
        panel.Children.Add(pool);
        panel.Children.Add(new TextBlock { Text = L.T("Một tài khoản: gắn riêng. Nhiều tài khoản: bật gộp ổ, rồi thêm từng nguồn vào danh sách ưu tiên."), Foreground = (Brush)FindResource("Muted"), FontSize = 12, Margin = new Thickness(0, 0, 0, 10) });
        foreach (var a in accounts) remote.Items.Add(new ComboBoxItem { Content = L.T($"{a.DisplayName}  ·  {new CloudProvider(a.Type, a.Type)}"), Tag = a.Name });
        remote.SelectedIndex = 0;
        AddField(panel, L.T("Tài khoản cloud"), remote);
        AddField(panel, L.T("Thư mục trên cloud (để trống = thư mục gốc)"), path);
        AddField(poolPanel, L.T("Giới hạn nguồn đang chọn (GiB; 0 = tự động theo cloud)"), limit);
        var add = new Button { Content = L.T("＋ Thêm / cập nhật tài khoản đã chọn"), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 10, 0, 10) };
        add.Click += (_, _) =>
        {
            try
            {
                var source = ReadSource();
                var existing = sources.Items.Cast<PoolSource>().FirstOrDefault(s => s.Remote.Split(':')[0].Equals(source.Remote.Split(':')[0], StringComparison.OrdinalIgnoreCase));
                if (existing is null) sources.Items.Add(source);
                else { var index = sources.Items.IndexOf(existing); sources.Items[index] = source; }
                RefreshSources();
                feedback.Text = existing is null ? L.T("Đã thêm tài khoản. Chọn tài khoản khác để thêm tiếp.") : L.T("Đã cập nhật thư mục và giới hạn của tài khoản.");
                remote.SelectedItem = remote.Items.Cast<ComboBoxItem>().FirstOrDefault(item => !sources.Items.Cast<PoolSource>().Any(s => s.Remote.Split(':')[0] == (string)item.Tag)) ?? remote.SelectedItem;
                path.Clear(); limit.Text = "0";
            }
            catch (Exception ex) { feedback.Text = ex.Message; }
        };
        poolPanel.Children.Add(add);
        poolPanel.Children.Add(new TextBlock { Text = L.T("Các tài khoản trong ổ gộp · ưu tiên từ trên xuống"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
        System.Windows.Automation.AutomationProperties.SetName(sources, L.T("Danh sách nguồn cloud theo thứ tự ưu tiên"));
        var sourceText = new FrameworkElementFactory(typeof(TextBlock));
        sourceText.SetBinding(TextBlock.TextProperty, new Binding("."));
        sourceText.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        sourceText.SetValue(TextBlock.MaxWidthProperty, 640.0);
        sourceText.SetValue(TextBlock.MarginProperty, new Thickness(5, 5, 5, 5));
        sources.ItemTemplate = new DataTemplate { VisualTree = sourceText };
        poolPanel.Children.Add(sources);
        sources.SelectionChanged += (_, _) =>
        {
            if (sources.SelectedItem is not PoolSource source) return;
            var colon = source.Remote.IndexOf(':');
            remote.SelectedItem = remote.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == source.Remote[..colon]);
            path.Text = source.Remote[(colon + 1)..];
            limit.Text = (source.LimitBytes / 1073741824m).ToString(CultureInfo.InvariantCulture);
        };
        var ordering = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 8) };
        foreach (var (caption, offset) in new[] { (L.T("↑ Lên"), -1), (L.T("↓ Xuống"), 1) })
        {
            var button = new Button { Content = caption, Margin = new Thickness(0, 0, 8, 0) };
            button.Click += (_, _) => { var i = sources.SelectedIndex; var next = i + offset; if (i < 0 || next < 0 || next >= sources.Items.Count) return; var item = sources.Items[i]; sources.Items.RemoveAt(i); sources.Items.Insert(next, item); sources.SelectedIndex = next; RefreshSources(); };
            ordering.Children.Add(button);
        }
        var remove = new Button { Content = L.T("Bỏ nguồn") };
        remove.Click += (_, _) => { if (sources.SelectedIndex >= 0) sources.Items.RemoveAt(sources.SelectedIndex); RefreshSources(); };
        ordering.Children.Add(remove); poolPanel.Children.Add(ordering);
        poolPanel.Children.Add(new TextBlock { Text = L.T("Cần ít nhất 2 tài khoản. File mới vào nguồn số 1; khi không đủ chỗ sẽ dùng nguồn tiếp theo. File cũ giữ nguyên vị trí. Chọn một dòng để sửa thư mục hoặc giới hạn, rồi bấm Cập nhật. File trùng đường dẫn được hiển thị chung."), Foreground = (Brush)System.Windows.Application.Current.FindResource("Muted"), FontSize = 12, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(poolPanel);
        pool.Checked += (_, _) =>
        {
            poolPanel.Visibility = Visibility.Visible;
            if (original?.IsPool != true && sources.Items.Count == 0 && remote.SelectedItem is ComboBoxItem)
            {
                sources.Items.Add(ReadSource()); RefreshSources();
                remote.SelectedItem = remote.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag != sources.Items.Cast<PoolSource>().First().Remote.Split(':')[0]) ?? remote.SelectedItem;
            }
        };
        pool.Unchecked += (_, _) => poolPanel.Visibility = Visibility.Collapsed;
        poolPanel.Visibility = Visibility.Collapsed;
        panel.Children.Add(readOnly); panel.Children.Add(auto);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var test = new Button { Content = L.T("Kiểm tra"), Margin = new Thickness(0, 0, 9, 0) };
        test.Click += async (_, _) => await TestAsync(); buttons.Children.Add(test);
        var save = new Button { Content = L.T("Lưu ổ đĩa"), Style = (Style)FindResource("Primary") };
        save.Click += async (_, _) =>
        {
            if (testing) return;
            try { var result = ReadProfile(); testing = true; IsEnabled = false; feedback.Text = L.T("Đang kiểm tra các tài khoản và dung lượng…"); await service.TestProfileAsync(result); Result = result; testing = false; DialogResult = true; }
            catch (Exception ex) { feedback.Text = ex.Message; }
            finally { testing = false; IsEnabled = true; }
        };
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
        RefreshSources();
    }
    private void RefreshSources()
    {
        var index = 1;
        foreach (var source in sources.Items.Cast<PoolSource>())
        {
            var colon = source.Remote.IndexOf(':');
            var account = accounts.FirstOrDefault(a => a.Name == source.Remote[..colon]);
            var folder = source.Remote[(colon + 1)..];
            source.DisplayName = $"{index++:00}  {account?.DisplayName ?? "Tài khoản chưa có tênL.T("} · {new CloudProvider(account?.Type ?? ")cloud", account?.Type ?? "Cloud")}" + (folder == "" ? L.T(" / Thư mục gốc") : " / " + folder);
        }
        sources.Items.Refresh();
    }
    private PoolSource ReadSource()
    {
        if (remote.SelectedItem is not ComboBoxItem account) throw new InvalidOperationException(L.T("Chọn tài khoản cloud."));
        if (!decimal.TryParse(limit.Text.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var gib) || gib < 0 || gib > long.MaxValue / 1073741824m)
            throw new ArgumentException(L.T("Giới hạn phải là số GiB không âm."));
        if (gib > 0 && gib * 1073741824m < 1) throw new ArgumentException(L.T("Giới hạn cần ít nhất 1 byte."));
        return new PoolSource { Remote = account.Tag + ":" + path.Text.Trim().TrimStart('/'), LimitBytes = (long)(gib * 1073741824m) };
    }
    private static void AddField(StackPanel panel, string label, Control input)
    {
        panel.Children.Add(new TextBlock { Text = label, FontSize = 12, Margin = new Thickness(0, 12, 0, 6), Foreground = (Brush)System.Windows.Application.Current.FindResource("Muted") });
        System.Windows.Automation.AutomationProperties.SetName(input, label);
        panel.Children.Add(input);
    }
    private DriveProfile ReadProfile()
    {
        if (pool.IsChecked == true && sources.Items.Count < 2) throw new InvalidOperationException(L.T("Thêm ít nhất 2 tài khoản vào danh sách ổ gộp."));
        if (letter.SelectedItem is not string drive) throw new InvalidOperationException(L.T("Chọn ký tự ổ đĩa còn trống."));
        if (pool.IsChecked != true && remote.SelectedItem is not ComboBoxItem) throw new InvalidOperationException(L.T("Chọn tài khoản cloud."));
        var result = new DriveProfile { Id = original?.Id ?? Guid.NewGuid().ToString("N"), Name = DriveProfile.ResolveName(name.Text, profiles, original?.Id),
            Remote = pool.IsChecked == true ? "" : ((ComboBoxItem)remote.SelectedItem).Tag + ":" + path.Text.Trim().TrimStart('/'),
            Sources = pool.IsChecked == true ? sources.Items.Cast<PoolSource>().ToList() : new(),
            Letter = drive, ReadOnly = readOnly.IsChecked == true, AutoConnect = auto.IsChecked == true };
        DriveService.ValidateProfile(result);
        if (profiles.Any(p => p.Id != result.Id && p.Letter.Equals(result.Letter, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException(L.T("Ký tự ổ đĩa đã được dùng trong cấu hình khác."));
        return result;
    }
    private async Task TestAsync()
    {
        if (testing) return;
        try { var p = ReadProfile(); testing = true; IsEnabled = false; feedback.Text = L.T("Đang kiểm tra các nguồn cloud…"); await service.TestProfileAsync(p); feedback.Text = L.T("✓ Các tài khoản và thư mục truy cập được."); }
        catch (Exception ex) { feedback.Text = ex.Message; }
        finally { testing = false; IsEnabled = true; }
    }
}

