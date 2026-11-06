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
    [DllImport("shell32.dll", EntryPoint = "SHChangeNotify")]
    private static extern void NotifyFolder(int change, uint flags, IntPtr item1, IntPtr item2);
    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderIDList(in Guid folder, uint flags, IntPtr token, out IntPtr item);
    [StructLayout(LayoutKind.Sequential)]
    private struct VolumeChange
    {
        public uint Size, DeviceType, Reserved, UnitMask;
        public ushort Flags;
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessageTimeoutW(IntPtr window, uint message, UIntPtr action,
        ref VolumeChange volume, uint flags, uint timeout, out UIntPtr result);

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
        // Invalidate Explorer's cached network-drive mask, not just its tree item.
        var volume = new VolumeChange { Size = (uint)Marshal.SizeOf<VolumeChange>(), DeviceType = 2,
            UnitMask = 1u << (char.ToUpperInvariant(letter[0]) - 'A'), Flags = 2 };
        SendMessageTimeoutW(new IntPtr(0xffff), 0x219, new UIntPtr(0x8004), ref volume, 2, 1000, out _);
        // A drive-path notification alone can leave ghost entries in This PC.
        // Refresh its namespace too, as WinFsp's mount removal does.
        var computerFolder = new Guid("0ac0837c-bbf8-452a-850d-79d08e667ca7");
        if (SHGetKnownFolderIDList(in computerFolder, 0, IntPtr.Zero, out var item) >= 0)
        {
            try { NotifyFolder(0x1000, 0x1000, item, IntPtr.Zero); }
            finally { Marshal.FreeCoTaskMem(item); }
        }
    }
}
