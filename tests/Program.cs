using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using EasyMount;

var appRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
var rclone = args.Length > 0 ? Path.GetFullPath(args[0]) : throw new ArgumentException("Pass the internal build engine path.");
if (!File.Exists(rclone)) throw new FileNotFoundException("Pass the bundled rclone.exe path as the first argument.", rclone);

var temporaryRoot = Path.Combine(Path.GetTempPath(), "EasyMount-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temporaryRoot);
int passed = 0;
void Check(bool value, string description)
{
    if (!value) throw new Exception("FAILED: " + description);
    Console.WriteLine("PASS " + description);
    passed++;
}
void Reject(Action action, Type expected, string description)
{
    try { action(); }
    catch (Exception ex) when (expected.IsInstanceOfType(ex)) { Check(true, description); return; }
    throw new Exception("FAILED: " + description + " did not reject input.");
}
async Task RejectAsync(Func<Task> action, Type expected, string description)
{
    try { await action(); }
    catch (Exception ex) when (expected.IsInstanceOfType(ex)) { Check(true, description); return; }
    throw new Exception("FAILED: " + description + " did not reject input.");
}
DriveProfile Profile(string name = "Tài liệu học tập", string letter = "R:") => new()
{
    Name = name, Letter = letter, Remote = "fixture account:folder with spaces", AutoConnect = true
};

Process? server = null;
try
{
    using var service = new DriveService(Path.Combine(temporaryRoot, "settings with spaces"), rclone);
    Check(service.HasRclone, "bundled engine is available");
    Check(service.LoadProfiles().Count == 0, "first launch has no fabricated drive profiles");
    var profile = Profile();
    Check(profile.ReadOnly, "new drives default to read-only");
    DriveService.ValidateProfile(profile);
    Check(true, "Unicode display names and remote paths with spaces are valid");
    service.SaveProfiles(new[] { profile });
    var profilesPath = Path.Combine(service.DataDirectory, "drives.json");
    var original = File.ReadAllBytes(profilesPath);
    var restored = service.LoadProfiles().Single();
    Check(restored.Id == profile.Id && restored.Name == profile.Name && restored.Remote == profile.Remote && restored.AutoConnect && restored.ReadOnly,
        "saved profile roundtrips all user configuration");
    Reject(() => service.SaveProfiles(new[] { profile, Profile("Duplicate letter") }), typeof(ArgumentException), "duplicate drive letter is rejected");
    Check(File.ReadAllBytes(profilesPath).SequenceEqual(original), "failed save preserves the previous configuration bytes");
    var duplicate = Profile("Duplicate identity", "S:"); duplicate.Id = profile.Id;
    Reject(() => service.SaveProfiles(new[] { profile, duplicate }), typeof(ArgumentException), "duplicate profile identity is rejected");
    var invalid = Profile(); invalid.Remote = "--config=untrusted";
    Reject(() => DriveService.ValidateProfile(invalid), typeof(ArgumentException), "option-shaped remote injection is rejected");
    invalid = Profile(); invalid.Letter = "C:";
    Reject(() => DriveService.ValidateProfile(invalid), typeof(ArgumentException), "system drive cannot be selected");
    invalid = Profile(); invalid.Name = "name\nwith control";
    Reject(() => DriveService.ValidateProfile(invalid), typeof(ArgumentException), "control characters are rejected");
    File.WriteAllText(profilesPath, "{broken-json");
    Reject(() => service.LoadProfiles(), typeof(InvalidDataException), "corrupt JSON yields a recoverable configuration error");
    Check(File.ReadAllText(profilesPath) == "{broken-json", "reading corrupt JSON never rewrites the original file");
    File.WriteAllText(profilesPath, "[null]");
    Reject(() => service.LoadProfiles(), typeof(InvalidDataException), "null profile records yield a recoverable configuration error");
    File.WriteAllBytes(profilesPath, original);
    Check(!Directory.GetFiles(service.DataDirectory, "*.tmp").Any(), "atomic saves leave no temporary profile files");

    var pool = new DriveProfile { Name = "Kho tổng hợp", Letter = "T:", ReadOnly = false,
        Sources = new() { new() { Remote = "fixture account:folder with spaces", LimitBytes = 1024 }, new() { Remote = "second account:", LimitBytes = 2048 } } };
    service.SaveProfiles(new[] { profile, pool });
    var savedPool = service.LoadProfiles().Single(p => p.IsPool);
    Check(savedPool.Sources.Select(s => s.Remote).SequenceEqual(pool.Sources.Select(s => s.Remote)) && savedPool.Sources[1].LimitBytes == 2048,
        "pool persists ordered sources and per-folder capacities");
    savedPool.Name = "Tên ổ mới";
    service.SaveProfiles(new[] { profile, savedPool });
    Check(service.LoadProfiles().Single(p => p.IsPool).Name == "Tên ổ mới", "pool display name can be changed without changing routing");
    var env = service.PoolEnvironment(pool);
    var plan = System.Text.Json.JsonSerializer.Deserialize<List<PoolSource>>(File.ReadAllText(env.Single(e => e.Key.EndsWith("_EASYMOUNT_PLAN")).Value))!;
    Check(DriveService.MountRemote(pool) == "em_pool_" + pool.Id + ":" && plan[0].Remote == "fixture account:folder with spaces" && plan[1].LimitBytes == 2048 && env.Count == 2,
        "pool engine receives a credential-free plan preserving priority and capacity settings");
    pool.Sources.Reverse();
    env = service.PoolEnvironment(pool);
    plan = System.Text.Json.JsonSerializer.Deserialize<List<PoolSource>>(File.ReadAllText(env.Single(e => e.Key.EndsWith("_EASYMOUNT_PLAN")).Value))!;
    Check(plan[0].LimitBytes == 2048 && plan[1].LimitBytes == 1024, "priority changes reorder both routes and their capacity limits");
    var largePool = new DriveProfile { Name = "Many accounts", Letter = "U:", Sources = Enumerable.Range(0, 1000).Select(i => new PoolSource { Remote = "account-" + i + ":" }).ToList() };
    DriveService.ValidateProfile(largePool);
    Check(File.ReadAllText(service.PoolEnvironment(largePool).Single(e => e.Key.EndsWith("_EASYMOUNT_PLAN")).Value).Contains("account-999:"), "1000 configured pool accounts are accepted without an account-count cap");
    pool.Sources.Add(new() { Remote = pool.Sources[0].Remote });
    Reject(() => DriveService.ValidateProfile(pool), typeof(ArgumentException), "duplicate pool accounts are rejected");
    pool.Sources.RemoveAt(2);
    var oldSource = pool.Sources[0].Remote; pool.Sources[0].Remote = "second account:path:ro";
    Reject(() => DriveService.ValidateProfile(pool), typeof(ArgumentException), "pool paths cannot inject upstream read-only flags");
    pool.Sources[0].Remote = "second account:../outside";
    Reject(() => DriveService.ValidateProfile(pool), typeof(ArgumentException), "pool folder cannot escape through parent path segments");
    pool.Sources[0].Remote = oldSource;
    var cached = Path.Combine(service.DataDirectory, "cache", savedPool.Id);
    Directory.CreateDirectory(cached); File.WriteAllText(Path.Combine(cached, "pending.fixture"), "pending cache fixture");
    savedPool.Sources.Reverse();
    Reject(() => service.SaveProfiles(new[] { profile, savedPool }), typeof(IOException), "routing edits are blocked while cached files may need recovery");
    savedPool.Sources.Reverse(); savedPool.Name = "Đổi tên khi còn cache";
    service.SaveProfiles(new[] { profile, savedPool });
    Check(service.LoadProfiles().Single(p => p.IsPool).Name == savedPool.Name, "display-name edits preserve cached upload routing");

    var fixtureRoot = Path.Combine(temporaryRoot, "fixture files");
    Directory.CreateDirectory(Path.Combine(fixtureRoot, "folder with spaces"));
    File.WriteAllText(Path.Combine(fixtureRoot, "folder with spaces", "hello.txt"), "EasyMount local fixture; no user cloud data.");
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    var address = $"http://127.0.0.1:{port}/";
    File.WriteAllText(service.ConfigPath, $"[fixture account]\ntype = webdav\nurl = {address}\nvendor = other\n");
    var start = new ProcessStartInfo(rclone) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
    foreach (var argument in new[] { "--config", service.ConfigPath, "serve", "webdav", fixtureRoot, "--addr", $"127.0.0.1:{port}", "--read-only" })
        start.ArgumentList.Add(argument);
    server = Process.Start(start) ?? throw new Exception("Could not launch local WebDAV fixture.");
    var serverError = server.StandardError.ReadToEndAsync();
    var serverOutput = server.StandardOutput.ReadToEndAsync();
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
    var ready = false;
    for (var attempt = 0; attempt < 40; attempt++)
    {
        if (server.HasExited) throw new Exception("WebDAV fixture exited: " + await serverError);
        try { using var response = await http.GetAsync(address); ready = response.IsSuccessStatusCode; }
        catch (HttpRequestException) { }
        catch (TaskCanceledException) { }
        if (ready) break;
        await Task.Delay(100);
    }
    Check(ready, "real rclone loopback WebDAV server starts");
    var remotes = await service.ListRemotesAsync();
    Check(remotes.Count == 1 && remotes[0] == new RemoteAccount("fixture account", "webdav"), "real engine lists configured account without dumping tokens");
    service.SaveAccountLabel("fixture account", "tester@example.com");
    Check((await service.ListRemotesAsync())[0] is { Name: "fixture account", DisplayName: "tester@example.com" }, "email display name preserves the internal remote used by mounted profiles");
    service.SaveAccountLabel("fixture account", "Kho cá nhân");
    Check((await service.ListRemotesAsync())[0].DisplayName == "Kho cá nhân", "custom account name replaces the email and survives reload");
    Reject(() => service.SaveAccountLabel("fixture account", "bad\nname"), typeof(ArgumentException), "account labels reject control characters");
    await service.TestRemoteAsync(profile.Remote);
    Check(true, "real engine tests a WebDAV directory containing spaces");
    await RejectAsync(() => service.TestRemoteAsync("not-configured:"), typeof(InvalidOperationException), "unconfigured account surfaces a connection error");
    await RejectAsync(() => service.TestRemoteAsync("fixture account:does-not-exist"), typeof(InvalidOperationException), "missing remote folder surfaces a connection error");
    await RejectAsync(() => service.TestRemoteAsync("--config=untrusted"), typeof(ArgumentException), "connection testing rejects option injection");
    Reject(() => AccountSetupSession.ValidateName("em_pool_reserved"), typeof(ArgumentException), "account names cannot collide with generated pool names");
    Reject(() => AccountSetupSession.ValidateName("bad\nname"), typeof(ArgumentException), "account setup rejects invalid configuration section names");
    var oauthFile = Path.Combine(temporaryRoot, "oauth-clients.json");
    Check(AccountSetupSession.OAuthParameters(oauthFile, "drive").Count == 0, "missing app OAuth settings do not fabricate client credentials");
    File.WriteAllText(oauthFile, "{\"drive\":{\"client_id\":\"fixture-id\",\"client_secret\":\"fixture-secret\",\"ignored\":\"value\"}}");
    var appOAuth = AccountSetupSession.OAuthParameters(oauthFile, "drive");
    Check(appOAuth.Count == 2 && appOAuth["client_id"] == "fixture-id", "app-level OAuth settings load only the expected client fields");
    File.WriteAllText(oauthFile, "{invalid");
    Reject(() => AccountSetupSession.OAuthParameters(oauthFile, "drive"), typeof(System.Text.Json.JsonException), "invalid app OAuth JSON fails explicitly rather than using fabricated settings");
    await using (var setup = new AccountSetupSession(service))
    {
        var catalog = await setup.ProvidersAsync();
        Check(catalog.Any(p => p.Name == "drive") && catalog.Any(p => p.Name == "onedrive") && catalog.Any(p => p.Name == "webdav"), "native setup discovers the bundled provider catalog");
        using var unauthenticated = new HttpClient();
        using var denied = await unauthenticated.PostAsync(new Uri(setup.Address, "config/providers"), new StringContent("{}"));
        Check(denied.StatusCode == HttpStatusCode.Unauthorized, "configuration API rejects requests without its random session credentials");
        await RejectAsync(() => setup.BeginAsync("fixture account", "webdav", new(), true), typeof(ArgumentException), "existing accounts cannot be overwritten by the setup wizard");
        var question = await setup.BeginAsync("gui_fixture", "webdav", new(), true);
        var sawPassword = false;
        for (var i = 0; question.State != "" && i < 80; i++)
        {
            var option = question.Option ?? throw new Exception("Expected a native configuration question.");
            var value = option.Name switch { "url" => address, "vendor" => "other", "pass" => "fixture-password-no-user-data", "config_fs_advanced" => "false", _ => option.DefaultStr };
            sawPassword |= option.IsPassword;
            question = await setup.ContinueAsync(question, value);
        }
        Check(question.State == "" && sawPassword, "native forms complete the real WebDAV state machine, including a password field");
        var stored = File.ReadAllText(service.ConfigPath);
        Check(!stored.Contains("fixture-password-no-user-data"), "password answers are obscured before being stored by rclone");
        await setup.CompleteAsync();
    }
    Check((await service.ListRemotesAsync()).Any(r => r.Name == "gui_fixture"), "completed native account remains available after setup closes");
    service.SaveAccountLabel("gui_fixture", "Kho cá nhân");
    Check((await service.ListRemotesAsync()).Count(r => r.DisplayName == "Kho cá nhân") == 2, "matching display names never overwrite distinct authenticated accounts");
    await using (var cancelled = new AccountSetupSession(service))
    {
        await cancelled.ProvidersAsync();
        var question = await cancelled.BeginAsync("cancelled_fixture", "webdav", new(), true);
        Check(question.State != "", "incomplete setup remains a question instead of reporting success");
    }
    remotes = await service.ListRemotesAsync();
    Check(remotes.All(r => r.Name != "cancelled_fixture") && remotes.Any(r => r.Name == "fixture account") && remotes.Any(r => r.Name == "gui_fixture"), "cancelling removes only the newly created incomplete account");
    bool callbackAvailable;
    using (var callbackProbe = new TcpListener(IPAddress.Loopback, 53682))
    {
        try { callbackProbe.Start(); callbackAvailable = true; }
        catch (SocketException) { callbackAvailable = false; }
    }
    if (callbackAvailable)
    {
    Task<ConfigStep> oauthRequest;
    await using (var oauth = new AccountSetupSession(service))
    {
        await oauth.ProvidersAsync();
        oauthRequest = oauth.BeginAsync("oauth_fixture", "drive", new() { ["config_is_local"] = "true", ["config_auth_no_browser"] = "true", ["scope"] = "drive.readonly", ["client_id"] = "fixture-client-id", ["client_secret"] = "fixture-client-secret" }, false);
        Uri? browserUrl = null;
        for (var i = 0; i < 40 && browserUrl is null; i++)
        {
            if (oauthRequest.IsCompleted)
            {
                var response = await oauthRequest;
                if (response.Option?.Name == "config_is_local") oauthRequest = oauth.ContinueAsync(response, "true");
                else throw new Exception("Unexpected pre-login question: " + response.Option?.Name);
            }
            browserUrl = await oauth.BrowserUrlAsync();
            if (browserUrl is null) await Task.Delay(100);
        }
        Check(browserUrl is not null && browserUrl.IsLoopback && !oauthRequest.IsCompleted, "OAuth starts a local browser callback and reports progress without a console or real login");
    }
    await RejectAsync(async () => { await oauthRequest; }, typeof(InvalidOperationException), "closing setup cancels a pending OAuth callback");
    Check((await service.ListRemotesAsync()).All(r => r.Name != "oauth_fixture"), "cancelled OAuth does not leave an incomplete account in the configuration");
    }
    else Console.WriteLine("SKIP 3 OAuth callback checks: port 53682 is in use; an existing user's login is preserved.");
    if (!service.HasWinFsp)
    {
        var states = new List<DriveSnapshot>();
        service.StateChanged += state => states.Add(state);
        await RejectAsync(() => service.ConnectAsync(profile), typeof(InvalidOperationException), "missing WinFsp blocks mounting before a process or drive is created");
        Check(states.Any(s => s.ProfileId == profile.Id && s.Status == "Error" && s.Error!.Contains("WinFsp")) && !service.IsConnected(profile.Id),
            "mount prerequisite failure is visible and never marked connected");
    }
    else Console.WriteLine("SKIP missing-driver check: WinFsp is already installed; tests do not create host mounts.");
    await service.DisconnectAllAsync();
    Check(true, "disconnecting an idle service completes safely");
    Console.WriteLine($"RESULT: {passed} checks passed. True mounted-drive I/O and Google/Microsoft OAuth require WinFsp and a user account; not exercised here.");
}
finally
{
    if (server is not null)
    {
        if (!server.HasExited) server.Kill(entireProcessTree: true); // Disposable, read-only loopback test server only.
        await server.WaitForExitAsync();
        server.Dispose();
    }
    Directory.Delete(temporaryRoot, recursive: true);
}
