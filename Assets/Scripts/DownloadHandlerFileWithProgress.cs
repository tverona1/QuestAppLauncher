using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace QuestAppLauncher
{
    /// <summary>
    /// File download handler class with callback for progress.
    /// This is a download hanlder that can be assocaited with a UnityWebRequest.
    /// Based on: DownloadHandlerFile.cs by Luke Holland
    /// (https://gist.github.com/luke161/a251b01c00f58d65a252812be8dce670)
    /// </summary>
    public class DownloadHandlerFileWithProgress : DownloadHandler
    {
        // Whether to remove file on error
        public bool removeFileOnAbort = false;

        // Total content length in bytes
        private int contentLength;

        // Bytes recieved so far
        private int received;

        // File stream to write to
        private FileStream stream;

        // Callback to incidate progress: Percentage, total content length and current received
        private Action<float, int, int> downloadProgress;

        // Whether we have successfully received all the data
        private bool receivedAllData = false;

        /// <summary>
        /// Constructor for the file download handler. Instantiates base class in pre-allocated mode.
        /// </summary>
        /// <param name="localFilePath">Local path to save file to</param>
        /// <param name="downloadProgress">Download progress indicator</param>
        /// <param name="bufferSize">Buffer size</param>
        /// <param name="fileShare">File sharing mode</param>
        public DownloadHandlerFileWithProgress(string localFilePath, Action<float, int, int> downloadProgress = null,
            int bufferSize = 32768, FileShare fileShare = FileShare.ReadWrite) : base(new byte[bufferSize])
        {
            string directory = Path.GetDirectoryName(localFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            this.downloadProgress = downloadProgress;
            this.contentLength = -1;
            this.received = 0;
            this.stream = new FileStream(localFilePath, FileMode.OpenOrCreate, FileAccess.Write, fileShare, bufferSize);
        }

        protected override float GetProgress()
        {
            return contentLength <= 0 ? 0 : Mathf.Clamp01((float)this.received / (float)contentLength);
        }

        public override void ReceiveContentLength(int contentLength)
        {
            this.contentLength = contentLength;
        }

        public override async Task<bool> ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || data.Length == 0)
            {
                return false;
            }

            this.received += dataLength;
            await this.stream.WriteAsync(data, 0, dataLength);

            if (null != this.downloadProgress)
            {
                this.downloadProgress(GetProgress(), this.contentLength, this.received);
            }

            return true;
        }

        public override void CompleteContent()
        {
            this.receivedAllData = true;
            CloseStream();
        }

        public new void Dispose()
        {
            CloseStream();
            base.Dispose();
        }

        private void CloseStream()
        {
            string filePath = this.stream.Name;
            if (this.stream != null)
            {
                this.stream.Dispose();
                this.stream = null;
            }

            if (this.removeFileOnAbort && !this.receivedAllData && File.Exists(filePath))
            {
                // We have not successfully received all the content, so delete the file
                File.Delete(filePath);
            }
        }
    }
}