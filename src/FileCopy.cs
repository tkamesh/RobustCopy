namespace RobustCopy
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;

    public class FileCopy
    {
        private BlockingCollection<Tuple<string, string>> filesToCopy;
        private BlockingCollection<string> copiedFiles;

        public FileCopy(BlockingCollection<Tuple<string, string>> filesToCopy, BlockingCollection<string> copiedFiles)
        {
            this.filesToCopy = filesToCopy;
            this.copiedFiles = copiedFiles;
        }

        public void CopyFiles()
        {
            while (!filesToCopy.IsCompleted)
            {
                if (filesToCopy.TryTake(out Tuple<string, string> work, 10))
                {
                    File.Copy(work.Item1, work.Item2, true);
                    copiedFiles.Add(work.Item2);
                }
            }
        }
    }
}
