using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace EasyMount;

internal static class WindowsDrive
{
    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetGetConnectionW(string localName, StringBuilder remoteName, ref int length);
    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetCancelConnection2W(string name, int flags, [MarshalAs(UnmanagedType.Bool)] bool force);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern void SHChangeNotify(int change, uint flags, string item1, IntPtr item2);

    public static string? Connection(string letter)
    {
        var buffer = new StringBuilder(4096); var length = buffer.Capacity;
        var result = WNetGetConnectionW(letter, buffer, ref length);
        return result is 0 or 1201 && buffer.Length > 0 ? buffer.ToString() : null;
    }

    public static void Added(string letter) => SHChangeNotify(0x100, 0x1005, letter + "\\", IntPtr.Zero);

    // Called only after the owned worker stopped and the logical drive disappeared.
    // Never cancel a new mapping made by another app, or force-close open files.
    public static void Removed(string letter, string? expectedRemote)
    {
        if (Array.Exists(DriveInfo.GetDrives(), d => d.Name.StartsWith(letter, StringComparison.OrdinalIgnoreCase))) return;
        var current = Connection(letter);
        if (current is not null && expectedRemote is not null && current.Equals(expectedRemote, StringComparison.OrdinalIgnoreCase))
        {
            var result = WNetCancelConnection2W(letter, 1, false);
            if (result is not (0 or 2250)) throw new IOException($"Windows chưa bỏ ánh xạ {letter} (mã {result}). Thử ngắt lại sau khi đóng Explorer đang dùng ổ.");
        }
        SHChangeNotify(0x80, 0x1005, letter + "\\", IntPtr.Zero);
    }
}
