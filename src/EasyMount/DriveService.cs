using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace EasyMount;

public sealed class DriveService : IDisposable
{
    private sealed record Mount(Process Process, HttpClient Client, string ProfileId, string Letter, bool ReadOnly)
    {
        public bool Started { get; set; }
        public string? NetworkPath { get; set; }
    }
    private readonly ConcurrentDictionary<string, Mount> mounts = new();
    // ponytail: one gate is enough for a desktop drive manager; use per-drive gates if parallel startup is needed.
    private readonly SemaphoreSlim operationGate = new(1, 1);
    private readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
    private bool disposed;

    public string DataDirectory { get; }
    public string ConfigPath => Path.Combine(DataDirectory, "rclone.conf");
    public string RclonePath { get; }
    public bool HasRclone => File.Exists(RclonePath);
    public bool HasWinFsp
    {
        get
        {
            foreach (var folder in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) })
                if (File.Exists(Path.Combine(folder, "WinFsp", "bin", "winfsp-x64.dll"))) return true;
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\WinFsp") ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WinFsp");
            return key?.GetValue("InstallDir") is string installDir && File.Exists(Path.Combine(installDir, "bin", "winfsp-x64.dll"));
        }
    }
    public event Action<DriveSnapshot>? StateChanged;

    public DriveService(string? dataDirectory = null, string? rclonePath = null)
    {
        DataDirectory = dataDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyMount");
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(Path.Combine(DataDirectory, "logs"));
        RclonePath = rclonePath ?? FindRclone();
    }

    private static string FindRclone()
    {
        var bundled = Path.Combine(AppContext.BaseDirectory, "tools", "rclone.exe");
        if (File.Exists(bundled)) return bundled;
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            try
            {
                var candidate = Path.Combine(directory.Trim('"'), "rclone.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch (ArgumentException) { }
        }
        return bundled;
    }

    public static void ValidateProfile(DriveProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!Guid.TryParseExact(profile.Id, "N", out _)) throw new ArgumentException("Mã ổ đĩa không hợp lệ.");
        if (string.IsNullOrWhiteSpace(profile.Name) || profile.Name.Length > 64 || profile.Name.Any(char.IsControl))
            throw new ArgumentException("Tên ổ đĩa cần có 1–64 ký tự và không chứa ký tự điều khiển.");
        if (profile.Sources is null) throw new ArgumentException("Danh sách cloud không hợp lệ.");
        if (profile.IsPool)
        {
            if (profile.Sources.Count < 2) throw new ArgumentException("Ổ gộp cần ít nhất hai nguồn cloud.");
            foreach (var source in profile.Sources)
            {
                if (source is null || source.LimitBytes < 0) throw new ArgumentException("Nguồn cloud hoặc giới hạn dung lượng không hợp lệ.");
                ValidateRemote(source.Remote);
                if (source.Remote.IndexOfAny(new[] { '\"', '\\' }) >= 0 || source.Remote.EndsWith(":ro", StringComparison.OrdinalIgnoreCase) || source.Remote.EndsWith(":nc", StringComparison.OrdinalIgnoreCase) || source.Remote.EndsWith(":writeback", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Đường dẫn ổ gộp không được chứa dấu nháy kép, dấu gạch chéo ngược hoặc hậu tố điều khiển.");
            }
            // One source per remote prevents aliases of the same account/folder from being counted twice.
            var names = profile.Sources.Select(s => s.Remote[..s.Remote.IndexOf(':')]);
            if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != profile.Sources.Count)
                throw new ArgumentException("Mỗi tài khoản chỉ được chọn một lần trong cùng ổ gộp.");
        }
        else ValidateRemote(profile.Remote);
        if (!Regex.IsMatch(profile.Letter ?? "", "^[D-Z]:$"))
            throw new ArgumentException("Chọn ký tự ổ đĩa từ D: đến Z:.");
    }

    private static void ValidateRemote(string remote)
    {
        if (string.IsNullOrWhiteSpace(remote) || remote.Length > 2048 || remote.Any(char.IsControl) ||
            !Regex.IsMatch(remote, @"^[\p{L}\p{N}][\p{L}\p{N} _-]{0,63}:"))
            throw new ArgumentException("Đường dẫn cần có dạng tên-tài-khoản:thư-mục, ví dụ gdrive:Documents.");
        var path = remote[(remote.IndexOf(':') + 1)..];
        if (path.Split('/', '\\').Any(segment => segment == "..")) throw new ArgumentException("Thư mục cloud không được chứa đoạn ..");
    }

    public List<DriveProfile> LoadProfiles()
    {
        var path = Path.Combine(DataDirectory, "drives.json");
        if (!File.Exists(path)) return new();
        try
        {
            var profiles = JsonSerializer.Deserialize<List<DriveProfile>>(File.ReadAllText(path)) ?? throw new JsonException("Empty profile file.");
            ValidateProfiles(profiles);
            return profiles;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            throw new InvalidDataException($"Không đọc được cấu hình ổ đĩa. Tệp gốc được giữ tại {path}.", ex);
        }
    }

    private static void ValidateProfiles(IReadOnlyList<DriveProfile> profiles)
    {
        foreach (var profile in profiles) ValidateProfile(profile);
        if (profiles.Select(x => x.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != profiles.Count)
            throw new ArgumentException("Cấu hình có mã ổ đĩa trùng nhau.");
        if (profiles.Select(x => x.Letter).Distinct(StringComparer.OrdinalIgnoreCase).Count() != profiles.Count)
            throw new ArgumentException("Mỗi ổ đĩa cần một ký tự riêng.");
    }

    public void SaveProfiles(IEnumerable<DriveProfile> profiles)
    {
        var list = profiles.ToList();
        ValidateProfiles(list);
        foreach (var old in LoadProfiles())
        {
            var next = list.FirstOrDefault(p => p.Id == old.Id);
            if ((next is null || RoutingIdentity(next) != RoutingIdentity(old)) &&
                Directory.Exists(Path.Combine(DataDirectory, "cache", old.Id)) &&
                Directory.EnumerateFiles(Path.Combine(DataDirectory, "cache", old.Id), "*", SearchOption.AllDirectories).Any())
            {
                var clean = Path.Combine(DataDirectory, "cache", old.Id + ".clean");
                if (IsConnected(old.Id) || File.Exists(SessionFile(old.Id)) || !File.Exists(clean) || File.ReadAllText(clean) != RoutingIdentity(old))
                    throw new IOException("Ổ còn cache chưa xác minh. Kết nối lại cấu hình cũ, chờ upload xong và ngắt an toàn trước khi đổi nguồn. Bạn vẫn có thể đổi tên ổ.");
                var archive = Path.Combine(DataDirectory, "cache-archive");
                Directory.CreateDirectory(archive);
                Directory.Move(Path.Combine(DataDirectory, "cache", old.Id), Path.Combine(archive, old.Id + "-" + Guid.NewGuid().ToString("N")));
                File.Delete(clean); // Cached downloads are archived, never reused for a different cloud route.
            }
        }
        var path = Path.Combine(DataDirectory, "drives.json");
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, list, jsonOptions);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temp, path, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private ProcessStartInfo StartInfo(IEnumerable<string> args, bool hidden = true)
    {
        if (!HasRclone) throw new FileNotFoundException("Thiếu thành phần kết nối. Chạy lại bộ cài EasyMount.");
        var info = new ProcessStartInfo(RclonePath)
        {
            UseShellExecute = false,
            CreateNoWindow = hidden,
            RedirectStandardOutput = hidden,
            RedirectStandardError = hidden,
            StandardOutputEncoding = hidden ? Encoding.UTF8 : null,
            StandardErrorEncoding = hidden ? Encoding.UTF8 : null
        };
        info.ArgumentList.Add("--config");
        info.ArgumentList.Add(ConfigPath);
        foreach (var argument in args) info.ArgumentList.Add(argument);
        return info;
    }

    private static async Task<string> ReadBoundedAsync(StreamReader reader, int limit = 65536)
    {
        var result = new StringBuilder();
        var buffer = new char[4096];
        int read;
        while ((read = await reader.ReadAsync(buffer)) != 0)
            if (result.Length < limit) result.Append(buffer, 0, Math.Min(read, limit - result.Length));
        return result.ToString();
    }

    private async Task<string> RunAsync(string[] args, bool captureOutput = true, int outputLimit = 65536)
    {
        using var process = new Process { StartInfo = StartInfo(args) };
        process.Start();
        var stdout = ReadBoundedAsync(process.StandardOutput, captureOutput ? outputLimit : 0);
        var stderr = ReadBoundedAsync(process.StandardError);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(40));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true); // Listing/testing has no write operations.
            await process.WaitForExitAsync();
            await Task.WhenAll(stdout, stderr);
            throw new TimeoutException("Dịch vụ lưu trữ không phản hồi trong 40 giây. Kiểm tra mạng hoặc đăng nhập lại.");
        }
        await Task.WhenAll(stdout, stderr);
        if (process.ExitCode != 0) throw new InvalidOperationException("EasyMount: " + FriendlyError(await stderr));
        return await stdout;
    }

    private static string FriendlyError(string message)
    {
        // Do not surface OAuth query tokens if a provider includes a URL in its error text.
        message = Regex.Replace(message, @"(?i)(access_token|refresh_token|client_secret|authorization|password)([=: ]+)[^\s&\""']+", "$1$2[redacted]");
        return string.IsNullOrWhiteSpace(message) ? "Không thể hoàn tất thao tác. Xem nhật ký để biết thêm." : message.Trim()[..Math.Min(message.Trim().Length, 1800)];
    }

    public async Task<IReadOnlyList<RemoteAccount>> ListRemotesAsync()
    {
        var labelsFile = Path.Combine(DataDirectory, "account-labels.json");
        var labels = File.Exists(labelsFile) ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(labelsFile)) ?? new() : new Dictionary<string, string>();
        var output = await RunAsync(new[] { "listremotes", "--long" }, outputLimit: int.MaxValue);
        var accounts = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => Regex.Match(line.Trim(), @"^(.+?):\s+(\S+)\s*$"))
            .Where(match => match.Success)
            .Select(match => new RemoteAccount(match.Groups[1].Value, match.Groups[2].Value) { DisplayName = labels.GetValueOrDefault(match.Groups[1].Value, match.Groups[1].Value) })
            .ToList();
        var emailFile = Path.Combine(DataDirectory, "account-emails.json");
        var emails = File.Exists(emailFile) ? JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(emailFile)) ?? new() : new Dictionary<string,string>();
        var missing = accounts.Where(a => a.Type is "drive" or "onedrive" or "dropbox" && !emails.ContainsKey(a.Name)).ToList();
        if (missing.Count > 0)
        {
            // Read credentials only in memory; fixed provider endpoints receive their own token.
            try
            {
                using var config = JsonDocument.Parse(await RunAsync(new[] { "config", "dump" }, outputLimit: int.MaxValue));
                foreach (var account in missing)
                {
                    if (!config.RootElement.TryGetProperty(account.Name, out var section) || !section.TryGetProperty("token",out var token)) continue;
                    var email = await AccountSetupSession.ReadAccountEmailAsync(account.Type, token.GetString() ?? "");
                    if (!string.IsNullOrWhiteSpace(email) && email.Contains('@') && !email.Any(char.IsControl)) emails[account.Name] = email;
                }
                var temporary = emailFile + ".tmp";
                File.WriteAllText(temporary, JsonSerializer.Serialize(emails)); File.Move(temporary,emailFile,true);
            }
            catch { /* Identity lookup must not prevent session recovery. */ }
        }
        return accounts.Select(a => a with { Email = emails.GetValueOrDefault(a.Name) }).ToList();
    }

    public void SaveAccountLabel(string remote, string label)
    {
        AccountSetupSession.ValidateName(remote);
        label = label.Trim();
        if (string.IsNullOrWhiteSpace(label) || label.Length > 128 || label.Any(char.IsControl)) throw new ArgumentException("Tên tài khoản cần 1–128 ký tự, không có ký tự điều khiển.");
        var file = Path.Combine(DataDirectory, "account-labels.json");
        var labels = File.Exists(file) ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file)) ?? new() : new Dictionary<string, string>();
        labels[remote] = label;
        var temporary = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(labels)); File.Move(temporary, file, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public async Task TestRemoteAsync(string remote)
    {
        ValidateRemote(remote);
        await RunAsync(new[] { "lsf", remote, "--max-depth", "1", "--dirs-only", "--retries", "1", "--low-level-retries", "1", "--contimeout", "10s", "--timeout", "20s" }, captureOutput: false);
    }

    public async Task ValidateWriteAccessAsync(DriveProfile profile)
    {
        if (profile.ReadOnly) return;
        var remotes = profile.IsPool ? profile.Sources.Select(s => s.Remote) : new[] { profile.Remote };
        foreach (var remote in remotes)
        {
            var section = await RunAsync(new[] { "config", "redacted", remote[..remote.IndexOf(':')] });
            if (Regex.IsMatch(section, @"(?m)^type\s*=\s*drive\s*$") &&
                Regex.IsMatch(section, @"(?m)^scope\s*=\s*(?:https://www.googleapis.com/auth/)?drive\.readonly\s*$"))
                throw new InvalidOperationException("Tài khoản Google Drive này chỉ được cấp quyền đọc. Hãy dùng ổ Chỉ đọc hoặc đăng nhập lại và cấp quyền tải lên/chỉnh sửa trước khi gắn ổ ghi.");
        }
    }

    public async Task TestProfileAsync(DriveProfile profile)
    {
        ValidateProfile(profile);
        await ValidateWriteAccessAsync(profile);
        if (!profile.IsPool) { await TestRemoteAsync(profile.Remote); return; }
        foreach (var source in profile.Sources)
        {
            await TestRemoteAsync(source.Remote);
            if (!profile.ReadOnly && source.LimitBytes == 0)
            {
                var json = await RunAsync(new[] { "about", source.Remote, "--json", "--retries", "1", "--low-level-retries", "1" });
                using var quota = JsonDocument.Parse(json);
                if (!quota.RootElement.TryGetProperty("free", out var free) || !free.TryGetInt64(out var bytes) || bytes < 0)
                    throw new InvalidOperationException($"{source.Remote}: cloud không báo dung lượng trống. Hãy đặt giới hạn dung lượng cho nguồn này.");
            }
        }
    }

    // A stable per-profile remote preserves VFS recovery after a display-name edit.
    public static string MountRemote(DriveProfile profile) => profile.IsPool ? "em_pool_" + profile.Id + ":" : profile.Remote;
    private static string RoutingIdentity(DriveProfile profile) => JsonSerializer.Serialize(new { profile.Remote, profile.Sources });
    public Dictionary<string, string> PoolEnvironment(DriveProfile profile)
    {
        ValidateProfile(profile);
        var values = new Dictionary<string, string>();
        if (!profile.IsPool) return values;
        var directory = Path.Combine(DataDirectory, "pools");
        Directory.CreateDirectory(directory);
        var plan = Path.Combine(directory, profile.Id + ".json");
        var temp = plan + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temp, JsonSerializer.Serialize(profile.Sources, jsonOptions)); File.Move(temp, plan, overwrite: true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        var prefix = "RCLONE_CONFIG_EM_POOL_" + profile.Id.ToUpperInvariant();
        values[prefix + "_TYPE"] = "union";
        values[prefix + "_EASYMOUNT_PLAN"] = plan;
        return values;
    }

    private void Publish(string id, string status, string? error = null) => StateChanged?.Invoke(new DriveSnapshot(id, status, error));
    private static bool IsRunning(Mount mount)
    {
        try { return mount.Started && !mount.Process.HasExited; }
        catch (InvalidOperationException) { return false; }
    }

    private bool RemoveMount(Mount mount) => ((ICollection<KeyValuePair<string, Mount>>)mounts).Remove(new(mount.ProfileId, mount));

    private async Task HandleExitedAsync(Mount mount)
    {
        // Process callbacks must not dispose the client while Connect/Disconnect is using it.
        await operationGate.WaitAsync();
        try
        {
            if (!RemoveMount(mount)) return; // An older process must not remove a newer connection.
            try { WindowsDrive.Removed(mount.Letter, mount.NetworkPath); } catch (IOException) { /* Startup recovery retries shell cleanup. */ }
            DeleteSession(mount.ProfileId);
            mount.Client.Dispose();
            mount.Process.Dispose();
            Publish(mount.ProfileId, "Error", "Tiến trình ổ đĩa đã dừng. Tệp chưa tải lên (nếu có) vẫn được giữ trong bộ nhớ đệm; kết nối lại cùng cấu hình để phục hồi.");
        }
        finally { operationGate.Release(); }
    }

    public bool IsConnected(string id) => mounts.TryGetValue(id, out var mount) && IsRunning(mount);

    private static string NormalizeMountedRemote(string value) => Regex.Replace(value.Replace('\\', '/'), @"://\?/([A-Za-z]:/)", ":$1").TrimEnd('/');
    private string SessionFile(string id) => Path.Combine(DataDirectory, "sessions", id + ".json");
    private void DeleteSession(string id) { var file = SessionFile(id); if (File.Exists(file)) File.Delete(file); }
    private static HttpClient Controller(int port, string secret)
    {
        var client = new HttpClient(new HttpClientHandler { UseProxy = false }) { BaseAddress = new Uri($"http://127.0.0.1:{port}/"), Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("easymount:" + secret)));
        return client;
    }

    public async Task RecoverMountsAsync()
    {
        await operationGate.WaitAsync();
        try
        {
            foreach (var profile in LoadProfiles())
            {
                if (IsConnected(profile.Id)) continue;
                if (!File.Exists(SessionFile(profile.Id)))
                {
                    try { WindowsDrive.Removed(profile.Letter, @"\\server\" + profile.Name); }
                    catch (IOException ex) { Publish(profile.Id, "Error", ex.Message); }
                    continue;
                }
                Process? process = null; HttpClient? client = null;
                try
                {
                    var lease = JsonSerializer.Deserialize<MountSession>(File.ReadAllText(SessionFile(profile.Id))) ?? throw new InvalidDataException();
                    try { process = Process.GetProcessById(lease.ProcessId); }
                    catch (ArgumentException) { WindowsDrive.Removed(profile.Letter, @"\\server\" + profile.Name); DeleteSession(profile.Id); Publish(profile.Id, "Disconnected"); continue; }
                    if (!lease.Matches(process, profile, RoutingIdentity(profile))) throw new InvalidDataException();
                    client = Controller(lease.Port, lease.Secret());
                    var mount = new Mount(process, client, profile.Id, profile.Letter, profile.ReadOnly) { Started = true, NetworkPath = WindowsDrive.Connection(profile.Letter) };
                    using var stats = await RcAsync(mount, "vfs/stats"); // Authenticate before taking control of a surviving process.
                    if (stats.RootElement.GetProperty("opt").GetProperty("ReadOnly").GetBoolean() != lease.ReadOnly ||
                        NormalizeMountedRemote(stats.RootElement.GetProperty("fs").GetString() ?? "") != NormalizeMountedRemote(MountRemote(profile)))
                        throw new InvalidDataException();
                    mounts[profile.Id] = mount;
                    process.EnableRaisingEvents = true;
                    process.Exited += (_, _) => { _ = HandleExitedAsync(mount); };
                    process = null; client = null;
                    Publish(profile.Id, "Connected", "Đã khôi phục quản lý phiên ổ đĩa còn chạy.");
                }
                catch
                {
                    // Preserve unverified state for manual recovery. Never kill a process based only on a saved PID.
                    Publish(profile.Id, "Error", "Có phiên ổ đĩa cũ nhưng chưa xác minh được quyền điều khiển. Không tạo phiên trùng hoặc cưỡng bức ngắt; kiểm tra nhật ký và thử mở lại.");
                }
                finally { process?.Dispose(); client?.Dispose(); }
            }
        }
        finally { operationGate.Release(); }
    }

    public int MountTimeoutSeconds { get; set; } = 30;
    public string MountLogLevel { get; set; } = "INFO";
    public async Task ConnectAsync(DriveProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ObjectDisposedException.ThrowIf(disposed, this);
        UninstallSupport.EnsureNotUninstalling();
        await RecoverMountsAsync();
        await operationGate.WaitAsync();
        Mount? mount = null;
        try
        {
            ValidateProfile(profile);
            if (IsConnected(profile.Id)) return;
            if (mounts.TryGetValue(profile.Id, out var stale) && !IsRunning(stale) && RemoveMount(stale))
            {
                stale.Client.Dispose();
                stale.Process.Dispose();
            }
            if (File.Exists(SessionFile(profile.Id))) throw new IOException("Còn thông tin phiên cũ chưa xác minh. Không tạo kết nối trùng; kiểm tra hoặc phục hồi phiên trước.");
            await TestProfileAsync(profile);
            if (!HasRclone) throw new FileNotFoundException("Thiếu thành phần kết nối. Chạy lại bộ cài EasyMount.");
            if (!HasWinFsp) throw new InvalidOperationException("Cần cài WinFsp để tạo ổ đĩa Windows. Bấm Tải WinFsp trên màn hình chính để mở trang chính thức, cài xong rồi mở lại EasyMount.");
            if (DriveInfo.GetDrives().Any(drive => drive.Name.StartsWith(profile.Letter, StringComparison.OrdinalIgnoreCase)))
                throw new IOException($"Ổ {profile.Letter} đang được sử dụng. Hãy chọn ký tự khác.");
            if (mounts.Values.Any(m => m.Letter.Equals(profile.Letter, StringComparison.OrdinalIgnoreCase)))
                throw new IOException($"EasyMount đã sử dụng ổ {profile.Letter}.");

            Publish(profile.Id, "Connecting");
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
            var protectedSecret = MountSession.Protect(secret);
            var clean = Path.Combine(DataDirectory, "cache", profile.Id + ".clean");
            if (File.Exists(clean)) File.Delete(clean);
            var args = new List<string>
            {
                "mount", MountRemote(profile), profile.Letter,
                "--network-mode", "--volname", await NetworkVolumeAsync(profile),
                "--vfs-cache-mode", "writes", "--vfs-write-back", "5s",
                "--cache-dir", Path.Combine(DataDirectory, "cache", profile.Id),
                "--log-file", Path.Combine(DataDirectory, "logs", profile.Id + ".log"),
                "--log-level", MountLogLevel is "DEBUG" or "NOTICE" or "ERROR" ? MountLogLevel : "INFO", "--rc", "--rc-no-auth=false", "--rc-addr", "127.0.0.1:" + port
            };
            if (profile.ReadOnly) args.Add("--read-only");
            var process = new Process { StartInfo = StartInfo(args), EnableRaisingEvents = true };
            foreach (var pair in PoolEnvironment(profile)) process.StartInfo.Environment[pair.Key] = pair.Value;
            process.StartInfo.Environment["RCLONE_RC_USER"] = "easymount";
            process.StartInfo.Environment["RCLONE_RC_PASS"] = secret;
            var client = Controller(port, secret);
            mount = new Mount(process, client, profile.Id, profile.Letter, profile.ReadOnly);
            process.Start();
            mount.Started = true;
            mounts[profile.Id] = mount;
            MountSession.Save(SessionFile(profile.Id), new MountSession(process.Id, process.StartTime.ToUniversalTime().Ticks, RclonePath, profile.Id, profile.Letter, profile.ReadOnly, RoutingIdentity(profile), port, protectedSecret));
            _ = ReadBoundedAsync(process.StandardOutput, 0);
            var startupError = ReadBoundedAsync(process.StandardError);
            process.Exited += (sender, args) => { _ = HandleExitedAsync(mount); };
            var deadline = DateTime.UtcNow.AddSeconds(Math.Clamp(MountTimeoutSeconds, 10, 60));
            while (DateTime.UtcNow < deadline)
            {
                if (process.HasExited)
                    throw new InvalidOperationException("Không tạo được ổ đĩa: " + FriendlyError(await startupError));
                if (Directory.Exists(profile.Letter + "\\"))
                {
                    mount.NetworkPath = WindowsDrive.Connection(profile.Letter);
                    WindowsDrive.Added(profile.Letter);
                    Publish(profile.Id, "Connected");
                    return;
                }
                await Task.Delay(250);
            }
            throw new TimeoutException($"Ổ đĩa chưa sẵn sàng sau {Math.Clamp(MountTimeoutSeconds, 10, 60)} giây. Kiểm tra WinFsp, tài khoản và nhật ký.");
        }
        catch (Exception ex)
        {
            if (mount is not null)
            {
                RemoveMount(mount);
                if (mount.Started && !mount.Process.HasExited && Directory.Exists(profile.Letter + "\\"))
                {
                    mounts[profile.Id] = mount;
                    Publish(profile.Id, "Connected");
                    return; // The filesystem became ready at the timeout boundary; retain its owner.
                }
                if (mount.Started && !mount.Process.HasExited)
                {
                    // Failed startup is not exposed as connected. Prefer graceful quit before cleanup.
                    try { using var result = await RcAsync(mount, "core/quit"); } catch (Exception) { }
                    if (!mount.Process.WaitForExit(3000) && !Directory.Exists(profile.Letter + "\\")) mount.Process.Kill(entireProcessTree: true);
                }
                if (!mount.Started || mount.Process.HasExited) { mount.Client.Dispose(); mount.Process.Dispose(); }
                else mounts[profile.Id] = mount; // Never kill a writable filesystem which may now be in use.
            }
            Publish(profile.Id, "Error", ex.Message);
            throw;
        }
        finally { operationGate.Release(); }
    }

    private static async Task<JsonDocument> RcAsync(Mount mount, string command, string payload = "{}")
    {
        using var response = await mount.Client.PostAsync(command, new StringContent(payload, Encoding.UTF8, "application/json"));
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new IOException($"Không liên lạc được bộ điều khiển ổ đĩa ({(int)response.StatusCode}). Giữ ứng dụng chạy và thử lại.");
        return JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
    }

    public async Task<DriveCapacity?> GetCapacityAsync(DriveProfile profile)
    {
        await operationGate.WaitAsync();
        try
        {
            if (!mounts.TryGetValue(profile.Id, out var mount) || !IsRunning(mount)) return null;
            using var result = await RcAsync(mount, "operations/about", JsonSerializer.Serialize(new { fs = MountRemote(profile) }));
            return ParseCapacity(result.RootElement);
        }
        finally { operationGate.Release(); }
    }

    internal static DriveCapacity? ParseCapacity(JsonElement root)
    {
        if (!root.TryGetProperty("total", out var totalValue) || !totalValue.TryGetInt64(out var total) || total <= 0) return null;
        long used;
        if (root.TryGetProperty("used", out var usedValue) && usedValue.TryGetInt64(out used)) { }
        else if (root.TryGetProperty("free", out var freeValue) && freeValue.TryGetInt64(out var free) && free >= 0 && free <= total) used = total - free;
        else return null;
        return used >= 0 && used <= total ? new DriveCapacity(used, total, DateTime.Now) : null;
    }

    public async Task DisconnectAsync(string profileId)
    {
        await operationGate.WaitAsync();
        try
        {
            if (!mounts.TryGetValue(profileId, out var mount)) { Publish(profileId, "Disconnected"); return; }
            if (mount.Process.HasExited)
            {
                WindowsDrive.Removed(mount.Letter, mount.NetworkPath);
                RemoveMount(mount);
                DeleteSession(profileId);
                mount.Client.Dispose();
                mount.Process.Dispose();
                Publish(profileId, "Disconnected");
                return;
            }
            Publish(profileId, "Disconnecting");
            try
            {
                // Check even a read-only mount: an older writable session may have left cached uploads.
                {
                    using var stats = await RcAsync(mount, "vfs/stats");
                    if (!stats.RootElement.TryGetProperty("diskCache", out var cache))
                        throw new IOException("Không xác minh được trạng thái tải lên. Giữ ổ kết nối và thử lại sau.");
                    foreach (var key in new[] { "uploadsInProgress", "uploadsQueued", "erroredFiles" })
                    {
                        if (!cache.TryGetProperty(key, out var value) || !value.TryGetInt64(out var count))
                            throw new IOException("Không xác minh được hàng đợi tải lên. Giữ ổ kết nối và thử lại sau.");
                        if (count > 0)
                            throw new IOException("Còn tệp đang chờ tải lên hoặc gặp lỗi. Đóng các tệp đang mở, giữ EasyMount chạy và thử ngắt lại sau khi tải lên xong.");
                    }
                    // rclone's "inUse" is a VFS reference count, NOT an open-file count.
                    // Queue checks cannot prevent a different application starting a new write concurrently.
                }
                RemoveMount(mount); // Expected process exit must not be reported as a crash.
                try { using var result = await RcAsync(mount, "core/quit"); }
                catch (HttpRequestException) { /* core/quit may close its HTTP connection before returning. */ }
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await mount.Process.WaitForExitAsync(timeout.Token);
                while (DriveInfo.GetDrives().Any(d => d.Name.StartsWith(mount.Letter, StringComparison.OrdinalIgnoreCase)))
                {
                    if (timeout.IsCancellationRequested) throw new IOException("Tiến trình đã dừng nhưng Windows chưa tháo ổ. Giữ cache và kiểm tra trước khi kết nối lại.");
                    await Task.Delay(200);
                }
                WindowsDrive.Removed(mount.Letter, mount.NetworkPath);
                var profile = LoadProfiles().FirstOrDefault(p => p.Id == profileId);
                if (profile is not null)
                {
                    Directory.CreateDirectory(Path.Combine(DataDirectory, "cache"));
                    File.WriteAllText(Path.Combine(DataDirectory, "cache", profileId + ".clean"), RoutingIdentity(profile));
                }
                DeleteSession(profileId);
                mount.Client.Dispose();
                mount.Process.Dispose();
                Publish(profileId, "Disconnected");
            }
            catch (Exception ex)
            {
                var running = IsRunning(mount);
                if (running) mounts[profileId] = mount;
                else
                {
                    RemoveMount(mount);
                    mount.Client.Dispose();
                    mount.Process.Dispose();
                }
                Publish(profileId, running ? "Connected" : "Disconnected", ex.Message);
                throw;
            }
        }
        finally { operationGate.Release(); }
    }

    public async Task PrepareUninstallAsync()
    {
        var sessions = Path.Combine(DataDirectory, "sessions");
        if (Directory.Exists(sessions) && Directory.EnumerateFiles(sessions, "*.json").Any())
        {
            await RecoverMountsAsync();
            foreach (var file in Directory.EnumerateFiles(sessions, "*.json"))
                if (!mounts.ContainsKey(Path.GetFileNameWithoutExtension(file)))
                    throw new IOException("Có phiên ổ đĩa cũ chưa xác minh được; giữ ứng dụng để phục hồi trước khi gỡ.");
        }
        // Check all recovered VFS instances, including read-only mounts with older cached writes.
        foreach (var mount in mounts.Values)
        {
            if (!IsRunning(mount)) continue;
            using var stats = await RcAsync(mount, "vfs/stats");
            var cache = stats.RootElement.GetProperty("diskCache");
            foreach (var key in new[] { "uploadsInProgress", "uploadsQueued", "erroredFiles" })
                if (!cache.TryGetProperty(key, out var value) || !value.TryGetInt64(out var count) || count != 0)
                    throw new IOException("Còn file đang tải lên, chờ tải hoặc gặp lỗi. Chờ upload xong rồi gỡ lại; ứng dụng và cache được giữ nguyên.");
        }
        await DisconnectAllAsync();
    }

    public async Task DisconnectAllAsync()
    {
        // Waiting for this gate also waits for an in-flight ConnectAsync before taking the snapshot.
        await operationGate.WaitAsync();
        string[] ids;
        try { ids = mounts.Keys.ToArray(); }
        finally { operationGate.Release(); }
        foreach (var id in ids) await DisconnectAsync(id);
    }

    public void OpenLogs() => Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true, ArgumentList = { Path.Combine(DataDirectory, "logs") } });

    public async Task<string> NetworkVolumeAsync(DriveProfile profile)
    {
        var accounts = await ListRemotesAsync();
        var type = profile.IsPool ? "pool" : accounts.FirstOrDefault(a => a.Name == profile.Remote.Split(':')[0])?.Type ?? "cloud";
        var host = type switch { "drive" => "ggdrive", "onedrive" => "onedrive", "dropbox" => "dropbox", "pool" => "pool", _ => "easymount" };
        var share = Regex.Replace(profile.Name, @"[\\/:*?""<>|]", "_").Trim().TrimEnd('.');
        if (share.Length == 0) share = "EasyMount";
        return @"\\" + host + "\\" + share;
    }

    public void Dispose()
    {
        disposed = true;
        // The window must await DisconnectAllAsync before Dispose. Never force-kill pending cloud writes here.
        foreach (var pair in mounts.ToArray())
            if (mounts.TryRemove(pair.Key, out var mount)) { mount.Client.Dispose(); mount.Process.Dispose(); }
    }
}
