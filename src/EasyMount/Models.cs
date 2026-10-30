using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EasyMount;

public sealed class DriveProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Remote { get; set; } = "";
    public List<PoolSource> Sources { get; set; } = new();
    [JsonIgnore] public bool IsPool => Sources.Count > 0;
    public string Letter { get; set; } = "R:";
    public bool ReadOnly { get; set; } = true;
    public bool AutoConnect { get; set; }
}

public sealed class PoolSource
{
    public string Remote { get; set; } = "";
    public long LimitBytes { get; set; }
    public override string ToString() => Remote + (LimitBytes > 0 ? $"  ·  giới hạn {LimitBytes / 1073741824.0:0.##} GiB" : "  ·  dung lượng tự động");
}

public sealed record RemoteAccount(string Name, string Type)
{
    public string DisplayName { get; init; } = Name;
}
public sealed record DriveSnapshot(string ProfileId, string Status, string? Error = null);
