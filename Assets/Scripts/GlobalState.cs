using UnityEngine;
using System.Collections;

namespace QuestAppLauncher
{
    /// <summary>
    /// Simple class that holds state that survives scene reloads.
    /// Follows Unity's DontDestroyOnLoad singleton pattern.
    /// </summary>
    public class GlobalState : MonoBehaviour
    {
        public static GlobalState Instance;

        // State to indicate whether we already checked for update
        public bool CheckedForUpdate = false;

        void Awake()
        {
            this.InstantiateController();
        }

        private void InstantiateController()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else if (this != Instance)
            {
                Destroy(this.gameObject);
            }
        }
    }
}