using System.Diagnostics;
using System.Text.Json;
using EasyMount;

// Opt-in real-cloud acceptance, never part of unattended fixture CI.
if (args.Length is not (5 or 6)) throw new ArgumentException("Supply engine, private config, remote name, NEW private work directory, Python executable and optional fixture MiB (1 or 128).");
var engine = Path.GetFullPath(args[0]);
var sourceConfig = Path.GetFullPath(args[1]);
var remotes = args[2].Split(',');
if (remotes.Length is not (1 or 5) || remotes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != remotes.Length)
    throw new ArgumentException("Supply one remote or five distinct comma-separated Google Drive remotes.");
var isPool = remotes.Length == 5;
var fixtureMiB = args.Length == 6 ? int.Parse(args[5]) : 1;
if (fixtureMiB is not (1 or 128) || isPool && fixtureMiB != 1) throw new ArgumentException("Use a 1 or 128 MiB single-drive fixture; pool fixtures must be 1 MiB.");
var work = Path.GetFullPath(args[3]);
var repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
if (Directory.Exists(work) || work.StartsWith(repo + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
    throw new ArgumentException("Use a new private directory outside the source checkout; it will contain account credentials.");
if (remotes.Any(remote => remote.Contains(':') || remote.Contains('/') || remote.Contains('\\'))) throw new ArgumentException("Supply configured remote names, not paths.");
Directory.CreateDirectory(work);
using var service = new DriveService(work, engine);
File.Copy(sourceConfig, service.ConfigPath);
var accounts = await service.ListRemotesAsync();
var selectedAccounts = remotes.Select(remote => accounts.Single(a => a.Name == remote)).ToArray();
var account = selectedAccounts[0];
if (selectedAccounts.Any(a => a.Type is not ("drive" or "onedrive" or "dropbox")) || isPool && selectedAccounts.Any(a => a.Type != "drive"))
    throw new ArgumentException("Supply one supported provider or five Google Drive accounts.");
var provider = isPool ? "google-drive-pool" : account.Type;
var used = DriveInfo.GetDrives().Select(d => d.Name[..2]).ToHashSet(StringComparer.OrdinalIgnoreCase);
var letter = new[] { "Z:", "Y:", "X:", "Q:", "P:" }.FirstOrDefault(d => !used.Contains(d) && NetworkMap.IsAbsent(d));
if (letter is null) throw new IOException("No unused test drive letter is available.");
var folder = "EasyMount-VFS-root-" + Guid.NewGuid().ToString("N");
var roots = remotes.Select(remote => remote + ":" + folder).ToArray();
var profile = new DriveProfile { Name = "EasyMount cloud acceptance", Letter = letter,
    Remote = isPool ? "" : roots[0], ReadOnly = false, AutoConnect = false,
    Sources = isPool ? roots.Select(root => new PoolSource { Remote = root, LimitBytes = 1572864 }).ToList() : new() };
service.SaveProfiles(new[] { profile });
var checks = new List<string>();
var passed = false;
var disconnected = false;
string? failure = null;
try
{
    foreach (var root in roots) await Cloud("mkdir", root);
    checks.Add("create-unique-disposable-roots");
    await service.ConnectAsync(profile);
    if (!service.IsConnected(profile.Id) || !Directory.Exists(letter + "\\")) throw new IOException("Temporary mount unavailable.");
    checks.Add("native-service-connect");
    var info = new ProcessStartInfo(args[4]) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var item in new[] { Path.Combine(repo, "tests", isPool ? "check-mounted-pool.py" : "check-mounted-cloud.py"), "--engine", engine, "--config", service.ConfigPath,
        "--profiles", Path.Combine(work, "drives.json"), "--report", Path.Combine(work, "mounted-results.json") }) info.ArgumentList.Add(item);
    if (!isPool) { info.ArgumentList.Add("--fixture-mib"); info.ArgumentList.Add(fixtureMiB.ToString()); }
    using var check = Process.Start(info) ?? throw new IOException("Cannot start mounted-file checks.");
    var output = check.StandardOutput.ReadToEndAsync(); var error = check.StandardError.ReadToEndAsync();
    await check.WaitForExitAsync(); await output; await error;
    if (check.ExitCode != 0) throw new IOException("Mounted-file checks failed; private fixtures and cache retained.");
    using var report = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(work, "mounted-results.json")));
    var results = report.RootElement.EnumerateArray().ToArray();
    if (results.Length != 1 || results[0].GetProperty("status").GetString() != "passed") throw new IOException("Expected one passing mounted-file result.");
    foreach (var item in results[0].GetProperty("checks").EnumerateArray()) checks.Add(item.GetString()!);
    foreach (var root in roots) await Cloud("rmdir", root); // Never recursive: deletion must fail if anything remains.
    checks.Add("remove-empty-disposable-roots");
    passed = true;
}
catch (Exception ex) { failure = ex.GetType().Name; }
finally
{
    try
    {
        await service.PrepareUninstallAsync(); // Reuse authenticated queue/error checks; never kill a worker.
        disconnected = !service.IsConnected(profile.Id) && !Directory.Exists(letter + "\\") && NetworkMap.IsAbsent(letter);
        if (disconnected) checks.Add("safe-disconnect-no-logical-drive");
    }
    catch { failure ??= "SafeDisconnectRefused"; }
    var record = new { provider, accountCount = remotes.Length, fixtureBytes = fixtureMiB * 1048576, status = passed && disconnected ? "passed" : "failed", checks,
        failure, cachePreserved = true, workerMayRemain = !disconnected,
        scope = isPool ? "existing OAuth; temporary VFS; generated fixtures; pool limits 1.5 MiB per source, not physical quota exhaustion"
            : "one existing OAuth account; temporary VFS; generated fixture; no interruption or fresh-login testing" };
    await File.WriteAllTextAsync(Path.Combine(work, "summary.json"), JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"{provider} VFS: {record.status}; {checks.Count} checks; disconnected={disconnected}. Private work directory retained.");
}

return passed && disconnected ? 0 : 1;

async Task Cloud(string operation, string path)
{
    var info = new ProcessStartInfo(engine) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var item in new[] { operation, path, "--config", service.ConfigPath, "--retries", "1", "--low-level-retries", "1", "--contimeout", "15s", "--timeout", "30s" }) info.ArgumentList.Add(item);
    using var process = Process.Start(info) ?? throw new IOException("Cannot start isolated cloud operation.");
    var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync(); await output; await error;
    if (process.ExitCode != 0) throw new IOException("Isolated cloud operation failed; provider output withheld.");
}

internal static class NetworkMap
{
    [System.Runtime.InteropServices.DllImport("mpr.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int WNetGetConnectionW(string localName, System.Text.StringBuilder remoteName, ref int length);

    public static bool IsAbsent(string letter)
    {
        var buffer = new System.Text.StringBuilder(4096); var length = buffer.Capacity;
        return WNetGetConnectionW(letter, buffer, ref length) == 2250;
    }
}
