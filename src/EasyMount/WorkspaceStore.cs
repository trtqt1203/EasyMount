using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
namespace EasyMount;
public sealed class WorkspaceInfo
{
 public string Id { get; set; } = Guid.NewGuid().ToString("N");
 public string Name { get; set; } = "";
 public string Color { get; set; } = "#00E5B4";
 public override string ToString() => Name;
}
public sealed class WorkspaceStore
{
 public List<WorkspaceInfo> Items { get; set; } = new();
 public string Active { get; set; } = "";
 public Dictionary<string,string> Assignments { get; set; } = new();
 public bool Contains(string profileId) => Active == "" || Assignments.TryGetValue(profileId,out var workspace) && workspace == Active;
 public static WorkspaceStore Load(string path)
 {
  if(!File.Exists(path)) return new();
  var value=JsonSerializer.Deserialize<WorkspaceStore>(File.ReadAllText(path)) ?? throw new IOException("Không đọc được workspace.");
  if(value.Items is null || value.Assignments is null || value.Items.Select(w=>w.Id).Distinct().Count()!=value.Items.Count || value.Items.Any(w=>string.IsNullOrWhiteSpace(w.Id)||string.IsNullOrWhiteSpace(w.Name))) throw new IOException("Cấu hình workspace không hợp lệ.");
  var seeds = new Dictionary<string,string[]> { ["default"]=new[]{"Edit ảnh/video","Ổ đĩa của tôi"}, ["films"]=new[]{"Lưu trữ phim"}, ["documents"]=new[]{"Tài liệu"}, ["web"]=new[]{"Dự án web"} };
  var removed=value.Items.RemoveAll(w=>seeds.TryGetValue(w.Id,out var names) && names.Contains(w.Name) && !value.Assignments.Values.Contains(w.Id))>0;
  var activeChanged=value.Active!="" && !value.Items.Any(w=>w.Id==value.Active);
  if(activeChanged)value.Active="";
  if(removed || activeChanged)
  {
   var backup=path+".before-v063";if(!File.Exists(backup))File.Copy(path,backup);
   value.Save(path);
  }
  return value;
 }
 public void Save(string path) { var temp=path+".tmp"; File.WriteAllText(temp,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true})); File.Move(temp,path,true); }
 public void Delete(string id)
 {
  if (id == "" || Items.RemoveAll(w => w.Id == id) == 0) return;
  foreach (var drive in Assignments.Where(pair => pair.Value == id).Select(pair => pair.Key).ToArray()) Assignments.Remove(drive);
  if (Active == id) Active = "";
 }
 public WorkspaceInfo Create(string name) { if(string.IsNullOrWhiteSpace(name)||name.Trim().Length>40) throw new ArgumentException("Tên workspace cần 1–40 ký tự."); var w=new WorkspaceInfo{Name=name.Trim(),Color=new[]{"#00E5B4","#6366F1","#F59E0B","#F43F5E"}[Items.Count%4]};Items.Add(w);Active=w.Id;return w; }
}
public sealed class DesktopSettings
{
 public string Language { get; set; } = "en";
 public string Theme { get; set; } = "dark";
 public bool AutoConnect { get; set; } = true;
 public bool Retry { get; set; } = true;
 public int RetrySeconds { get; set; } = 60;
 public int TimeoutSeconds { get; set; } = 30;
 public bool NotifyConnected { get; set; }
 public bool NotifyDisconnected { get; set; } = true;
 public bool NotifyCapacity { get; set; } = true;
 public int CapacityThreshold { get; set; } = 90;
 public bool Sound { get; set; }
 public bool Background { get; set; } = true;
 public bool CacheMetadata { get; set; } = true;
 public bool Logging { get; set; } = true;
 public string LogLevel { get; set; } = "INFO";
 public static DesktopSettings Load(string path) => File.Exists(path) ? JsonSerializer.Deserialize<DesktopSettings>(File.ReadAllText(path)) ?? new() : new();
 public void Save(string path) { var temp=path+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,path,true); }
}
