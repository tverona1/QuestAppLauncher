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

namespace ControllerSelection {
    public class OVRInputHelpers {
        // Given a controller and tracking spcae, return the ray that controller uses.
        // Will fall back to center eye or camera on Gear if no controller is present.
        public static Ray GetSelectionRay(OVRInput.Controller activeController, Transform trackingSpace) {
            if (trackingSpace != null && activeController != OVRInput.Controller.None) {
                Quaternion orientation = OVRInput.GetLocalControllerRotation(activeController);
                Vector3 localStartPoint = OVRInput.GetLocalControllerPosition(activeController);

                Matrix4x4 localToWorld = trackingSpace.localToWorldMatrix;
                Vector3 worldStartPoint = localToWorld.MultiplyPoint(localStartPoint);
                Vector3 worldOrientation = localToWorld.MultiplyVector(orientation * Vector3.forward);

                return new Ray(worldStartPoint, worldOrientation);
            }

            Transform cameraTransform = Camera.main.transform;

            if (OVRManager.instance != null) {
                OVRCameraRig cameraRig = OVRManager.instance.GetComponent<OVRCameraRig>();
                if (cameraRig != null) {
                    cameraTransform = cameraRig.centerEyeAnchor;
                }
            }

            return new Ray(cameraTransform.position, cameraTransform.forward);
        }

        // Search the scene to find a tracking spce. This method can be expensive! Try to avoid it if possible.
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

        // Find the current active controller, based on last time a certain button was hit. Needs to know the previous active controller.
        public static OVRInput.Controller GetControllerForButton(OVRInput.Button joyPadClickButton, OVRInput.Controller oldController) {
            OVRInput.Controller controller = OVRInput.GetConnectedControllers();

            if ((controller & OVRInput.Controller.RTouch) == OVRInput.Controller.RTouch) {
                if (OVRInput.Get(joyPadClickButton, OVRInput.Controller.RTouch) || oldController == OVRInput.Controller.None) {
                    return OVRInput.Controller.RTouch;
                }
            }

            if ((controller & OVRInput.Controller.LTouch) == OVRInput.Controller.LTouch) {
                if (OVRInput.Get(joyPadClickButton, OVRInput.Controller.LTouch) || oldController == OVRInput.Controller.None) {
                    return OVRInput.Controller.LTouch;
                }
            }

            if ((controller & OVRInput.Controller.RTrackedRemote) == OVRInput.Controller.RTrackedRemote) {
                if (OVRInput.Get(joyPadClickButton, OVRInput.Controller.RTrackedRemote) || oldController == OVRInput.Controller.None) {
                    return OVRInput.Controller.RTrackedRemote;
                }
            }

            if ((controller & OVRInput.Controller.LTrackedRemote) == OVRInput.Controller.LTrackedRemote) {
                if (OVRInput.Get(joyPadClickButton, OVRInput.Controller.LTrackedRemote) || oldController == OVRInput.Controller.None) {
                    return OVRInput.Controller.LTrackedRemote;
                }
            }

            if ((controller & oldController) != oldController) {
                return OVRInput.Controller.None;
            }

            return oldController;
        }
    }
}