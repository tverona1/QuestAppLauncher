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

using System;
using System.Collections.Generic;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace ControllerSelection
{
    public class OVRRawRaycaster : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        public enum Hand
        {
            None,
            Left = OVRInputHelpers.HandFilter.Left,
            Right = OVRInputHelpers.HandFilter.Right
        }

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
        [Tooltip("Tertiary selection button")]
        public OVRInput.Button tertiaryButton = OVRInput.Button.One;
        [Tooltip("Primary pinch finger")]
        public OVRHand.HandFinger primaryPinchFinger = OVRHand.HandFinger.Index;
        [Tooltip("Secondary pinch finger")]
        public OVRHand.HandFinger secondaryPinchFinger = OVRHand.HandFinger.Middle;
        [Tooltip("Tertiary pinch finger")]
        public OVRHand.HandFinger tertiaryPinchFinger = OVRHand.HandFinger.Ring;
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
        public OVRRawRaycaster.SelectionCallback onTertiarySelect;

        //protected Ray pointer;
        protected Transform lastHitLeft = null;
        protected Transform lastHitRight = null;
        protected Transform lastButtonHitLeft = null;
        protected Transform lastButtonHitRight = null;
        protected Transform triggerButtonDownLeft = null;
        protected Transform triggerButtonDownRight = null;
        protected Transform padButtonDownLeft = null;
        protected Transform padButtonDownRight = null;
        protected Transform tertiaryButtonDownLeft = null;
        protected Transform tertiaryButtonDownRight = null;
        protected Transform triggerFingerDownLeft = null;
        protected Transform triggerFingerDownRight = null;
        protected Transform padFingerDownLeft = null;
        protected Transform padFingerDownRight = null;
        protected Transform tertiaryFingerDownLeft = null;
        protected Transform tertiaryFingerDownRight = null;

        // Hover actions
        [Flags]
        enum HoverAction
        {
            None = 0,
            Exit = 1,
            Enter = 2,
            Stay = 4,
            Hover = 8
        }

        // Map of transform to hover actions. We allocate this once & reuse to avoid memory allocations in Update().
        Dictionary<Transform, HoverAction> _actions = new Dictionary<Transform, HoverAction>(4);

        // Whether we're dragging
        bool _isDragging = false;

        void Awake()
        {
            if (trackingSpace == null)
            {
                Debug.LogWarning("OVRRawRaycaster did not have a tracking space set. Looking for one");
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
                Debug.LogWarning("OVRRawRaycaster did not have a tracking space set. Looking for one");
                trackingSpace = OVRInputHelpers.FindTrackingSpace();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
        }

        static void ProcessHit(Dictionary<Transform, HoverAction> actions, bool isHit, Transform hit, ref Transform lastHit)
        {
            if (isHit)
            {
                if (lastHit != null)
                {
                    if (lastHit != hit)
                    {
                        // We move out of the last hit, so action is Exit
                        actions[lastHit] |= HoverAction.Exit;
                        lastHit = null;
                    }
                    else
                    {
                        // We have not moved from last hit, so action is Stay
                        actions[lastHit] |= HoverAction.Stay;
                    }
                }

                if (lastHit == null)
                {
                    // We moved into a new hit, so action is Enter
                    actions[hit] |= HoverAction.Enter;
                }

                // We hit something, so action is hover
                actions[hit] |= HoverAction.Hover;
                lastHit = hit;
            }
            else if (lastHit != null)
            {
                // Nothing was hit, handle exit callback
                actions[lastHit] |= HoverAction.Exit;
                lastHit = null;
            }
        }

        void ProcessButtonPresses(OVRInput.Controller activeController, bool isLeft, Transform lastHit,
            ref Transform triggerDown, ref Transform padDown, ref Transform tertiaryDown)
        {
            // Handle selection callbacks. An object is selected if the button selecting it was
            // pressed AND released while hovering over the object.
            if (isLeft && (activeController & OVRInput.Controller.LTouch) != OVRInput.Controller.LTouch ||
                !isLeft && (activeController & OVRInput.Controller.RTouch) != OVRInput.Controller.RTouch)
            {
                return;
            }

            if (OVRInput.GetDown(tertiaryButton, activeController))
            {
                tertiaryDown = lastHit;
            }
            else if (OVRInput.GetUp(tertiaryButton, activeController))
            {
                if (tertiaryDown != null && tertiaryDown == lastHit)
                {
                    if (onTertiarySelect != null)
                    {
                        onTertiarySelect.Invoke(tertiaryDown);
                    }
                }
            }
            if (!OVRInput.Get(tertiaryButton, activeController))
            {
                tertiaryDown = null;
            }

            if (OVRInput.GetDown(secondaryButton, activeController))
            {
                padDown = lastHit;
            }
            else if (OVRInput.GetUp(secondaryButton, activeController))
            {
                if (padDown != null && padDown == lastHit)
                {
                    if (onSecondarySelect != null)
                    {
                        onSecondarySelect.Invoke(padDown);
                    }
                }
            }
            if (!OVRInput.Get(secondaryButton, activeController))
            {
                padDown = null;
            }

            if (OVRInput.GetDown(primaryButton, activeController))
            {
                triggerDown = lastHit;
            }
            else if (OVRInput.GetUp(primaryButton, activeController))
            {
                if (triggerDown != null && triggerDown == lastHit)
                {
                    if (onPrimarySelect != null)
                    {
                        onPrimarySelect.Invoke(triggerDown);
                    }
                }
            }
            if (!OVRInput.Get(primaryButton, activeController))
            {
                triggerDown = null;
            }
        }

        void ProcessHandPinch(OVRInput.Controller activeController, Transform lastHit,
            ref Transform triggerDown, ref Transform padDown, ref Transform tertiaryDown)
        {
            // Handle selection callbacks. An object is selected if the button selecting it was
            // pressed AND released while hovering over the object.
            if ((activeController & OVRInput.Controller.Hands) == OVRInput.Controller.None)
            {
                return;
            }

            if (!OVRPlugin.GetHandTrackingEnabled() || !HandsManager.Instance || !HandsManager.Instance.IsInitialized())
            {
                return;
            }

            if (OVRInputHelpers.IsFingerStartPinching(activeController, tertiaryPinchFinger))
            {
                tertiaryDown = lastHit;
            }
            else if (OVRInputHelpers.IsFingerStopPinching(activeController, tertiaryPinchFinger))
            {
                if (tertiaryDown != null && tertiaryDown == lastHit)
                {
                    if (onTertiarySelect != null)
                    {
                        onTertiarySelect.Invoke(tertiaryDown);
                    }
                }
            }
            else if (!OVRInputHelpers.IsFingerPinching(activeController, tertiaryPinchFinger))
            {
                tertiaryDown = null;
            }

            if (OVRInputHelpers.IsFingerStartPinching(activeController, secondaryPinchFinger))
            {
                padDown = lastHit;
            }
            else if (OVRInputHelpers.IsFingerStopPinching(activeController, secondaryPinchFinger))
            {
                if (padDown != null && padDown == lastHit)
                {
                    if (onSecondarySelect != null)
                    {
                        onSecondarySelect.Invoke(padDown);
                    }
                }
            }
            else if (!OVRInputHelpers.IsFingerPinching(activeController, secondaryPinchFinger))
            {
                padDown = null;
            }

            if (OVRInputHelpers.IsFingerStartPinching(activeController, primaryPinchFinger))
            {
                triggerDown = lastHit;
            }
            else if (OVRInputHelpers.IsFingerStopPinching(activeController, primaryPinchFinger))
            {
                if (triggerDown != null && triggerDown == lastHit)
                {
                    if (onPrimarySelect != null)
                    {
                        onPrimarySelect.Invoke(triggerDown);
                    }
                }
            }
            else if (!OVRInputHelpers.IsFingerPinching(activeController, primaryPinchFinger))
            {
                triggerDown = null;
            }
        }

        void Update()
        {
            Ray pointerLeft;
            Ray pointerRight;

            // Get left & right rays
            var activeControllerLeft = OVRInputHelpers.GetConnectedControllers(OVRInputHelpers.HandFilter.Left);
            var activeControllerRight = OVRInputHelpers.GetConnectedControllers(OVRInputHelpers.HandFilter.Right);

            bool gotRayLeft = OVRInputHelpers.GetSelectionRay(activeControllerLeft, trackingSpace, out pointerLeft);
            bool gotRayRight = OVRInputHelpers.GetSelectionRay(activeControllerRight, trackingSpace, out pointerRight);

            // Cast the rays
            RaycastHit hitLeft = default(RaycastHit);
            RaycastHit hitRight = default(RaycastHit);
            bool isHitLeft = gotRayLeft && Physics.Raycast(pointerLeft, out hitLeft, raycastDistance, ~excludeLayers);
            bool isHitRight = gotRayRight && Physics.Raycast(pointerRight, out hitRight, raycastDistance, ~excludeLayers);

            // Process the hits
            _actions.Clear();

            if (lastHitLeft != null)
            {
                _actions[lastHitLeft] = HoverAction.None;
            }

            if (lastHitRight != null)
            {
                _actions[lastHitRight] = HoverAction.None;
            }

            if (isHitLeft)
            {
                _actions[hitLeft.transform] = HoverAction.None;
            }

            if (isHitRight)
            {
                _actions[hitRight.transform] = HoverAction.None;
            }

            ProcessHit(_actions, isHitLeft, hitLeft.transform, ref lastHitLeft);
            ProcessHit(_actions, isHitRight, hitRight.transform, ref lastHitRight);

            // Perform the actions
            foreach (KeyValuePair<Transform, HoverAction> kvp in _actions)
            {
                // Only perform enter / exit actions if we are not staying on existing transform
                // and if we're not both entering & exiting
                if ((kvp.Value & HoverAction.Stay) == HoverAction.None &&
                    (kvp.Value & (HoverAction.Enter | HoverAction.Exit)) != (HoverAction.Enter | HoverAction.Exit))
                {
                    if ((kvp.Value & HoverAction.Enter) != HoverAction.None && onHoverEnter != null)
                    {
                        onHoverEnter.Invoke(kvp.Key);
                    }

                    if ((kvp.Value & HoverAction.Exit) != HoverAction.None && onHoverExit != null)
                    {
                        onHoverExit.Invoke(kvp.Key);
                    }
                }

                if ((kvp.Value & HoverAction.Hover) != HoverAction.None && onHover != null)
                {
                    // Hovering over transform
                    onHover.Invoke(kvp.Key);
                }
            }

            if (isHitLeft && !_isDragging)
            { 
                ProcessButtonPresses(activeControllerLeft, true, lastHitLeft, ref triggerButtonDownLeft, ref padButtonDownLeft, ref tertiaryButtonDownLeft);
                ProcessHandPinch(activeControllerLeft, lastHitLeft, ref triggerFingerDownLeft, ref padFingerDownLeft, ref tertiaryFingerDownLeft);
            }

            if (isHitRight && !_isDragging)
            {
                ProcessButtonPresses(activeControllerRight, false, lastHitRight, ref triggerButtonDownRight, ref padButtonDownRight, ref tertiaryButtonDownRight);
                ProcessHandPinch(activeControllerRight, lastHitRight, ref triggerFingerDownRight, ref padFingerDownRight, ref tertiaryFingerDownRight);
            }
        }
    }
}