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
using UnityEngine.SceneManagement;

namespace ControllerSelection {

    public class OVRPointerVisualizer : MonoBehaviour
    {
        public enum Hand
        {
            None,
            Left = OVRInputHelpers.HandFilter.Left,
            Right = OVRInputHelpers.HandFilter.Right
        }

        [Header("(Optional) Tracking space")]
        [Tooltip("Tracking space of the OVRCameraRig.\nIf tracking space is not set, the scene will be searched.\nThis search is expensive.")]
        public Transform trackingSpace = null;
        [Header("Visual Elements")]
        [Tooltip("Line Renderer used to draw selection ray.")]
        public LineRenderer linePointer = null;
        [Tooltip("Fallback gaze pointer.")]
        public Transform gazePointer = null;
        [Tooltip("Visually, how far out should the ray be drawn.")]
        public float rayDrawDistance = 500;
        [Tooltip("How far away the gaze pointer should be from the camera.")]
        public float gazeDrawDistance = 3;
        [Tooltip("Show gaze pointer as ray pointer.")]
        public bool showRayPointer = true;
        [Tooltip("Left or Right hand / controller")]
        public Hand handType = Hand.None;

        // Start ray draw distance
        private const float StartRayDrawDistance = 0.032f;

        void Awake()
        {
            if (trackingSpace == null)
            {
                Debug.LogWarning("OVRPointerVisualizer did not have a tracking space set. Looking for one");
                trackingSpace = OVRInputHelpers.FindTrackingSpace();
            }
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (trackingSpace == null)
            {
                Debug.LogWarning("OVRPointerVisualizer did not have a tracking space set. Looking for one");
                trackingSpace = OVRInputHelpers.FindTrackingSpace();
            }
        }

        void SetPointer(Ray ray)
        {
            float hitRayDrawDistance = rayDrawDistance;
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                hitRayDrawDistance = hit.distance;
            }

            if (linePointer != null)
            {
                linePointer.SetPosition(0, ray.origin + ray.direction * StartRayDrawDistance);
                linePointer.SetPosition(1, ray.origin + ray.direction * hitRayDrawDistance);
            }

            if (gazePointer != null)
            {
                gazePointer.position = ray.origin + ray.direction * (showRayPointer ? hitRayDrawDistance : gazeDrawDistance);
            }
        }

        void SetPointerVisibility(bool show)
        {
            if (show) {
                if (linePointer != null)
                {
                    linePointer.enabled = true;
                }
                if (gazePointer != null)
                {
                    gazePointer.gameObject.SetActive(showRayPointer ? true : false);
                }
            }
            else {
                if (linePointer != null)
                {
                    linePointer.enabled = false;
                }
                if (gazePointer != null)
                {
                    gazePointer.gameObject.SetActive(showRayPointer ? false : true);
                }
            }
        }

        void Update()
        {
            var activeController = OVRInputHelpers.GetConnectedControllers((OVRInputHelpers.HandFilter)handType);

            Ray selectionRay;
            bool gotRay = OVRInputHelpers.GetSelectionRay(activeController, trackingSpace, out selectionRay);
            SetPointerVisibility(gotRay && trackingSpace != null && activeController != OVRInput.Controller.None);

            if (gotRay)
            {
                SetPointer(selectionRay);
            }
        }
    }
}