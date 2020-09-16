namespace RobustCopy
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;

    public class CopiedFilesLogger
    {
        private BlockingCollection<string> copiedFiles;

        public CopiedFilesLogger(BlockingCollection<string> copiedFiles)
        {
            this.copiedFiles = copiedFiles;
        }

        public void LogCopiedFiles()
        {
            using (TextWriter writer = new StreamWriter(Constants.CopiedFilesLogPath, true))
            {
                while (!copiedFiles.IsCompleted)
                {
                    if (copiedFiles.TryTake(out string filePath, 10))
                    {
                        Console.WriteLine(filePath);
                        writer.WriteLine(filePath);
                        writer.Flush();
                    }
                }
            }
        }
    }
}
