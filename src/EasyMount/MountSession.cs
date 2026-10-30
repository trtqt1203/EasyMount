using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EasyMount;

// Only controller credentials are protected here; cloud credentials remain in rclone.conf.
internal sealed record MountSession(int ProcessId, long StartTicks, string Executable, string ProfileId, string Letter,
    bool ReadOnly, string Routing, int Port, string ProtectedSecret)
{
    public static string Protect(string secret) => Convert.ToBase64String(Crypt(Encoding.UTF8.GetBytes(secret), true));
    public string Secret() => Encoding.UTF8.GetString(Crypt(Convert.FromBase64String(ProtectedSecret), false));
    public bool Matches(Process process, DriveProfile profile, string routing) =>
        ProcessId == process.Id && StartTicks == process.StartTime.ToUniversalTime().Ticks &&
        string.Equals(Path.GetFullPath(Executable), process.MainModule?.FileName, StringComparison.OrdinalIgnoreCase) &&
        ProfileId == profile.Id && Letter == profile.Letter && ReadOnly == profile.ReadOnly && Routing == routing && Port is > 0 and < 65536;

    public static void Save(string file, MountSession session)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var temporary = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { JsonSerializer.Serialize(stream, session); stream.Flush(true); }
            File.Move(temporary, file, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    [StructLayout(LayoutKind.Sequential)] private struct Blob { public int Length; public IntPtr Data; }
    [DllImport("crypt32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(ref Blob input, string? description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out Blob output);
    [DllImport("crypt32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(ref Blob input, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out Blob output);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr memory);
    private static byte[] Crypt(byte[] bytes, bool protect)
    {
        var input = new Blob { Length = bytes.Length, Data = Marshal.AllocHGlobal(bytes.Length) };
        Blob output = default;
        try
        {
            Marshal.Copy(bytes, 0, input.Data, bytes.Length);
            var ok = protect ? CryptProtectData(ref input, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output)
                : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output);
            if (!ok) throw new CryptographicException("Không đọc/ghi được thông tin khôi phục phiên của tài khoản Windows này.");
            var result = new byte[output.Length]; Marshal.Copy(output.Data, result, 0, result.Length); return result;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
            for (int i = 0; i < input.Length; i++) Marshal.WriteByte(input.Data, i, 0);
            Marshal.FreeHGlobal(input.Data);
            if (output.Data != IntPtr.Zero) { for (int i = 0; i < output.Length; i++) Marshal.WriteByte(output.Data, i, 0); LocalFree(output.Data); }
        }
    }
}
