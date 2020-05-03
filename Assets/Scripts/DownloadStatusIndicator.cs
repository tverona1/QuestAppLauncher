using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace QuestAppLauncher
{
    /// <summary>
    /// Script to display download status
    /// </summary>
    public class DownloadStatusIndicator : MonoBehaviour, IDownloadProgress
    {
        /// <summary>
        /// State of the download
        /// </summary>
        enum State
        {
            None,
            DownloadStart,
            DownloadProgress,
            DownloadProgressUpdated,
            DownloadFinish,
            Error,
            Checking,
            NoUpdates,
            UpdateFinished
        };

        // Text gui
        private TextMeshProUGUI textGui;

        // Current & previous state. The callback may be invoked from non-main thread, so we use a simple
        // state machine to track the state.
        private State prevState = State.None;
        private State state = State.None;
        private readonly object stateLock = new object();

        // State information
        private string fileName;
        private float progress;
        private string errorMessage;

        // Start is called before the first frame update
        void Start()
        {
            this.textGui = GetComponent<TextMeshProUGUI>();
        }

        // Update is called once per frame
        async void Update()
        {
            bool clearText = false;

            if (this.state == this.prevState)
            {
                return;
            }

            lock (this.stateLock)
            {
                // Update UI based on state
                switch (this.state)
                {
                    case State.DownloadStart:
                        this.textGui.text = string.Format("Downloading {0}", this.fileName);
                        break;
                    case State.DownloadFinish:
                        this.textGui.text = string.Format("Downloading {0} [100%]", this.fileName);
                        break;
                    case State.Checking:
                        this.textGui.text = string.Format("Checking for updates");
                        break;
                    case State.UpdateFinished:
                        // End state - display last error if available & clear text after a delay
                        if (null == this.errorMessage)
                        {
                            this.textGui.text = string.Format("Update complete");
                        }
                        else
                        {
                            this.textGui.text = this.errorMessage;
                        }
                        clearText = true;
                        break;
                    case State.NoUpdates:
                        // End state - display last error if available & clear text after a delay
                        if (null == this.errorMessage)
                        {
                            this.textGui.text = string.Format("No updates available");
                        }
                        else
                        {
                            this.textGui.text = this.errorMessage;
                        }
                        clearText = true;
                        break;
                    case State.Error:
                        this.textGui.text = this.errorMessage;
                        break;
                    case State.DownloadProgress:
                        this.textGui.text = string.Format("Downloading {0} [{1:0}%]", this.fileName, this.progress * 100);
                        this.state = State.DownloadProgressUpdated;
                        break;
                }

                this.prevState = this.state;
            }

            if (clearText)
            {
                await ClearText();
            }
        }

        public void OnDownloadStart(string fileName)
        {
            lock (this.stateLock)
            {
                this.fileName = fileName;
                this.state = State.DownloadStart;
            }
        }

        public void OnDownloadProgress(float progressPercentage, int totalContent, int receivedContent)
        {
            lock (this.stateLock)
            {
                this.state = State.DownloadProgress;
                this.progress = progressPercentage;
            }
        }

        public void OnDownloadFinish()
        {
            lock (this.stateLock)
            {
                this.state = State.DownloadFinish;
            }
        }

        public void OnError(string message)
        {
            lock (this.stateLock)
            {
                this.state = State.Error;
                this.errorMessage = message;
            }
        }

        public void OnNoUpdatesAvailable()
        {
            lock (this.stateLock)
            {
                this.state = State.NoUpdates;
            }
        }

        public void OnCheckingForUpdates()
        {
            lock (this.stateLock)
            {
                this.state = State.Checking;
            }
        }

        public void OnUpdateFinish()
        {
            lock (this.stateLock)
            {
                this.state = State.UpdateFinished;
            }
        }

        private async Task ClearText()
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            this.textGui.text = "";
        }
    }
}