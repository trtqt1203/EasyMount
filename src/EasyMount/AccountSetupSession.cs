using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace EasyMount;

public sealed class ConfigChoice
{
    public string Value { get; set; } = "";
    public string Help { get; set; } = "";
    public override string ToString() => Value == "true" ? "Có" : Value == "false" ? "Không" : (string.IsNullOrWhiteSpace(Help) ? Value : Help.Split('\n')[0]);
}
public sealed class ConfigQuestion
{
    public string Name { get; set; } = "";
    public string Help { get; set; } = "";
    public string DefaultStr { get; set; } = "";
    public bool Required { get; set; }
    public bool Exclusive { get; set; }
    public bool IsPassword { get; set; }
    public List<ConfigChoice> Examples { get; set; } = new();
}
public sealed class ConfigStep
{
    public string State { get; set; } = "";
    public string Error { get; set; } = "";
    public ConfigQuestion? Option { get; set; }
}
public sealed record CloudProvider(string Name, string Description)
{
    public override string ToString() => Name switch { "drive" => "Google Drive", "onedrive" => "Microsoft OneDrive", "dropbox" => "Dropbox", "s3" => "Amazon S3 / S3 compatible", "webdav" => "WebDAV", "sftp" => "SFTP", _ => Description };
}

// One authenticated loopback server per dialog. No credentials on the command line,
// no interactive console, and cancellation only removes the newly created account.
public sealed class AccountSetupSession : IAsyncDisposable
{
    private readonly DriveService service;
    private readonly Process process;
    private readonly HttpClient client;
    private readonly List<string> secrets = new();
    private Task<ConfigStep>? pending;
    private string? createdName;
    private string? createdType;
    private Dictionary<string, string> initialParameters = new();
    private bool complete, disposed;
    private readonly JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
    public Uri Address => client.BaseAddress!;

    public AccountSetupSession(DriveService service)
    {
        this.service = service;
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start(); var port = ((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop();
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var info = new ProcessStartInfo(service.RclonePath) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true };
        foreach (var arg in new[] { "--config", service.ConfigPath, "--ask-password=false", "rcd", "--rc-addr", "127.0.0.1:" + port, "--rc-no-auth=false", "--log-level", "ERROR" }) info.ArgumentList.Add(arg);
        info.Environment["RCLONE_RC_USER"] = "easymount-setup";
        info.Environment["RCLONE_RC_PASS"] = secret;
        process = Process.Start(info) ?? throw new InvalidOperationException("Không mở được dịch vụ đăng nhập.");
        process.StandardInput.Close();
        _ = process.StandardOutput.BaseStream.CopyToAsync(System.IO.Stream.Null);
        _ = process.StandardError.BaseStream.CopyToAsync(System.IO.Stream.Null);
        client = new HttpClient(new HttpClientHandler { UseProxy = false }) { BaseAddress = new Uri($"http://127.0.0.1:{port}/"), Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("easymount-setup:" + secret)));
    }

    private async Task<JsonElement> CallAsync(string command, object body, CancellationToken cancellationToken = default)
    {
        using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(command, content, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Không hoàn tất được bước kết nối (HTTP {(int)response.StatusCode}). Kiểm tra thông tin hoặc thử lại.");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.Clone();
    }

    public async Task<IReadOnlyList<CloudProvider>> ProvidersAsync()
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            if (process.HasExited) throw new InvalidOperationException("Không khởi động được dịch vụ kết nối. Đóng cửa sổ cấu hình cũ và thử lại.");
            try
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var result = await CallAsync("config/providers", new { }, deadline.Token);
                return result.GetProperty("providers").EnumerateArray().Select(p => new CloudProvider(p.GetProperty("Name").GetString()!, p.GetProperty("Description").GetString()!)).ToList();
            }
            catch (HttpRequestException) { await Task.Delay(100); }
            catch (OperationCanceledException) { await Task.Delay(100); }
        }
        throw new TimeoutException("Dịch vụ kết nối chưa sẵn sàng. Hãy thử mở lại cửa sổ.");
    }

    public static void ValidateName(string name)
    {
        if (!Regex.IsMatch(name, @"^[\p{L}\p{N}][\p{L}\p{N} _-]{0,63}$") || name.StartsWith("em_pool_", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Tên cần 1–64 chữ, số, khoảng trắng, dấu - hoặc _. Không dùng tiền tố em_pool_.");
    }

    public static Dictionary<string, string> OAuthParameters(string file, string provider)
    {
        if (!System.IO.File.Exists(file)) return new();
        using var json = JsonDocument.Parse(System.IO.File.ReadAllText(file));
        var values = new Dictionary<string, string>();
        if (json.RootElement.TryGetProperty(provider, out var config))
            foreach (var key in new[] { "client_id", "client_secret" })
                if (config.TryGetProperty(key, out var item) && item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString())) values[key] = item.GetString()!;
        return values;
    }

    public async Task<ConfigStep> BeginAsync(string name, string type, Dictionary<string, string> parameters, bool all)
    {
        ValidateName(name);
        if (createdName is not null) throw new InvalidOperationException("Phiên này đã bắt đầu. Đóng cửa sổ rồi thêm tài khoản mới để thử lại.");
        var remotes = await CallAsync("config/listremotes", new { });
        if (remotes.GetProperty("remotes").EnumerateArray().Any(n => name.Equals(n.GetString(), StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Tên này đã được sử dụng. Chọn tên khác để giữ tài khoản hiện có.");
        createdName = name; createdType = type;
        initialParameters = new Dictionary<string, string>(parameters);
        foreach (var pair in parameters)
            if (Regex.IsMatch(pair.Key, "token|secret|password", RegexOptions.IgnoreCase) && !string.IsNullOrEmpty(pair.Value)) secrets.Add(pair.Value);
        pending = StepAsync(new { name, type, parameters, opt = new { nonInteractive = true, all, obscure = true } });
        return await pending;
    }

    public async Task<ConfigStep> ContinueAsync(ConfigStep step, string answer)
    {
        if (createdName is null) throw new InvalidOperationException("Chưa bắt đầu cấu hình tài khoản.");
        if (step.Option?.IsPassword == true || Regex.IsMatch(step.Option?.Name ?? "", "token|secret|password", RegexOptions.IgnoreCase))
            if (!string.IsNullOrEmpty(answer)) secrets.Add(answer);
        if (step.Option?.IsPassword == true && step.State.StartsWith("*all-set", StringComparison.Ordinal) && answer != "")
        {
            var obscured = await CallAsync("core/obscure", new { clear = answer });
            answer = obscured.GetProperty("obscured").GetString()!;
        }
        // Use config/create with continue so the provider state machine does not
        // switch to edit mode or erase an in-progress account on each answer.
        pending = StepAsync(new { name = createdName, type = createdType, parameters = initialParameters, opt = new { nonInteractive = true, @continue = true, state = step.State, result = answer } });
        return await pending;
    }

    private async Task<ConfigStep> StepAsync(object input)
    {
        var json = await CallAsync("config/create", input);
        var step = json.Deserialize<ConfigStep>(jsonOptions) ?? throw new InvalidOperationException("Dịch vụ trả về cấu hình không hợp lệ.");
        foreach (var secret in secrets) step.Error = step.Error.Replace(secret, "••••");
        step.Error = Regex.Replace(step.Error, @"(?i)(access_token|refresh_token|client_secret|password)([=: ]+)[^\s&]+", "$1$2[ẩn]");
        return step;
    }

    public async Task<Uri?> BrowserUrlAsync()
    {
        var json = await CallAsync("config/oauthstatus", new { });
        if (json.TryGetProperty("authUrl", out var value) && Uri.TryCreate(value.GetString(), UriKind.Absolute, out var uri) && uri.IsLoopback && uri.Scheme == "http") return uri;
        return null;
    }

    public async Task<string?> AccountEmailAsync()
    {
        if (!complete || createdName is null || createdType is not ("drive" or "onedrive" or "dropbox")) return null;
        try
        {
            var config = await CallAsync("config/get", new { name = createdName });
            using var token = JsonDocument.Parse(config.GetProperty("token").GetString()!);
            using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.RootElement.GetProperty("access_token").GetString());
            using var request = createdType switch
            {
                "onedrive" => new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me?$select=mail,userPrincipalName"),
                "dropbox" => new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/2/users/get_current_account"),
                _ => new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/drive/v3/about?fields=user(emailAddress)")
            };
            using var response = await http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var info = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = info.RootElement;
            return createdType switch
            {
                "onedrive" => root.TryGetProperty("mail", out var mail) && mail.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(mail.GetString()) ? mail.GetString() : root.GetProperty("userPrincipalName").GetString(),
                "dropbox" => root.GetProperty("email").GetString(),
                _ => root.GetProperty("user").GetProperty("emailAddress").GetString()
            };
        }
        catch { return null; } // Providers may omit identity; the user can enter a label.
    }

    public async Task CompleteAsync()
    {
        if (createdName is null || pending is null || !(await pending).State.Equals("", StringComparison.Ordinal)) throw new InvalidOperationException("Tài khoản chưa hoàn tất xác thực.");
        await service.TestRemoteAsync(createdName + ":");
        complete = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        if (!process.HasExited)
        {
            try { await CallAsync("config/oauthstop", new { }).WaitAsync(TimeSpan.FromSeconds(2)); } catch { }
            if (pending is not null) { try { await pending.WaitAsync(TimeSpan.FromSeconds(5)); } catch { } }
            if (!complete && createdName is not null && (pending is null || pending.IsCompleted))
            {
                try { await CallAsync("config/delete", new { name = createdName }).WaitAsync(TimeSpan.FromSeconds(3)); }
                catch { /* Never modify unrelated account sections to recover a failed setup. */ }
            }
            // This process only configures accounts; it owns no mounted drive or uploads.
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
        secrets.Clear(); client.Dispose(); process.Dispose();
    }
}
