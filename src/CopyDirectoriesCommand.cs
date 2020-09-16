namespace RobustCopy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    using CliFx;
    using CliFx.Attributes;

    [Command]
    public class CopyDirectoriesCommand : ICommand
    {
        [CommandParameter(0, Description = "Source directory")]
        public string Source { get; set; }

        [CommandParameter(1, Description = "Destination directory")]
        public string Destination { get; set; }

        [CommandOption('t', Description = "Number of copy threads")]
        public int NumThreads { get; set; } = 2;

        public async ValueTask ExecuteAsync(IConsole console)
        {
            if (!Directory.Exists(Source))
            {
                console.Output.WriteLine($"Source Directory {Source} is not present");
                throw new DirectoryNotFoundException(Source);
            }

            if (File.Exists(Constants.CopiedFilesLogPath))
            {
                alreadyCopiedFiles = new HashSet<string>(File.ReadAllLines(Constants.CopiedFilesLogPath), StringComparer.OrdinalIgnoreCase);
            }

            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < NumThreads; i++)
            {
                var fileCopy = new FileCopy(DiscoveredFiles, CopiedFiles);
                var thread = new Thread(fileCopy.CopyFiles);
                thread.Start();
                threads.Add(thread);
            }

            Thread loggerThread;
            {
                var copiedFiles = new CopiedFilesLogger(CopiedFiles);
                loggerThread = new Thread(copiedFiles.LogCopiedFiles);
                loggerThread.Start();
            }

            if (File.Exists(Constants.DiscoveredFilesLogPath))
            {
                foreach (var line in File.ReadAllLines(Constants.DiscoveredFilesLogPath))
                {
                    string destFileName = line.Replace(Source, Destination);
                    if (!alreadyCopiedFiles.Contains(destFileName))
                    {
                        DiscoveredFiles.Add(Tuple.Create(line, destFileName));
                    }
                }
            }
            else
            {
                TraverseFileSystem(Source, Destination);
                File.WriteAllLines(Constants.DiscoveredFilesLogPath, discoveredFiles);
            }

            DiscoveredFiles.CompleteAdding();
            threads.ForEach(x => x.Join());
            CopiedFiles.CompleteAdding();
            loggerThread.Join();
        }

        private BlockingCollection<Tuple<string, string>> DiscoveredFiles = new BlockingCollection<Tuple<string, string>>();
        private BlockingCollection<string> CopiedFiles = new BlockingCollection<string>();
        private HashSet<string> alreadyCopiedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private List<string> discoveredFiles = new List<string>();

        internal void TraverseFileSystem(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            List<string> subDirectories = new List<string>();
            IntPtr hFindHandle = IntPtr.Zero;
            try
            {
                WIN32_FIND_DATAW findData = new WIN32_FIND_DATAW();
                string findFileSpec = Path.Combine(source, "*");
                hFindHandle = NativeMethods.FindFirstFileW(findFileSpec, out findData);
                if (hFindHandle == (IntPtr)(-1))
                {
                    NativeMethods.RaiseIOExceptionFromErrorCode((Win32ErrorCode)Marshal.GetLastWin32Error(), source);
                }

                do
                {
                    if (findData.cFileName == "." || findData.cFileName == "..")
                    {
                        continue;
                    }

                    string fullName = Path.Combine(source, findData.cFileName);
                    string destFileName = Path.Combine(destination, findData.cFileName);

                    if ((findData.dwFileAttributes & FileAttributes.Directory) != 0)
                    {
                        subDirectories.Add(findData.cFileName);
                    }

                    if ((findData.dwFileAttributes & FileAttributes.Directory) == 0)
                    {
                        if (!alreadyCopiedFiles.Contains(destFileName))
                        {
                            discoveredFiles.Add(fullName);
                            DiscoveredFiles.Add(Tuple.Create(fullName, destFileName));
                        }
                    }
                }
                while (NativeMethods.FindNextFileW(hFindHandle, out findData));

                Win32ErrorCode errorCode = (Win32ErrorCode)Marshal.GetLastWin32Error();
                if (errorCode != Win32ErrorCode.ERROR_NO_MORE_FILES)
                {
                    NativeMethods.RaiseIOExceptionFromErrorCode(errorCode, source);
                }

                foreach (var dirName in subDirectories)
                {
                    TraverseFileSystem(Path.Combine(source, dirName), Path.Combine(destination, dirName));
                }
            }
            finally
            {
                if (hFindHandle != (IntPtr)(-1))
                {
                    NativeMethods.FindClose(hFindHandle);
                }
            }
        }
    }
}
