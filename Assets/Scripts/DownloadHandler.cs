using System;
using System.Threading.Tasks;

namespace QuestAppLauncher
{
    /// <summary>
    /// File download handler class with callback for progress.
    /// This is a download hanlder that can be assocaited with a UnityWebRequest.
    /// Based on: DownloadHandlerFile.cs by Luke Holland
    /// (https://gist.github.com/luke161/a251b01c00f58d65a252812be8dce670)
    /// </summary>
    public abstract class DownloadHandler : IDisposable
    {
        public byte[] buffer
        {
            private set;
            get;
        }
 
        public DownloadHandler(byte[] buffer)
        {
            this.buffer = buffer;
        }

        protected abstract float GetProgress();

        public abstract void ReceiveContentLength(int contentLength);

        public abstract Task<bool> ReceiveData(byte[] data, int dataLength);

        public abstract void CompleteContent();

        public void Dispose()
        {
        }
    }
}
