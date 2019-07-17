/************************************************************************************

Copyright   :   Copyright 2017-Present Oculus VR, LLC. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.2 (the "License");
you may not use the Oculus VR Rift SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculusvr.com/licenses/LICENSE-3.2

Unless required by applicable law or agreed to in writing, the Oculus VR SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace ControllerSelection {
    public class OVRRawRaycaster : MonoBehaviour {
        [System.Serializable]
        public class HoverCallback : UnityEvent<Transform> { }
        [System.Serializable]
        public class SelectionCallback : UnityEvent<Transform> { }

        [Header("(Optional) Tracking space")]
        [Tooltip("Tracking space of the OVRCameraRig.\nIf tracking space is not set, the scene will be searched.\nThis search is expensive.")]
        public Transform trackingSpace = null;


        [Header("Selection")]
        [Tooltip("Primary selection button")]
        public OVRInput.Button primaryButton = OVRInput.Button.PrimaryIndexTrigger;
        [Tooltip("Secondary selection button")]
        public OVRInput.Button secondaryButton = OVRInput.Button.PrimaryTouchpad;
        [Tooltip("Layers to exclude from raycast")]
        public LayerMask excludeLayers;
        [Tooltip("Maximum raycast distance")]
        public float raycastDistance = 500;

        [Header("Hover Callbacks")]
        public OVRRawRaycaster.HoverCallback onHoverEnter;
        public OVRRawRaycaster.HoverCallback onHoverExit;
        public OVRRawRaycaster.HoverCallback onHover;

        [Header("Selection Callbacks")]
        public OVRRawRaycaster.SelectionCallback onPrimarySelect;
        public OVRRawRaycaster.SelectionCallback onSecondarySelect;

        //protected Ray pointer;
        protected Transform lastHit = null;
        protected Transform triggerDown = null;
        protected Transform padDown = null;

        [HideInInspector]
        public OVRInput.Controller activeController = OVRInput.Controller.None;

        void Awake() {
            if (trackingSpace == null) {
                Debug.LogWarning("OVRRawRaycaster did not have a tracking space set. Looking for one");
                trackingSpace = OVRInputHelpers.FindTrackingSpace();
            }
        }

        void OnEnable() {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (trackingSpace == null) {
                Debug.LogWarning("OVRRawRaycaster did not have a tracking space set. Looking for one");
                trackingSpace = OVRInputHelpers.FindTrackingSpace();
            }
        }

        void Update() {
            activeController = OVRInputHelpers.GetControllerForButton(OVRInput.Button.PrimaryIndexTrigger, activeController);
            Ray pointer = OVRInputHelpers.GetSelectionRay(activeController, trackingSpace);

            RaycastHit hit; // Was anything hit?
            if (Physics.Raycast(pointer, out hit, raycastDistance, ~excludeLayers)) {
                if (lastHit != null && lastHit != hit.transform) {
                    if (onHoverExit != null) {
                        onHoverExit.Invoke(lastHit);
                    }
                    lastHit = null;
                }

                if (lastHit == null) {
                    if (onHoverEnter != null) {
                        onHoverEnter.Invoke(hit.transform);
                    }
                }

                if (onHover != null) {
                    onHover.Invoke(hit.transform);
                }

                lastHit = hit.transform;

                // Handle selection callbacks. An object is selected if the button selecting it was
                // pressed AND released while hovering over the object.
                if (activeController != OVRInput.Controller.None) {
                    if (OVRInput.GetDown(secondaryButton, activeController)) {
                        padDown = lastHit;
                    }
                    else if (OVRInput.GetUp(secondaryButton, activeController)) {
                        if (padDown != null && padDown == lastHit) {
                            if (onSecondarySelect != null) {
                                onSecondarySelect.Invoke(padDown);
                            }
                        }
                    }
                    if (!OVRInput.Get(secondaryButton, activeController)) {
                        padDown = null;
                    }

                    if (OVRInput.GetDown(primaryButton, activeController)) {
                        triggerDown = lastHit;
                    }
                    else if (OVRInput.GetUp(primaryButton, activeController)) {
                        if (triggerDown != null && triggerDown == lastHit) {
                            if (onPrimarySelect != null) {
                                onPrimarySelect.Invoke(triggerDown);
                            }
                        }
                    }
                    if (!OVRInput.Get(primaryButton, activeController)) {
                        triggerDown = null;
                    }
                }
#if UNITY_ANDROID && !UNITY_EDITOR
            // Gaze pointer fallback
            else {
                if (Input.GetMouseButtonDown(0) ) {
                    triggerDown = lastHit;
                }
                else if (Input.GetMouseButtonUp(0) ) {
                    if (triggerDown != null && triggerDown == lastHit) {
                        if (onPrimarySelect != null) {
                            onPrimarySelect.Invoke(triggerDown);
                        }
                    }
                }
                if (!Input.GetMouseButton(0)) {
                    triggerDown = null;
                }
            }
#endif
            }
            // Nothing was hit, handle exit callback
            else if (lastHit != null) {
                if (onHoverExit != null) {
                    onHoverExit.Invoke(lastHit);
                }
                lastHit = null;
            }
        }
    }
}