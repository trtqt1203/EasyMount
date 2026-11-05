using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
namespace EasyMount;
public sealed class SettingsWindow : Window
{
 readonly DesktopSettings options; readonly Action save; readonly Action clearCache;
 readonly StackPanel content=new(); readonly TextBlock toast=new();
 readonly StackPanel nav;
 string selectedTab="Giao diện";
 readonly DispatcherTimer toastTimer=new(){Interval=TimeSpan.FromSeconds(2)};
 readonly Brush muted=(Brush)Application.Current.FindResource("Muted");
 public SettingsWindow(DesktopSettings options,Action save,Action clearCache)
 {
  this.options=options;this.save=save;this.clearCache=clearCache;
  Style=(Style)FindResource(typeof(Window));Title=L.T("Cài đặt · EasyMount");Width=680;Height=520;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;
  var grid=new Grid();grid.ColumnDefinitions.Add(new(){Width=new GridLength(160)});grid.ColumnDefinitions.Add(new());Content=grid;
  nav=new StackPanel{Margin=new Thickness(10,16,10,16)};var side=new Border{Background=(Brush)FindResource("Surface"),BorderBrush=Theme.Brush("#1E3D35"),BorderThickness=new Thickness(0,0,1,0),Child=nav};grid.Children.Add(side);
  var right=new DockPanel{Margin=new Thickness(28,24,28,16)};Grid.SetColumn(right,1);grid.Children.Add(right);
  DockPanel.SetDock(toast,Dock.Bottom);toast.FontSize=11;toast.Foreground=(Brush)FindResource("Accent");toast.Margin=new Thickness(0,10,0,0);right.Children.Add(toast);
  right.Children.Add(new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
  BuildNav();
  toastTimer.Tick+=(_,_)=>{toast.Text="";toastTimer.Stop();};Closed+=(_,_)=>{toastTimer.Stop();L.Current.PropertyChanged-=LanguageUpdated;};L.Current.PropertyChanged+=LanguageUpdated;Show(L.T(selectedTab));
 }
 void BuildNav()
 {
  nav.Children.Clear();
  nav.Children.Add(new TextBlock{Text=L.T("CÀI ĐẶT"),FontSize=11,Foreground=muted,Margin=new Thickness(10,0,0,8)});
  foreach(var item in new[]{"Giao diện","Kết nối","Thông báo","Hiệu năng","Cập nhật"})
  {
   var label=new StackPanel{Orientation=Orientation.Horizontal};var icon=new System.Windows.Shapes.Path{Stroke=(Brush)FindResource("Muted"),StrokeThickness=1.5,Width=14,Height=14,Stretch=Stretch.Uniform,Margin=new Thickness(0,0,8,0),Data=Geometry.Parse(item=="Kết nối"?"M 2,8 L 6,4 L 10,4 M 6,12 L 10,12 L 14,8 M 5,9 L 11,7":item=="Thông báo"?"M 3,12 L 13,12 L 11,9 L 11,5 C 11,1 5,1 5,5 L 5,9 Z M 6,15 L 10,15":item=="Hiệu năng"?"M 9,1 L 3,9 L 8,9 L 6,15 L 13,6 L 8,6 Z":"M 13,6 C 11,0 2,2 2,8 C 2,14 11,15 13,10 M 13,2 L 13,6 L 9,6")};label.Children.Add(icon);label.Children.Add(new TextBlock{Text=L.T(item)});var b=new Button{Content=label,HorizontalContentAlignment=HorizontalAlignment.Left,FontSize=13,Padding=new Thickness(10,7,10,7),MinHeight=34,Margin=new Thickness(0,2,0,2),Background=Brushes.Transparent,BorderThickness=new Thickness(2,0,0,0)};
   b.Click+=(_,_)=>{foreach(var child in nav.Children)if(child is Button n)n.Background=Brushes.Transparent;b.Background=Theme.Brush("#123B31");selectedTab=item;Show(L.T(item));};nav.Children.Add(b);
   if(item==selectedTab)b.Background=Theme.Brush("#123B31");
  }
 }
 void LanguageUpdated(object? sender,System.ComponentModel.PropertyChangedEventArgs e)
 {
  Dispatcher.BeginInvoke(new Action(()=>{Title=L.T("Cài đặt · EasyMount");BuildNav();Show(L.T(selectedTab));}));
 }
 ComboBox AppearanceChoice(string label,(string Id,string Name)[] values,string selected,Action<string> change)
 {
  var box=new ComboBox{MinWidth=135,FontSize=12,VerticalAlignment=VerticalAlignment.Center};
  System.Windows.Automation.AutomationProperties.SetName(box,label);
  foreach(var value in values){var item=new ComboBoxItem{Content=value.Name,Tag=value.Id};box.Items.Add(item);if(value.Id==selected)box.SelectedItem=item;}
  box.SelectionChanged+=(_,_)=>{if(box.SelectedItem is ComboBoxItem item && item.Tag is string id){change(id);Changed();}};
  return box;
 }
 void Changed(){try{save();toast.Text=L.T("✓ Đã lưu");}catch(Exception ex){toast.Text=L.T("Chưa lưu được: ")+ex.Message;}toastTimer.Stop();toastTimer.Start();}
 StackPanel Row(string label,string sub,UIElement control)
 {
  var row=new DockPanel{MinHeight=44,LastChildFill=true};DockPanel.SetDock(control,Dock.Right);row.Children.Add(control);var text=new StackPanel{VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,5,12,5)};
  text.Children.Add(new TextBlock{Text=label,FontSize=13});if(sub!="")text.Children.Add(new TextBlock{Text=sub,FontSize=12,Foreground=muted,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,3,0,0)});row.Children.Add(text);
  var result=new StackPanel();result.Children.Add(new Border{Child=row,BorderBrush=Theme.Brush("#1E3D35"),BorderThickness=new Thickness(0,0,0,1)});content.Children.Add(result);return result;
 }
 void Toggle(string label,string sub,bool value,Action<bool> update,Func<UIElement>? child=null)
 {
  var button=new ToggleButton{IsChecked=value,Width=36,Height=20,VerticalAlignment=VerticalAlignment.Center,Style=(Style)FindResource("SettingToggle")};System.Windows.Automation.AutomationProperties.SetName(button,label);
  var row=Row(label,sub,button);Border? childBox=null;
  if(child!=null){childBox=new Border{Child=child(),Margin=new Thickness(16,0,0,0),Padding=new Thickness(10),Background=Theme.Brush("#163028"),Visibility=value?Visibility.Visible:Visibility.Collapsed};row.Children.Add(childBox);}
  button.Click+=(_,_)=>{try{update(button.IsChecked==true);if(childBox!=null)childBox.Visibility=button.IsChecked==true?Visibility.Visible:Visibility.Collapsed;Changed();}catch(Exception ex){button.IsChecked=button.IsChecked!=true;toast.Text=L.T("Không áp dụng được: ")+ex.Message;}};
 }
 ComboBox Choice(string name,string[] values,string selected,Action<string> change)
 {
  var c=new ComboBox{ItemsSource=values,SelectedItem=selected,MinWidth=95,FontSize=11,VerticalAlignment=VerticalAlignment.Center};System.Windows.Automation.AutomationProperties.SetName(c,name);c.SelectionChanged+=(_,_)=>{if(c.SelectedItem is string v){change(v);Changed();}};return c;
 }
 UIElement Child(string text,UIElement control){var p=new DockPanel();DockPanel.SetDock(control,Dock.Right);p.Children.Add(control);p.Children.Add(new TextBlock{Text=text,FontSize=11,VerticalAlignment=VerticalAlignment.Center,Foreground=muted});return p;}
 void Show(string tab)
 {
  content.Children.Clear();content.Children.Add(new TextBlock{Text=tab==L.T("Kết nối")?L.T("KẾT NỐI & ĐỒNG BỘ"):tab.ToUpperInvariant(),FontSize=11,Foreground=muted,Margin=new Thickness(0,0,0,12)});
  if(tab==L.T("Giao diện"))
  {
   Row(L.T("Ngôn ngữ"),L.T("Chọn ngôn ngữ hiển thị"),AppearanceChoice(L.T("Ngôn ngữ"),new[]{("en","English"),("vi","Tiếng Việt")},options.Language=="vi"?"vi":"en",value=>options.Language=value));
   Row(L.T("Chủ đề"),L.T("Đổi giữa giao diện sáng và tối"),AppearanceChoice(L.T("Chủ đề"),new[]{("dark",L.T("Tối")),("light",L.T("Sáng"))},options.Theme=="light"?"light":"dark",value=>options.Theme=value));
   content.Children.Add(new TextBlock{Text=L.T("Áp dụng ngay và lưu cho lần mở sau"),FontSize=11,Foreground=muted,Margin=new Thickness(0,18,0,0)});
  }
  if(tab==L.T("Kết nối"))
  {
   Toggle(L.T("Tự động kết nối khi khởi động"),L.T("Áp dụng các ổ đã bật Tự kết nối"),options.AutoConnect,v=>options.AutoConnect=v);
   Toggle(L.T("Thử lại khi mất kết nối"),L.T("Chỉ thử lại phiên bị ngắt ngoài ý muốn"),options.Retry,v=>options.Retry=v,()=>Child(L.T("Interval thử lại"),Choice(L.T("Interval thử lại"),new[]{L.T("30 giây"),L.T("1 phút"),L.T("5 phút")},options.RetrySeconds==30?L.T("30 giây"):options.RetrySeconds==300?L.T("5 phút"):L.T("1 phút"),v=>options.RetrySeconds=v==L.T("30 giây")?30:v==L.T("5 phút")?300:60)));
   Row(L.T("Timeout kết nối"),L.T("Thời gian chờ ổ Windows xuất hiện"),Choice(L.T("Timeout kết nối"),new[]{L.T("10 giây"),L.T("30 giây"),L.T("60 giây")},options.TimeoutSeconds+L.T(" giây"),v=>options.TimeoutSeconds=int.Parse(v.Split(' ')[0])));
   var slider=new Slider{Minimum=1,Maximum=10,Value=1,Width=85,IsEnabled=false};Row(L.T("Số kết nối đồng thời"),L.T("1 · Tạo/ngắt ổ tuần tự để bảo vệ phiên và cache"),slider);
  }
  if(tab==L.T("Thông báo"))
  {
   Toggle(L.T("Kết nối thành công"),L.T("Thông báo khi ổ đĩa kết nối"),options.NotifyConnected,v=>options.NotifyConnected=v);
   Toggle(L.T("Mất kết nối"),L.T("Cảnh báo phiên bị ngắt ngoài ý muốn"),options.NotifyDisconnected,v=>options.NotifyDisconnected=v);
   Toggle(L.T("Cảnh báo dung lượng gần đầy"),L.T("Theo dung lượng dịch vụ trả về"),options.NotifyCapacity,v=>options.NotifyCapacity=v,()=>Child(L.T("Ngưỡng cảnh báo"),Choice(L.T("Ngưỡng cảnh báo"),new[]{"80%","90%","95%"},options.CapacityThreshold+"%",v=>options.CapacityThreshold=int.Parse(v.TrimEnd('%')))));
   Toggle(L.T("Âm thanh thông báo"),L.T("Phát âm thanh kèm thông báo"),options.Sound,v=>options.Sound=v);
  }
  if(tab==L.T("Hiệu năng"))
  {
   Toggle(L.T("Giữ ổ kết nối khi đóng cửa sổ"),L.T("Mặc định tắt: đóng cửa sổ sẽ ngắt ổ an toàn và thoát. Bật để giữ ổ trong khay hệ thống."),options.KeepMountedOnClose,v=>options.KeepMountedOnClose=v);
   using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");var exe=Environment.ProcessPath??"";var enabled=string.Equals(key?.GetValue("EasyMount") as string,"\""+exe+"\"",StringComparison.OrdinalIgnoreCase);
   Toggle(L.T("Khởi động cùng Windows"),L.T("Tự mở app khi đăng nhập Windows"),enabled,v=>{using var run=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");if(v)run.SetValue("EasyMount","\""+exe+"\"");else run.DeleteValue("EasyMount",false);});
   Toggle(L.T("Cache metadata ổ đĩa"),L.T("Giữ số liệu dung lượng trong bộ nhớ"),options.CacheMetadata,v=>options.CacheMetadata=v,()=>{var b=new Button{Content=L.T("Xóa cache"),FontSize=11,Padding=new Thickness(8,4,8,4),MinHeight=28};b.Click+=(_,_)=>{if(MessageBox.Show(this,L.T("Xóa metadata dung lượng? Cache file chờ tải lên được giữ nguyên."),"EasyMount",MessageBoxButton.YesNo)==MessageBoxResult.Yes){clearCache();Changed();}};return Child(L.T("Metadata trong bộ nhớ"),b);});
   Toggle(L.T("Ghi nhật ký hoạt động"),L.T("Mức log áp dụng cho kết nối mới"),options.Logging,v=>options.Logging=v,()=>Child(L.T("Mức độ log"),Choice(L.T("Mức độ log"),new[]{L.T("Cơ bản"),L.T("Chi tiết"),"Debug"},options.LogLevel=="DEBUG"?"Debug":options.LogLevel=="NOTICE"?L.T("Cơ bản"):L.T("Chi tiết"),v=>options.LogLevel=v=="Debug"?"DEBUG":v==L.T("Cơ bản")?"NOTICE":"INFO")));
   var view=new Button{Content=L.T("Danh sách / lưới"),MinHeight=30,FontSize=11,Padding=new Thickness(8,4,8,4)};view.Click+=(_,_)=>{MessageBox.Show(this,L.T("Dùng hai nút danh sách/lưới ở thanh công cụ chính."),"EasyMount");};Row(L.T("Chế độ xem ổ đĩa"),L.T("Lưu riêng cho giao diện chính"),view);
  }
  if(tab==L.T("Cập nhật"))
  {
   Row(L.T("Phiên bản hiện tại"),L.T("Bản đang cài trên máy"),new TextBlock{Text="v"+Assembly.GetExecutingAssembly().GetName().Version?.ToString(3),Foreground=(Brush)FindResource("Accent"),FontSize=11,VerticalAlignment=VerticalAlignment.Center});
   var channel=Choice(L.T("Kênh cập nhật"),new[]{"Stable"},"Stable",_=>{});Row(L.T("Kênh cập nhật"),L.T("Chỉ có kênh Stable được phát hành"),channel);
   var reporting=new ToggleButton{IsChecked=false,IsEnabled=false,Width=36,Height=20,Style=(Style)FindResource("SettingToggle")};Row(L.T("Gửi báo cáo lỗi ẩn danh"),L.T("Chưa có dịch vụ nhận báo cáo; không gửi dữ liệu"),reporting);
   var check=new Button{Content=L.T("Kiểm tra cập nhật"),Style=(Style)FindResource("Primary"),Margin=new Thickness(0,18,0,0)};check.Click+=(_,_)=>new UpdatesWindow{Owner=this}.ShowDialog();content.Children.Add(check);
  }
 }
}
