using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Marvel.Server;

/// <summary>Platform durability primitives shared by session and campaign generations.</summary>
internal static class DurableFiles
{
    internal static void WriteFlushed(string path, ReadOnlySpan<byte> bytes)
    {
        using var stream = new FileStream(
            path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            bufferSize: 4096, FileOptions.WriteThrough);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        stream.Write(bytes);
        stream.Flush(flushToDisk: true);
    }

    internal static void EnsurePrivateDirectory(
        string path,
        bool setModeWhenAlreadyPresent = true)
    {
        if (!Directory.Exists(path))
        {
            string? parent = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parent))
            {
                throw new IOException("session directory has no durable parent");
            }

            // Existing ancestors may be shared system directories. They provide the
            // durable rename boundary, but the server does not own their permissions.
            EnsurePrivateDirectory(parent, setModeWhenAlreadyPresent: false);
            string temporary = Path.Combine(
                parent,
                ".creating-" + Path.GetFileName(path) + "-"
                + Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant());
            Directory.CreateDirectory(temporary);
            SetPrivateDirectoryMode(temporary);
            try
            {
                MoveDurably(temporary, path, overwrite: false);
            }
            finally
            {
                if (Directory.Exists(temporary))
                {
                    Directory.Delete(temporary);
                }
            }
        }

        if (setModeWhenAlreadyPresent)
        {
            SetPrivateDirectoryMode(path);
        }
    }

    private static void SetPrivateDirectoryMode(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead
                | UnixFileMode.UserWrite
                | UnixFileMode.UserExecute);
        }
    }

    internal static void MoveDurably(string source, string destination, bool overwrite)
    {
        if (OperatingSystem.IsWindows())
        {
            uint flags = 0x00000008;
            if (overwrite)
            {
                flags |= 0x00000001;
            }

            if (!MoveFileEx(source, destination, flags))
            {
                throw new IOException(
                    "could not durably publish a session generation",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            }
            return;
        }

        if (Directory.Exists(source))
        {
            if (overwrite)
            {
                throw new IOException("session directories cannot be replaced");
            }

            Directory.Move(source, destination);
        }
        else
        {
            File.Move(source, destination, overwrite);
        }
        FlushDirectory(Path.GetDirectoryName(destination)!);
    }

    internal static void FlushDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        int descriptor = OpenDirectory(path, 0);
        if (descriptor < 0)
        {
            throw new IOException(
                "could not open session directory for durability",
                new Win32Exception(Marshal.GetLastWin32Error()));
        }

        try
        {
            if (FlushDescriptor(descriptor) != 0)
            {
                throw new IOException(
                    "could not flush session directory metadata",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }
        finally
        {
            _ = CloseDescriptor(descriptor);
        }
    }

    [DllImport("kernel32.dll", EntryPoint = "MoveFileExW", SetLastError = true,
        CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MoveFileEx(
        string existingFileName,
        string newFileName,
        uint flags);

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    private static extern int OpenDirectory(string path, int flags);

    [DllImport("libc", EntryPoint = "fsync", SetLastError = true)]
    private static extern int FlushDescriptor(int descriptor);

    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    private static extern int CloseDescriptor(int descriptor);

}
