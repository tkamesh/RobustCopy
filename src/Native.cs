namespace RobustCopy
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    public static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr FindFirstFileW(string lpFileName, out WIN32_FIND_DATAW lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FindNextFileW(IntPtr hFindFile, out WIN32_FIND_DATAW lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FindClose(IntPtr hFindFile);

        internal static int MakeHRFromErrorCode(Win32ErrorCode errorCode)
        {
            return (int)(0x80070000 | (uint)errorCode);
        }

        internal static void RaiseIOExceptionFromErrorCode(Win32ErrorCode errorCode, string maybeFullPath)
        {
            switch (errorCode)
            {
                case Win32ErrorCode.ERROR_FILE_NOT_FOUND:
                    throw new FileNotFoundException();
                case Win32ErrorCode.ERROR_PATH_NOT_FOUND:
                    throw new DirectoryNotFoundException();
                case Win32ErrorCode.ERROR_ACCESS_DENIED:
                    throw new UnauthorizedAccessException();
                case Win32ErrorCode.ERROR_ALREADY_EXISTS:
                    if (maybeFullPath.Length == 0)
                    {
                        goto default;
                    }

                    throw new IOException("File already exists: " + maybeFullPath);

                case Win32ErrorCode.ERROR_FILENAME_EXCED_RANGE:
                    throw new PathTooLongException("The path \"" + maybeFullPath + "\" is still too long");

                case Win32ErrorCode.ERROR_INVALID_DRIVE:
                    throw new DriveNotFoundException(maybeFullPath);

                case Win32ErrorCode.ERROR_INVALID_PARAMETER:
                    throw new ArgumentException("Invalid parameter");

                case Win32ErrorCode.ERROR_SHARING_VIOLATION:
                    if (maybeFullPath.Length == 0)
                    {
                        throw new IOException("Sharing violation", NativeMethods.MakeHRFromErrorCode(errorCode));
                    }
                    else
                    {
                        throw new IOException("Sharing violation to " + maybeFullPath, NativeMethods.MakeHRFromErrorCode(errorCode));
                    }

                case Win32ErrorCode.ERROR_FILE_EXISTS:
                    if (maybeFullPath.Length == 0)
                    {
                        goto default;
                    }

                    throw new IOException("The file \"" + maybeFullPath + "\" already exists", NativeMethods.MakeHRFromErrorCode(errorCode));

                case Win32ErrorCode.ERROR_JOURNAL_ENTRY_DELETED:
                    throw new IOException("Journal entry deleted", NativeMethods.MakeHRFromErrorCode(errorCode));

                case Win32ErrorCode.ERROR_OPERATION_ABORTED:
                    throw new OperationCanceledException();

                case Win32ErrorCode.ERROR_INVALID_NAME:
                    throw new IOException("The path \"" + maybeFullPath + "\" is invalid", NativeMethods.MakeHRFromErrorCode(errorCode));

                default:
                    throw new IOException(NativeMethods.GetMessage(errorCode), NativeMethods.MakeHRFromErrorCode(errorCode));
            }
        }
    }

    public enum Win32ErrorCode
    {
        ERROR_FILE_NOT_FOUND = 2,
        ERROR_PATH_NOT_FOUND = 3,
        ERROR_ACCESS_DENIED = 5,
        ERROR_INVALID_HANDLE = 6,
        ERROR_INVALID_DRIVE = 15,
        ERROR_NO_MORE_FILES = 18,
        ERROR_NOT_READY = 21,
        ERROR_SHARING_VIOLATION = 32,
        ERROR_FILE_EXISTS = 80,
        ERROR_INVALID_PARAMETER = 87,       //  0x57
        ERROR_INSUFFICIENT_BUFFER = 122,
        ERROR_INVALID_NAME = 123,           //  0x7b
        ERROR_BAD_PATHNAME = 161,
        ERROR_ALREADY_EXISTS = 183,
        ERROR_FILENAME_EXCED_RANGE = 206,
        ERROR_OPERATION_ABORTED = 995,
        ERROR_JOURNAL_ENTRY_DELETED = 1181
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WIN32_FIND_DATAW
    {
        public FileAttributes dwFileAttributes;
        public FILETIME ftCreationTime;
        public FILETIME ftLastAccessTime;
        public FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public int dwReserved0;
        public int dwReserved1;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }
}
