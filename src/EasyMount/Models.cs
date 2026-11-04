using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace EasyMount;

public sealed class DriveProfile
{
    public static string ResolveName(string? name, IEnumerable<DriveProfile> profiles, string? exceptId = null)
    {
        if (!string.IsNullOrWhiteSpace(name)) return name.Trim();
        var used = profiles.Where(p => p.Id != exceptId).Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; ; i++) if (!used.Contains("Drive" + i)) return "Drive" + i;
    }
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Remote { get; set; } = "";
    public List<PoolSource> Sources { get; set; } = new();
    [JsonIgnore] public bool IsPool => Sources.Count > 0;
    public string Letter { get; set; } = "R:";
    public bool ReadOnly { get; set; }
    public bool AutoConnect { get; set; }
}

public sealed class PoolSource
{
    public string Remote { get; set; } = "";
    public long LimitBytes { get; set; }
    [JsonIgnore] public string? DisplayName { get; set; }
    public override string ToString() => (DisplayName ?? Remote) + (LimitBytes > 0 ? $"  Â·  tá»‘i Ä‘a {LimitBytes / 1073741824.0:0.##} GiB" : "  Â·  dung lÆ°á»£ng tá»± Ä‘á»™ng");
}

public sealed record RemoteAccount(string Name, string Type)
{
    public string DisplayName { get; init; } = Name;
    public string? Email { get; init; }
}
public sealed record DriveSnapshot(string ProfileId, string Status, string? Error = null);
public sealed record DriveCapacity(long UsedBytes, long TotalBytes, DateTime CheckedAt);

public static class DrivePresentation
{
    public static string[] ChildNames(DriveProfile pool, IEnumerable<DriveProfile> profiles, IEnumerable<RemoteAccount> accounts) => pool.Sources.Select(source =>
        profiles.FirstOrDefault(p => !p.IsPool && p.Remote.Equals(source.Remote, StringComparison.OrdinalIgnoreCase))?.Name
        ?? profiles.FirstOrDefault(p => !p.IsPool && p.Remote.Split(':')[0].Equals(source.Remote.Split(':')[0], StringComparison.OrdinalIgnoreCase))?.Name
        ?? accounts.FirstOrDefault(a => a.Name.Equals(source.Remote.Split(':')[0], StringComparison.OrdinalIgnoreCase))?.DisplayName
        ?? source.Remote.Split(':')[0]).ToArray();

    // Cloud quota counts once, even when exposed through a pool and separate drives.
    public static DriveCapacity? WorkspaceCapacity(IEnumerable<(DriveProfile Profile, DriveCapacity? Capacity)> rows)
    {
        var entries = rows.Select(r => (r.Capacity, Sources: (r.Profile.IsPool ? r.Profile.Sources.Select(s => s.Remote) : new[] { r.Profile.Remote }).Select(s => s.Split(':')[0]).ToHashSet(StringComparer.OrdinalIgnoreCase))).ToList();
        var required = entries.SelectMany(e => e.Sources).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long used = 0, total = 0;
        foreach (var entry in entries.Where(e => e.Capacity is not null).OrderByDescending(e => e.Sources.Count))
        {
            if (entry.Sources.IsSubsetOf(covered)) continue;
            if (entry.Sources.Overlaps(covered)) return null; // Partially overlapping pool totals cannot be split reliably.
            covered.UnionWith(entry.Sources);
            used = checked(used + entry.Capacity!.UsedBytes); total = checked(total + entry.Capacity.TotalBytes);
        }
        return total > 0 && covered.SetEquals(required) ? new DriveCapacity(used, total, DateTime.Now) : null;
    }
}
