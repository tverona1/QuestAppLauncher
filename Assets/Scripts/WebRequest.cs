using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace QuestAppLauncher
{
    /// <summary>
    /// WebRequest helper
    /// </summary>
    public class WebRequest : IDisposable
    {
        private HttpClient client;
        private HttpResponseMessage response;
        private string url;

        public DownloadHandler downloadHandler
        {
            get;
            set;
        }

        public bool isNetworkError
        {
            get;
            private set;
        }

        public bool isHttpError
        {
            get;
            private set;
        }

        public string error
        {
            get;
            private set;
        }

        public WebRequest(string url)
        {
            this.url = url;
            this.client = new HttpClient();
            this.client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "WebRequest");
        }

        public void Dispose()
        {
            response?.Dispose();
            response = null;
        }

        public void SetRequestHeader(string header, string value)
        {
            this.client.DefaultRequestHeaders.Add(header, value);
        }

        public HttpResponseHeaders GetResponseHeaders()
        {
            return this.response?.Headers;
        }

        public async Task SendWebRequest() {
            if (this.downloadHandler == null)
            {
                throw new System.Exception("Null download handler");
            }

            try
            {
                using (this.response = client.GetAsync(this.url, HttpCompletionOption.ResponseHeadersRead).Result)
                {
                    this.isHttpError = !this.response.IsSuccessStatusCode;
                    if (this.isHttpError)
                    {
                        this.error = $"Http status error: {this.response.StatusCode}";
                        return;
                    }

                    this.downloadHandler.ReceiveContentLength((int?)this.response.Content.Headers.ContentLength ?? -1);
                    using (var contentStream = await this.response.Content.ReadAsStreamAsync())
                    {
                        do
                        {
                            var read = await contentStream.ReadAsync(this.downloadHandler.buffer, 0, this.downloadHandler.buffer.Length);
                            if (0 == read)
                            {
                                break;
                            }
                            var ret = await this.downloadHandler.ReceiveData(this.downloadHandler.buffer, read);
                            if (!ret)
                            {
                                break;
                            }
                        }
                        while (true);
                    }

                    this.downloadHandler.CompleteContent();
                }
            }
            catch (HttpRequestException httpException)
            {
                this.isNetworkError = true;
                this.error = httpException.Message;
            }
        }
    }
}