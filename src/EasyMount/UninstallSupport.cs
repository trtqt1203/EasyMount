using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EasyMount;

// Same-user IPC carries only a shutdown request, never cloud/controller credentials.
internal sealed class UninstallSupport : IDisposable
{
    private readonly CancellationTokenSource stopped = new();
    private static string RegistryDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyMount");
    private static string RootsFile => Path.Combine(RegistryDirectory, "runtime-roots.json");
    private static string GuardFile => Path.Combine(RegistryDirectory, "uninstall.pending");
    private static string Pipe(int pid) => "EasyMount.Uninstall." + pid;

    public static void RegisterRoot(string root)
    {
        Directory.CreateDirectory(RegistryDirectory);
        using var gate = new Mutex(false, @"Local\EasyMount.RuntimeRoots");
        try { gate.WaitOne(); } catch (AbandonedMutexException) { }
        try
        {
            var roots = ReadRoots().Append(Path.GetFullPath(root)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var temporary = RootsFile + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(roots));
            File.Move(temporary, RootsFile, true);
        }
        finally { gate.ReleaseMutex(); }
    }

    private static string[] ReadRoots() => File.Exists(RootsFile)
        ? JsonSerializer.Deserialize<string[]>(File.ReadAllText(RootsFile)) ?? throw new InvalidDataException("Không đọc được danh sách phiên EasyMount.") : Array.Empty<string>();

    public static void EnsureNotUninstalling()
    {
        if (!File.Exists(GuardFile)) return;
        try
        {
            if (Mutex.TryOpenExisting(@"Global\_MSIExecute", out var installer))
            {
                using (installer)
                {
                    bool acquired;
                    try { acquired = installer.WaitOne(0); } catch (AbandonedMutexException) { acquired = true; }
                    if (!acquired) throw new IOException("Bộ gỡ EasyMount đang chạy. Hãy chờ hoàn tất hoặc hủy gỡ trước khi mở/kết nối ổ.");
                    installer.ReleaseMutex();
                }
            }
            File.Delete(GuardFile); // No MSI transaction owns the guard after completion/cancellation.
        }
        catch (UnauthorizedAccessException) { throw new IOException("Chưa xác minh được trạng thái bộ gỡ EasyMount. Hãy hoàn tất bộ gỡ trước khi kết nối ổ."); }
    }

    public UninstallSupport(Func<Task<string?>> prepare, Action exit)
    {
        _ = ListenAsync(prepare, exit);
    }

    private async Task ListenAsync(Func<Task<string?>> prepare, Action exit)
    {
        while (!stopped.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(Pipe(Environment.ProcessId), PipeDirection.InOut, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(stopped.Token);
                using var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, true);
                using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, true) { AutoFlush = true };
                var request = await reader.ReadLineAsync(stopped.Token).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
                if (request != "prepare-uninstall") { await writer.WriteLineAsync("Yêu cầu không hợp lệ."); continue; }
                var error = await prepare();
                await writer.WriteLineAsync(error ?? "ready");
                if (error is null) { exit(); return; }
            }
            catch (OperationCanceledException) { return; }
            catch (IOException) { /* A disconnected client must not crash the running app. */ }
            catch (TimeoutException) { }
        }
    }

    public static async Task<int> RunAsync(string executable, Action<string>? report = null)
    {
        Directory.CreateDirectory(RegistryDirectory);
        File.WriteAllText(GuardFile, DateTimeOffset.UtcNow.ToString("o"));
        try
        {
            foreach (var process in Process.GetProcessesByName("EasyMount"))
            {
                using (process)
                {
                    if (process.Id == Environment.ProcessId || process.HasExited) continue;
                    if (!string.Equals(process.MainModule?.FileName, executable, StringComparison.OrdinalIgnoreCase)) continue;
                    using var client = new NamedPipeClientStream(".", Pipe(process.Id), PipeDirection.InOut,
                        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                    await client.ConnectAsync(5000);
                    using var writer = new StreamWriter(client, new UTF8Encoding(false), 1024, true) { AutoFlush = true };
                    using var reader = new StreamReader(client, Encoding.UTF8, false, 1024, true);
                    await writer.WriteLineAsync("prepare-uninstall");
                    var response = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromMinutes(2));
                    if (response != "ready") throw new IOException(response ?? "EasyMount chưa xác nhận ngắt ổ an toàn.");
                    await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20));
                }
            }
            var engine = Path.Combine(Path.GetDirectoryName(executable)!, "tools", "rclone.exe");
            foreach (var root in ReadRoots().Append(RegistryDirectory).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!Path.IsPathFullyQualified(root)) throw new InvalidDataException("Đường dẫn phiên không hợp lệ.");
                if (!Directory.Exists(root)) continue;
                using var service = new DriveService(root, engine);
                await service.PrepareUninstallAsync();
            }
            foreach (var worker in Process.GetProcessesByName("rclone"))
                using (worker)
                    if (!worker.HasExited && string.Equals(worker.MainModule?.FileName, engine, StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Còn tiến trình ổ đĩa chưa xác minh được. Mở EasyMount để phục hồi và ngắt ổ trước khi gỡ.");
            return 0;
        }
        catch (Exception ex)
        {
            File.Delete(GuardFile);
            var reason = ex is IOException ? ex.Message : "Không xác minh được toàn bộ phiên EasyMount. Giữ nguyên ứng dụng và thử lại sau khi đóng các cửa sổ cấu hình hoặc phiên của người dùng khác.";
            File.WriteAllText(Path.Combine(RegistryDirectory, "uninstall-blocked.txt"), reason);
            report?.Invoke("Chưa thể gỡ EasyMount: " + reason);
            return 1;
        }
    }

    public void Dispose() { stopped.Cancel(); }
}
