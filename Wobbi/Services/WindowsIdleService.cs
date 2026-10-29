
using System;
using System.Runtime.InteropServices;

namespace Wobbi.Services;

internal static class WindowsIdleService
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(
        ref LastInputInfo info);

    public static TimeSpan? GetIdleTime()
    {
        var info = new LastInputInfo
        {
            cbSize = (uint)Marshal.SizeOf<LastInputInfo>()
        };

        if (!GetLastInputInfo(ref info))
            return null;

        uint idleMilliseconds = unchecked(
            (uint)Environment.TickCount - info.dwTime);

        return TimeSpan.FromMilliseconds(idleMilliseconds);
    }
}
