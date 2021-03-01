using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace QuestAppLauncher
{
    /// <summary>
    /// Text download handler
    /// </summary>
    public class DownloadHandlerText : DownloadHandler
    {
        private MemoryStream memoryStream = new MemoryStream();

        public String text
        {
            get
            {
                using (var reader = new StreamReader(this.memoryStream, Encoding.UTF8))
                {
                    this.memoryStream.Seek(0, SeekOrigin.Begin);
                    return reader.ReadToEnd();
                }
            }
        }


        public DownloadHandlerText(int bufferSize = 32768) : base(new byte[bufferSize])
        {
        }

        protected override float GetProgress()
        {
            return 0;
        }

        public override void ReceiveContentLength(int contentLength)
        {
        }

        public override async Task<bool> ReceiveData(byte[] data, int dataLength)
        {
            await this.memoryStream.WriteAsync(data, 0, dataLength);
            return true;
        }

        public override void CompleteContent()
        {
        }

        public void Dispose()
        {
        }
    }
}
