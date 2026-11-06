using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace EasyMount;

internal static class ConfigProtection
{
    private static bool Encrypted(string path) => File.Exists(path) && File.ReadLines(path).Take(4).Any(line => line.Contains("RCLONE_ENCRYPT_V0", StringComparison.Ordinal));
    internal static void Apply(ProcessStartInfo info, string config)
    {
        if (!Encrypted(config)) return;
        var key = config + ".key";
        if (!File.Exists(key)) throw new IOException("Encrypted cloud configuration needs its Windows-protected key. Keep both files and use the original Windows account.");
        info.Environment["RCLONE_CONFIG_PASS"] = MountSession.Unprotect(File.ReadAllText(key));
    }
    internal static bool Ensure(string config, string engine)
    {
        if (Encrypted(config)) { var check = new ProcessStartInfo(); Apply(check, config); return true; }
        // Existing workers may still save refreshed tokens. Migrate only with all workers stopped.
        if (!File.Exists(engine) || Process.GetProcessesByName("rclone").Length != 0) return false;
        using var gate = new System.Threading.Mutex(false, @"Local\EasyMount.ConfigProtection");
        if (!gate.WaitOne(TimeSpan.FromSeconds(15))) throw new IOException("Cloud configuration is being secured by another EasyMount process.");
        var temp = config + ".encrypt-" + Guid.NewGuid().ToString("N");
        try
        {
            if (Encrypted(config)) return true;
            var keyFile = config + ".key";
            var password = File.Exists(keyFile) ? MountSession.Unprotect(File.ReadAllText(keyFile)) : Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            if (!File.Exists(keyFile)) File.WriteAllText(keyFile, MountSession.Protect(password));
            if (File.Exists(config)) File.Copy(config, temp); else File.WriteAllText(temp, "");
            var start = new ProcessStartInfo(engine) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in new[] { "--config", temp, "config", "encryption", "set" }) start.ArgumentList.Add(arg);
            using var process = Process.Start(start) ?? throw new IOException("Cannot start configuration encryption.");
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            process.StandardInput.WriteLine(password); process.StandardInput.WriteLine(password); process.StandardInput.Close();
            if (!process.WaitForExit(15000)) { process.Kill(true); throw new IOException("Configuration encryption timed out; original configuration retained."); }
            System.Threading.Tasks.Task.WaitAll(stdout, stderr);
            if (process.ExitCode != 0 || !Encrypted(temp)) throw new IOException("Configuration encryption failed; original configuration retained.");
            start = new ProcessStartInfo(engine) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in new[] { "--config", temp, "--ask-password=false", "listremotes" }) start.ArgumentList.Add(arg);
            start.Environment["RCLONE_CONFIG_PASS"] = password;
            using var verify = Process.Start(start) ?? throw new IOException("Cannot verify encrypted configuration.");
            stdout = verify.StandardOutput.ReadToEndAsync(); stderr = verify.StandardError.ReadToEndAsync();
            if (!verify.WaitForExit(15000)) { verify.Kill(true); throw new IOException("Encrypted configuration verification timed out."); }
            System.Threading.Tasks.Task.WaitAll(stdout, stderr);
            if (verify.ExitCode != 0) throw new IOException("Encrypted configuration verification failed; original retained.");
            File.Move(temp, config, true);
            return true;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); gate.ReleaseMutex(); }
    }
}
