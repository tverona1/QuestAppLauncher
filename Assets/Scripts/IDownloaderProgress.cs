namespace QuestAppLauncher
{
    /// <summary>
    /// Interface to determine download progress
    /// </summary>
    public interface IDownloadProgress
    {
        void OnCheckingForUpdates();
        void OnNoUpdatesAvailable();
        void OnUpdateFinish();

        void OnDownloadStart(string name);
        void OnDownloadProgress(float progressPercentage, int totalContent, int receivedContent);
        void OnDownloadFinish();

        void OnError(string message);
    }
}
