using System.Diagnostics;
using System.Text.Json;
using EasyMount;

// Opt-in real-cloud acceptance, never part of unattended fixture CI.
if (args.Length != 5) throw new ArgumentException("Supply engine, private config, remote name, NEW private work directory and Python executable.");
var engine = Path.GetFullPath(args[0]);
var sourceConfig = Path.GetFullPath(args[1]);
var remote = args[2];
var work = Path.GetFullPath(args[3]);
var repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
if (Directory.Exists(work) || work.StartsWith(repo + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
    throw new ArgumentException("Use a new private directory outside the source checkout; it will contain account credentials.");
if (remote.Contains(':') || remote.Contains('/') || remote.Contains('\\')) throw new ArgumentException("Supply a configured remote name, not a path.");
Directory.CreateDirectory(work);
using var service = new DriveService(work, engine);
File.Copy(sourceConfig, service.ConfigPath);
var accounts = await service.ListRemotesAsync();
var account = accounts.Single(a => a.Name == remote);
if (account.Type is not ("drive" or "onedrive" or "dropbox")) throw new ArgumentException("Only the three supported providers may be tested.");
var used = DriveInfo.GetDrives().Select(d => d.Name[..2]).ToHashSet(StringComparer.OrdinalIgnoreCase);
var letter = new[] { "Z:", "Y:", "X:", "Q:", "P:" }.FirstOrDefault(d => !used.Contains(d) && NetworkMap.IsAbsent(d));
if (letter is null) throw new IOException("No unused test drive letter is available.");
var profile = new DriveProfile { Name = "EasyMount cloud acceptance", Letter = letter,
    Remote = remote + ":EasyMount-VFS-root-" + Guid.NewGuid().ToString("N"), ReadOnly = false, AutoConnect = false };
service.SaveProfiles(new[] { profile });
var checks = new List<string>();
var passed = false;
var disconnected = false;
string? failure = null;
try
{
    await Cloud("mkdir", profile.Remote);
    checks.Add("create-unique-disposable-root");
    await service.ConnectAsync(profile);
    if (!service.IsConnected(profile.Id) || !Directory.Exists(letter + "\\")) throw new IOException("Temporary mount unavailable.");
    checks.Add("native-service-connect");
    var info = new ProcessStartInfo(args[4]) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var item in new[] { Path.Combine(repo, "tests", "check-mounted-cloud.py"), "--engine", engine, "--config", service.ConfigPath,
        "--profiles", Path.Combine(work, "drives.json"), "--report", Path.Combine(work, "mounted-results.json") }) info.ArgumentList.Add(item);
    using var check = Process.Start(info) ?? throw new IOException("Cannot start mounted-file checks.");
    var output = check.StandardOutput.ReadToEndAsync(); var error = check.StandardError.ReadToEndAsync();
    await check.WaitForExitAsync(); await output; await error;
    if (check.ExitCode != 0) throw new IOException("Mounted-file checks failed; private fixtures and cache retained.");
    using var report = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(work, "mounted-results.json")));
    var results = report.RootElement.EnumerateArray().ToArray();
    if (results.Length != 1 || results[0].GetProperty("status").GetString() != "passed") throw new IOException("Expected one passing mounted-file result.");
    foreach (var item in results[0].GetProperty("checks").EnumerateArray()) checks.Add(item.GetString()!);
    await Cloud("rmdir", profile.Remote); // Never recursive: deletion must fail if anything remains.
    checks.Add("remove-empty-disposable-root");
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
    var record = new { provider = account.Type, status = passed && disconnected ? "passed" : "failed", checks,
        failure, cachePreserved = true, workerMayRemain = !disconnected, scope = "one existing account; temporary VFS; generated fixture; no fresh OAuth" };
    await File.WriteAllTextAsync(Path.Combine(work, "summary.json"), JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"{account.Type} VFS: {record.status}; {checks.Count} checks; disconnected={disconnected}. Private work directory retained.");
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
