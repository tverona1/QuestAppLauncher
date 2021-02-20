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

using System.Collections.Specialized;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ControllerSelection
{
    public class OVRInputHelpers
    {
        public enum HandFilter
        {
            None = 0,
            Left = 1,
            Right = 2,
            Both = Left | Right
        }

        /// <summary>
        /// Indicates whether hand started pinching in this frame
        /// </summary>
        /// <param name="activeController"></param>
        /// <param name="finger"></param>
        /// <returns></returns>
        public static bool IsFingerStartPinching(OVRInput.Controller activeController, OVRHand.HandFinger finger)
        {
            bool isLeftHand = (activeController & OVRInput.Controller.LHand) == OVRInput.Controller.LHand;
            var curPinch = isLeftHand ? HandsManager.Instance.GetLeftPinchMode(finger) : HandsManager.Instance.GetRightPinchMode(finger);
            var lastPinch = isLeftHand ? HandsManager.Instance.GetLastLeftPinchMode(finger) : HandsManager.Instance.GetLastRightPinchMode(finger);

            return (lastPinch == HandsManager.PinchMode.NotPinched && curPinch == HandsManager.PinchMode.Pinched);
        }

        /// <summary>
        /// Indicates whether hand stopped pinching in this frame
        /// </summary>
        /// <param name="activeController"></param>
        /// <param name="finger"></param>
        /// <returns></returns>
        public static bool IsFingerStopPinching(OVRInput.Controller activeController, OVRHand.HandFinger finger)
        {
            bool isLeftHand = (activeController & OVRInput.Controller.LHand) == OVRInput.Controller.LHand;
            var curPinch = isLeftHand ? HandsManager.Instance.GetLeftPinchMode(finger) : HandsManager.Instance.GetRightPinchMode(finger);
            var lastPinch = isLeftHand ? HandsManager.Instance.GetLastLeftPinchMode(finger) : HandsManager.Instance.GetLastRightPinchMode(finger);

            return (lastPinch == HandsManager.PinchMode.Pinched && curPinch == HandsManager.PinchMode.NotPinched);
        }

        /// <summary>
        /// Indicates whether hand is pinching
        /// </summary>
        /// <param name="activeController"></param>
        /// <param name="finger"></param>
        /// <returns></returns>
        public static bool IsFingerPinching(OVRInput.Controller activeController, OVRHand.HandFinger finger)
        {
            bool isLeftHand = (activeController & OVRInput.Controller.LHand) == OVRInput.Controller.LHand;
            var curPinch = isLeftHand ? HandsManager.Instance.GetLeftPinchMode(finger) : HandsManager.Instance.GetRightPinchMode(finger);
            return (curPinch == HandsManager.PinchMode.Pinched);
        }

        /// <summary>
        /// Returns hand orientation
        /// </summary>
        /// <param name="factiveController"></param>
        /// <param name="orientation"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static bool GetHandOrientation(OVRInput.Controller activeController, out Quaternion orientation, out Vector3 position)
        {
            orientation = default(Quaternion);
            position = default(Vector3);

            if ((activeController & OVRInput.Controller.Hands) == OVRInput.Controller.None)
            {
                return false;
            }

            if (!HandsManager.Instance || !HandsManager.Instance.IsInitialized())
            {
                return false;
            }

            var hand = (activeController & OVRInput.Controller.RHand) == OVRInput.Controller.RHand ? HandsManager.Instance.RightHand : HandsManager.Instance.LeftHand;
            var isReliable = hand.IsTracked && hand.HandConfidence == OVRHand.TrackingConfidence.High;
            if (!isReliable || !hand.IsPointerPoseValid)
            {
                return false;
            }

            var pointer = hand.PointerPose;

            orientation = pointer.rotation;
            position = pointer.position;

            return true;
        }

        // Given a controller and tracking space, return the ray that controller uses.
        // Will fall back to center eye or camera on Gear if no controller is present.
        public static bool GetSelectionRay(OVRInput.Controller activeController, Transform trackingSpace, out Ray ray)
        {
            ray = default(Ray);
            if (trackingSpace != null && activeController != OVRInput.Controller.None)
            {
                Quaternion orientation = default(Quaternion);
                Vector3 localStartPoint = default(Vector3);

                if ((activeController & OVRInput.Controller.Hands) != OVRInput.Controller.None)
                {
                    if (!GetHandOrientation(activeController, out orientation, out localStartPoint))
                    {
                        return false;
                    }
                }
                else
                {
                    orientation = OVRInput.GetLocalControllerRotation(activeController);
                    localStartPoint = OVRInput.GetLocalControllerPosition(activeController);
                }

                Matrix4x4 localToWorld = trackingSpace.localToWorldMatrix;
                Vector3 worldStartPoint = localToWorld.MultiplyPoint(localStartPoint);
                Vector3 worldOrientation = localToWorld.MultiplyVector(orientation * Vector3.forward);

                ray = new Ray(worldStartPoint, worldOrientation);
                return true;
            }

            return false;
        }

        // Search the scene to find a tracking space. This method can be expensive! Try to avoid it if possible.
        public static Transform FindTrackingSpace() {
            // There should be an OVRManager in the scene
            if (OVRManager.instance != null) {
                Transform trackingSpace = OVRManager.instance.transform.Find("TrackingSpace");
                if (trackingSpace != null) {
                    return trackingSpace;
                }
            }

            Debug.LogWarning("OVRManager is not in scene, finding tracking space is going to be expensive!");

            // Look for any CameraRig objects
            OVRCameraRig[] cameraRigs = UnityEngine.Object.FindObjectsOfType(typeof(OVRCameraRig)) as OVRCameraRig[];
            foreach (OVRCameraRig cameraRig in cameraRigs) {
                if (cameraRig.gameObject.activeSelf) {
                    Transform trackingSpace = cameraRig.transform.Find("TrackingSpace");
                    if (trackingSpace != null) {
                        return trackingSpace;
                    }
                }
            }

            // Last resort, look for a tracking space
            GameObject trackingSpaceGO = UnityEngine.GameObject.Find("TrackingSpace");
            if (trackingSpaceGO != null) {
                return trackingSpaceGO.transform;
            }

            // Guess it doesn't exist
            return null;
        }

        /// <summary>
        /// Returns connected controllers
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static OVRInput.Controller GetConnectedControllers(HandFilter filter = HandFilter.Both)
        {
            OVRInput.Controller controller = OVRInput.GetConnectedControllers();

            if (((filter & HandFilter.Right) == HandFilter.Right) && (controller & OVRInput.Controller.RTouch) == OVRInput.Controller.RTouch)
            {
                return OVRInput.Controller.RTouch;
            }

            if (((filter & HandFilter.Left) == HandFilter.Left) && (controller & OVRInput.Controller.LTouch) == OVRInput.Controller.LTouch)
            {
                return OVRInput.Controller.LTouch;
            }

            controller = OVRInput.Controller.None;
            if (OVRPlugin.GetHandTrackingEnabled())
            {
                if ((filter & HandFilter.Both) == HandFilter.Both)
                {
                    return OVRInput.Controller.Hands;
                }
                else if ((filter & HandFilter.Right) == HandFilter.Right)
                {
                    return OVRInput.Controller.RHand;
                }
                else
                {
                    return OVRInput.Controller.LHand;
                }
            }

            return OVRInput.Controller.None;
        }
	}
}