using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using EasyMount;

var appRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
var rclone = args.Length > 0 ? Path.GetFullPath(args[0]) : throw new ArgumentException("Pass the internal build engine path.");
if (!File.Exists(rclone)) throw new FileNotFoundException("Pass the bundled rclone.exe path as the first argument.", rclone);
await ReleaseClientChecks.RunAsync(args.Contains("--api-test"));

var temporaryRoot = Path.Combine(Path.GetTempPath(), "EasyMount-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temporaryRoot);
int passed = 0;
void Check(bool value, string description)
{
    if (!value) throw new Exception("FAILED: " + description);
    Console.WriteLine("PASS " + description);
    passed++;
}
var closeSettingsPath = Path.Combine(temporaryRoot, "close-settings.json");
Check(!DesktopSettings.Load(closeSettingsPath).KeepMountedOnClose, "new settings close and unmount instead of silently running in the tray");
File.WriteAllText(closeSettingsPath, "{\"Background\":true,\"Language\":\"vi\",\"Theme\":\"light\",\"AutoConnect\":false}");
var closeSettings = DesktopSettings.Load(closeSettingsPath);
Check(!closeSettings.KeepMountedOnClose && closeSettings.Language == "vi" && closeSettings.Theme == "light" && !closeSettings.AutoConnect,
    "legacy background default does not opt users into keeping mounts; unrelated preferences survive");
closeSettings.KeepMountedOnClose = true;
closeSettings.Save(closeSettingsPath);
Check(DesktopSettings.Load(closeSettingsPath).KeepMountedOnClose, "keeping mounts on close requires a persisted explicit opt-in");
foreach (var sample in new[] {
    (Json: "{\"mail\":\"user@example.com\"}", Email: "user@example.com"),
    (Json: "{\"mail\":null,\"userPrincipalName\":\"member@example.com\"}", Email: "member@example.com"),
    (Json: "{\"owner\":{\"user\":{\"email\":\"owner@example.com\"}}}", Email: "owner@example.com"),
    (Json: "{\"owner\":{\"user\":{\"email\":\"not an email\"}}}", Email: (string?)null),
    (Json: "{}", Email: (string?)null)
})
{
    using var identity = System.Text.Json.JsonDocument.Parse(sample.Json);
    Check(AccountSetupSession.ParseOneDriveEmail(identity.RootElement) == sample.Email, "OneDrive identity parser handles profile/drive fallback without inventing an email");
}
Check(DriveProfile.ResolveName("  Tài liệu  ", Array.Empty<DriveProfile>()) == "Tài liệu", "custom drive name is preserved and trimmed");
Check(DriveProfile.ResolveName(" ", new[] { new DriveProfile { Name="drive1" }, new DriveProfile { Name="Drive3" } }) == "Drive2", "blank drive names use first available Drive number without case collisions");
var renamed = new DriveProfile {Name="Drive1"};
Check(DriveProfile.ResolveName("",new[]{renamed},renamed.Id)=="Drive1", "blank rename excludes its own previous default name");
var childA = new DriveProfile{Name="Ảnh",Remote="alpha:"};
var childB = new DriveProfile{Name="Phim",Remote="beta:"};
var testPool = new DriveProfile{Name="Drive chung",Sources=new(){new(){Remote="alpha:"},new(){Remote="beta:"}}};
Check(DrivePresentation.ChildNames(testPool,new[]{childA,childB},Array.Empty<RemoteAccount>()).SequenceEqual(new[]{"Ảnh","Phim"}),"pooled subtitle uses user drive names in source order");
var capA=new DriveCapacity(10,100,DateTime.Now);var capB=new DriveCapacity(20,200,DateTime.Now);var capPool=new DriveCapacity(30,300,DateTime.Now);
Check(DrivePresentation.WorkspaceCapacity(new[]{(childA,(DriveCapacity?)capA),(childB,capB),(testPool,capPool)}) is {UsedBytes:30,TotalBytes:300},"workspace storage excludes duplicate pool and member quotas");
Check(DrivePresentation.WorkspaceCapacity(new[]{(childA,(DriveCapacity?)capA),(childB,null),(testPool,capPool)}) is {UsedBytes:30,TotalBytes:300},"pool capacity can cover a member with no separate quota");
Check(DrivePresentation.WorkspaceCapacity(new[]{(childA,(DriveCapacity?)capA),(childB,null)}) is null,"missing source quota is unknown rather than a fabricated workspace total");
var workspaceFile=Path.Combine(temporaryRoot,"workspaces.json");
var appearanceFile=Path.Combine(temporaryRoot,"appearance-settings.json");
var appearance=DesktopSettings.Load(appearanceFile);
Check(appearance.Language=="en" && appearance.Theme=="dark","new app preferences default to English and dark appearance");
File.WriteAllText(appearanceFile,"{\"AutoConnect\":false,\"RetrySeconds\":300}");
appearance=DesktopSettings.Load(appearanceFile);
Check(appearance.Language=="en" && appearance.Theme=="dark" && !appearance.AutoConnect && appearance.RetrySeconds==300,"older preferences gain appearance defaults while retaining connection choices");
appearance.Language="vi";appearance.Theme="light";appearance.Save(appearanceFile);
appearance=DesktopSettings.Load(appearanceFile);
Check(appearance.Language=="vi" && appearance.Theme=="light" && !appearance.AutoConnect && appearance.RetrySeconds==300,"language and light theme survive restart without changing connection preferences");
var workspaceStore=WorkspaceStore.Load(workspaceFile);
Check(workspaceStore.Contains("existing-drive") && workspaceStore.Items.Count==0,"fresh install has no pre-created workspaces and all existing drives remain visible");
var addedWorkspace=workspaceStore.Create("Tài liệu thử");
Check(!workspaceStore.Contains("existing-drive"),"new workspace starts empty without affecting existing drive");
workspaceStore.Assignments["existing-drive"]=addedWorkspace.Id;workspaceStore.Save(workspaceFile);
var workspaceReload=WorkspaceStore.Load(workspaceFile);
Check(workspaceReload.Active==addedWorkspace.Id && workspaceReload.Contains("existing-drive"),"workspace assignment and active workspace survive restart");
workspaceReload.Active="";Check(workspaceReload.Contains("existing-drive"),"all-drives view includes user workspace drives without moving files");
var otherWorkspace=workspaceReload.Create("Keep me");
workspaceReload.Assignments["other-drive"]=otherWorkspace.Id;
workspaceReload.Active=addedWorkspace.Id;workspaceReload.Delete(addedWorkspace.Id);
Check(workspaceReload.Active=="" && workspaceReload.Items.Count==1 && workspaceReload.Items[0].Id==otherWorkspace.Id,"deleting active workspace returns to all drives and preserves other workspaces");
Check(!workspaceReload.Assignments.ContainsKey("existing-drive") && workspaceReload.Assignments["other-drive"]==otherWorkspace.Id && workspaceReload.Contains("existing-drive"),"workspace deletion unassigns only its members and keeps those drives visible");
workspaceReload.Delete("missing");workspaceReload.Delete("");Check(workspaceReload.Items.Count==1,"deleting missing or virtual workspace is a no-op");
workspaceReload.Active=otherWorkspace.Id;workspaceReload.Delete(otherWorkspace.Id);workspaceReload.Save(workspaceFile);
var emptyReload=WorkspaceStore.Load(workspaceFile);
Check(emptyReload.Items.Count==0 && emptyReload.Assignments.Count==0 && emptyReload.Active=="" && emptyReload.Contains("existing-drive"),"deleting final workspace persists a truly empty workspace list while retaining all-drives access");
try{workspaceStore.Create("  ");Check(false,"blank workspace rejected");}catch(ArgumentException){Check(true,"blank workspace rejected");}
File.WriteAllText(workspaceFile,"{\"Items\":[{\"Id\":\"dup\",\"Name\":\"A\"},{\"Id\":\"dup\",\"Name\":\"B\"}]}");try{WorkspaceStore.Load(workspaceFile);Check(false,"corrupt workspace rejected");}catch(IOException){Check(true,"corrupt workspace rejected without overwriting it");}
File.WriteAllText(workspaceFile,"{\"Items\":[{\"Id\":\"default\",\"Name\":\"Edit ảnh/video\"},{\"Id\":\"films\",\"Name\":\"Lưu trữ phim\"},{\"Id\":\"custom123\",\"Name\":\"Lưu trữ phim\"}],\"Active\":\"default\",\"Assignments\":{}}");
var migrated=WorkspaceStore.Load(workspaceFile);
Check(migrated.Items.Count==1 && migrated.Items[0].Id=="custom123" && migrated.Active=="" && File.Exists(workspaceFile+".before-v063"),"migration removes only unused legacy seeds, preserves user-created names and backs up old settings");
File.WriteAllText(workspaceFile,"{\"Items\":[{\"Id\":\"films\",\"Name\":\"Lưu trữ phim\"}],\"Active\":\"films\",\"Assignments\":{\"drive1\":\"films\"}}");
Check(WorkspaceStore.Load(workspaceFile).Contains("drive1"),"migration preserves a legacy group explicitly used by the user");
var optionsFile=Path.Combine(temporaryRoot,"desktop-settings.json");var options=new DesktopSettings{Retry=false,TimeoutSeconds=60,CapacityThreshold=95};options.Save(optionsFile);var reloadedOptions=DesktopSettings.Load(optionsFile);
Check(!reloadedOptions.Retry && reloadedOptions.TimeoutSeconds==60 && reloadedOptions.CapacityThreshold==95,"desktop settings persist independent of drive credentials");
foreach (var (json, used, total) in new[] {
    ("{\"total\":1000,\"used\":700,\"free\":300}", 700L, 1000L),
    ("{\"total\":1000,\"free\":350}", 650L, 1000L) })
{
    using var capacityJson = System.Text.Json.JsonDocument.Parse(json);
    var capacity = DriveService.ParseCapacity(capacityJson.RootElement);
    Check(capacity?.UsedBytes == used && capacity.TotalBytes == total, "capacity metadata reports provider values with free-space fallback");
}
foreach (var json in new[] { "{}", "{\"total\":0,\"used\":0}", "{\"total\":100,\"used\":-1}", "{\"total\":100,\"used\":101}", "{\"total\":100,\"free\":101}" })
{
    using var capacityJson = System.Text.Json.JsonDocument.Parse(json);
    Check(DriveService.ParseCapacity(capacityJson.RootElement) is null, "missing or inconsistent capacity stays unknown instead of fabricating usage");
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
    var transitionCalls = 0;
    var retryStep = await AccountSetupSession.FollowTransitionsAsync(new ConfigStep { State = "choose_type", Error = "Failed to query root: fixture error" }, step =>
    {
        transitionCalls++;
        return Task.FromResult(new ConfigStep { State = "choose_type_done", Option = new ConfigQuestion { Name = "config_type", Exclusive = true } });
    });
    Check(transitionCalls == 1 && retryStep.Option?.Name == "config_type" && retryStep.Error.Contains("fixture error"), "OneDrive error-only transition resumes a usable question and retains its cause");
    await RejectAsync(async () => await AccountSetupSession.FollowTransitionsAsync(new ConfigStep { State = "loop" }, step => Task.FromResult(step)), typeof(InvalidOperationException), "invalid provider transition loops stop instead of hanging the wizard");
    Check(!profile.ReadOnly, "new drives default to read and write");
    profile.ReadOnly = true; // An explicitly selected read-only mode must survive saving.
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
    if (service.HasWinFsp && args.Contains("--mounts"))
    {
        var mountRoot = Path.Combine(temporaryRoot, "mount recovery");
        var localRoot = Path.Combine(temporaryRoot, "mount source"); Directory.CreateDirectory(localRoot);
        var mountLetter = "ZYXWVUT".Select(c => c + ":").First(l => !DriveInfo.GetDrives().Any(d => d.Name.StartsWith(l, StringComparison.OrdinalIgnoreCase)));
        var mountedProfile = new DriveProfile { Name = "Drive chung – tên tiếng Việt tùy chỉnh", Letter = mountLetter, Remote = "mount_fixture:", ReadOnly = false };
        var first = new DriveService(mountRoot, rclone);
        Directory.CreateDirectory(mountRoot);
        File.WriteAllText(first.ConfigPath, "[mount_fixture]\ntype = local\n" + "[readonly_fixture]\ntype = drive\nscope = drive.readonly\n");
        mountedProfile.Remote += localRoot.Replace('\\', '/');
        first.SaveProfiles(new[] { mountedProfile });
        var onlyRead = new DriveProfile { Name = "readonly auth", Remote = "readonly_fixture:", Letter = mountLetter, ReadOnly = false };
        await RejectAsync(() => first.ValidateWriteAccessAsync(onlyRead), typeof(InvalidOperationException), "writable mount rejects a read-only Google OAuth grant before creating a drive");
        onlyRead.ReadOnly = true; await first.ValidateWriteAccessAsync(onlyRead);
        Check(true, "read-only mounts retain support for read-only OAuth grants");
        DriveService? recovered = null;
        int? disposableMountPid = null;
        try
        {
            await first.ConnectAsync(mountedProfile);
            var leaseFile = Path.Combine(mountRoot, "sessions", mountedProfile.Id + ".json");
            var lease = System.Text.Json.JsonSerializer.Deserialize<MountSession>(File.ReadAllText(leaseFile))!;
            disposableMountPid = lease.ProcessId;
            Check(!File.ReadAllText(leaseFile).Contains(lease.Secret()) && lease.Secret().Length > 20, "real mount persists Windows-protected controller credentials without plaintext");
            Check(Directory.Exists(mountLetter + "\\"), "WinFsp creates a real disposable Windows network drive");
            Check(WindowsDrive.Connection(mountLetter)?.StartsWith(@"\\easymount\", StringComparison.OrdinalIgnoreCase) == true, "network volume uses the service host instead of server");
            Check(WindowsDrive.Connection(mountLetter) == await first.NetworkVolumeAsync(mountedProfile), "long custom Vietnamese drive names survive the complete UNC path without truncation");
            first.Dispose();
            recovered = new DriveService(mountRoot, rclone);
            var wrong = lease with { StartTicks = lease.StartTicks - 1 };
            MountSession.Save(leaseFile, wrong);
            await recovered.RecoverMountsAsync();
            Check(!recovered.IsConnected(mountedProfile.Id), "recovery refuses a mismatched process creation time");
            await RejectAsync(() => recovered.PrepareUninstallAsync(), typeof(IOException), "uninstall refuses an unverified surviving session instead of deleting the app");
            MountSession.Save(leaseFile, lease);
            MountSession.Save(leaseFile, lease with { ProtectedSecret = MountSession.Protect("wrong-controller-secret") });
            await recovered.RecoverMountsAsync();
            Check(!recovered.IsConnected(mountedProfile.Id), "recovery rejects an unauthenticated controller without touching the worker");
            MountSession.Save(leaseFile, lease);
            await recovered.RecoverMountsAsync();
            Check(recovered.IsConnected(mountedProfile.Id), "new service authenticates and recovers the surviving real mount");
            File.WriteAllText(mountLetter + "\\pending.txt", "Disposable EasyMount pending upload fixture");
            await RejectAsync(() => recovered.DisconnectAsync(mountedProfile.Id), typeof(IOException), "disconnect refuses a real pending cached upload");
            await RejectAsync(() => recovered.PrepareUninstallAsync(), typeof(IOException), "uninstall preflight preserves a real pending upload");
            Check(recovered.IsConnected(mountedProfile.Id), "pending-upload refusal preserves the running filesystem");
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (!File.Exists(Path.Combine(localRoot, "pending.txt")) && DateTime.UtcNow < deadline) await Task.Delay(250);
            Check(File.ReadAllText(Path.Combine(localRoot, "pending.txt")) == "Disposable EasyMount pending upload fixture", "queued fixture is completely written to its local backend");
            await Task.Delay(500);
            await recovered.PrepareUninstallAsync();
            Check(!Directory.Exists(mountLetter + "\\") && !File.Exists(leaseFile), "safe disconnect removes real drive and recovery lease");
            Check(WindowsDrive.Connection(mountLetter) is null, "safe disconnect removes the Windows network mapping");
            mountedProfile.Remote = "mount_fixture:" + temporaryRoot.Replace('\\', '/');
            recovered.SaveProfiles(new[] { mountedProfile });
            Check(Directory.Exists(Path.Combine(mountRoot, "cache-archive")), "verified clean cache is archived before changing cloud routes");
        }
        finally
        {
            recovered?.Dispose(); first.Dispose();
            if (disposableMountPid is int fixturePid)
            {
                try { using var fixtureWorker = Process.GetProcessById(fixturePid); if (!fixtureWorker.HasExited && fixtureWorker.MainModule?.FileName == rclone) { fixtureWorker.Kill(); await fixtureWorker.WaitForExitAsync(); } }
                catch (ArgumentException) { }
            } // Disposable local-fixture worker only; never a personal cloud mount.

        }
    }
    if (service.HasWinFsp && args.Contains("--mounts"))
    {
        var poolRoot = Path.Combine(temporaryRoot, "real pool");
        var firstSource = Path.Combine(temporaryRoot, "priority one"); Directory.CreateDirectory(firstSource);
        var secondSource = Path.Combine(temporaryRoot, "priority two"); Directory.CreateDirectory(secondSource);
        using var pooled = new DriveService(poolRoot, rclone);
        File.WriteAllText(pooled.ConfigPath, "[pool_a]\ntype = local\n[pool_b]\ntype = local\n");
        var drive = "ZYXWVUT".Select(c => c + ":").First(l => !DriveInfo.GetDrives().Any(d => d.Name.StartsWith(l)));
        var actualPool = new DriveProfile { Name = "Pool fixture", Letter = drive, Sources = new() {
            new() { Remote = "pool_a:" + firstSource.Replace('\\', '/'), LimitBytes = 1024 },
            new() { Remote = "pool_b:" + secondSource.Replace('\\', '/'), LimitBytes = 4096 } } };
        pooled.SaveProfiles(new[] { actualPool });
        Check((await pooled.NetworkVolumeAsync(actualPool)).StartsWith(@"\\pool\"), "pooled network host is compact so the friendly name leaves room for Explorer capacity text");
        try
        {
            await pooled.ConnectAsync(actualPool);
            var capacity = await pooled.GetCapacityAsync(actualPool);
            Check(capacity is { TotalBytes: > 0, UsedBytes: >= 0 }, "live pooled controller provides capacity metadata for the UI");
            File.WriteAllBytes(drive + "\\first.bin", Enumerable.Repeat((byte)17, 700).ToArray());
            var deadline = DateTime.UtcNow.AddSeconds(35);
            while (!File.Exists(Path.Combine(firstSource, "first.bin")) && DateTime.UtcNow < deadline) await Task.Delay(200);
            Check(File.ReadAllBytes(Path.Combine(firstSource, "first.bin")).SequenceEqual(Enumerable.Repeat((byte)17, 700)), "real pooled mount places a complete new file on priority source one");
            File.WriteAllBytes(drive + "\\second.bin", Enumerable.Repeat((byte)29, 700).ToArray());
            deadline = DateTime.UtcNow.AddSeconds(35);
            while (!File.Exists(Path.Combine(secondSource, "second.bin")) && DateTime.UtcNow < deadline) await Task.Delay(200);
            Check(File.ReadAllBytes(Path.Combine(secondSource, "second.bin")).SequenceEqual(Enumerable.Repeat((byte)29, 700)) && !File.Exists(Path.Combine(firstSource, "second.bin")), "real pooled mount switches to source two when source one cannot fit the whole file");
            Check(File.Exists(drive + "\\first.bin") && File.Exists(drive + "\\second.bin"), "both priority sources are readable through the same Windows drive");
            await Task.Delay(1000);
            await pooled.DisconnectAsync(actualPool.Id);
            Check(WindowsDrive.Connection(drive) is null && !Directory.Exists(drive + "\\"), "pooled mount disconnect removes both mapping and logical drive");
            await pooled.ConnectAsync(actualPool);
            await pooled.DisconnectAsync(actualPool.Id);
            Check(WindowsDrive.Connection(drive) is null, "reconnect and second disconnect leave no duplicate mapping");
        }
        finally { if (pooled.IsConnected(actualPool.Id)) await pooled.DisconnectAsync(actualPool.Id); }
    }
    if (!service.HasWinFsp)
    {
        var states = new List<DriveSnapshot>();
        service.StateChanged += state => states.Add(state);
        await RejectAsync(() => service.ConnectAsync(profile), typeof(InvalidOperationException), "missing WinFsp blocks mounting before a process or drive is created");
        Check(states.Any(s => s.ProfileId == profile.Id && s.Status == "Error" && s.Error!.Contains("WinFsp")) && !service.IsConnected(profile.Id),
            "mount prerequisite failure is visible and never marked connected");
    }
    else Console.WriteLine("SKIP missing-driver check: WinFsp is already installed; pass --mounts to run isolated local WinFsp checks.");
    await service.DisconnectAllAsync();
    Check(true, "disconnecting an idle service completes safely");
    Console.WriteLine($"RESULT: {passed} checks passed. Local WinFsp mount tests requested: {args.Contains("--mounts")}. Google/Microsoft cloud writes are not exercised.");
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
