/************************************************************************************

Copyright   :   Copyright 2014-Present Oculus VR, LLC. All Rights reserved.

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
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace ControllerSelection 
{
    public class OVRInputModule : UnityEngine.EventSystems.PointerInputModule
    {
		protected override void Awake() {
            base.Awake();
			if (trackingSpace == null) {
				Debug.LogWarning ("OVRInputModule did not have a tracking space set. Looking for one");
				trackingSpace = OVRInputHelpers.FindTrackingSpace ();
			}
		}

        protected override void OnEnable()
		{
            base.OnEnable();
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

        protected override void OnDisable()
		{
            base.OnDisable();

            SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if (trackingSpace == null) {
				Debug.LogWarning ("OVRInputModule did not have a tracking space set. Looking for one");
				trackingSpace = OVRInputHelpers.FindTrackingSpace ();
			}
		}

        [Header("(Optional) Tracking space")]
        [Tooltip("Tracking space of the OVRCameraRig.\nIf tracking space is not set, the scene will be searched.\nThis search is expensive.")]
        public Transform trackingSpace = null;

        [Header("Selection")]
        [Tooltip("Primary selection button")]
        public OVRInput.Button joyPadClickButton = OVRInput.Button.PrimaryIndexTrigger;

        [Header("Physics")]
        [Tooltip("Perform an sphere cast to determine correct depth for gaze pointer")]
        public bool performSphereCastForGazepointer;

        [Tooltip("Match the gaze pointer normal to geometry normal for physics colliders")]
        public bool matchNormalOnPhysicsColliders;

        [Header("Gamepad Stick Scroll")]
        [Tooltip("Enable scrolling with the left stick on a gamepad")]
        public bool useLeftStickScroll = true;

        [Tooltip("Deadzone for left stick to prevent accidental scrolling")]
        public float leftStickDeadZone = 0.15f;

        [Header("Touchpad Swipe Scroll")]
        [Tooltip("Enable scrolling by swiping the GearVR touchpad")]
        public bool useSwipeScroll = true;
        [Tooltip("Minimum swipe amount to trigger scrolling")]
        public float minSwipeMovement = 0;
        [Tooltip("Distance scrolled when swipe scroll occurs")]
        public float swipeScrollScale = 4f;

        
		[HideInInspector]
		public OVRInput.Controller activeController = OVRInput.Controller.None;

		public delegate void RayHitDelegate(Vector3 hitPosition, Vector3 hitNormal);
        public RayHitDelegate OnSelectionRayHit;


        #region GearVR swipe scroll
        private Vector2 swipeStartPos;
        private Vector2 unusedSwipe;
        #endregion

        // The raycaster that gets to do pointer interaction (e.g. with a mouse), gaze interaction always works
       // private OVRRaycaster _activeGraphicRaycaster;
        [NonSerialized]
		public OVRRaycaster activeGraphicRaycaster;
        [Header("Dragging")]
        [Tooltip("Minimum pointer movement in degrees to start dragging")]
        public float angleDragThreshold = 1;

        // The following region contains code exactly the same as the implementation
        // of StandaloneInputModule. It is copied here rather than inheriting from StandaloneInputModule
        // because most of StandaloneInputModule is private so it isn't possible to easily derive from.
        // Future changes from Unity to StandaloneInputModule will make it possible for this class to
        // derive from StandaloneInputModule instead of PointerInput module.
        // 
        // The following functions are not present in the following region since they have modified
        // versions in the next region:
        // Process
        // ProcessMouseEvent
        // UseMouse
        #region StandaloneInputModule code
        
         private float m_NextAction;

        private Vector2 m_LastMousePosition;
        private Vector2 m_MousePosition;

        protected OVRInputModule()
        {}

        protected new void Reset()
        {
            allowActivationOnMobileDevice = true;
        }

        [Obsolete("Mode is no longer needed on input module as it handles both mouse and keyboard simultaneously.", false)]
        public enum InputMode
        {
            Mouse,
            Buttons
        }

        [Obsolete("Mode is no longer needed on input module as it handles both mouse and keyboard simultaneously.", false)]
        public InputMode inputMode
        {
            get { return InputMode.Mouse; }
        }
        [Header("Standalone Input Module")]
        [SerializeField]
        private string m_HorizontalAxis = "Horizontal";

        /// <summary>
        /// Name of the vertical axis for movement (if axis events are used).
        /// </summary>
        [SerializeField]
        private string m_VerticalAxis = "Vertical";

        /// <summary>
        /// Name of the submit button.
        /// </summary>
        [SerializeField]
        private string m_SubmitButton = "Submit";

        /// <summary>
        /// Name of the submit button.
        /// </summary>
        [SerializeField]
        private string m_CancelButton = "Cancel";

        [SerializeField]
        private float m_InputActionsPerSecond = 10;

        [SerializeField]
        private bool m_AllowActivationOnMobileDevice;

        public bool allowActivationOnMobileDevice
        {
            get { return m_AllowActivationOnMobileDevice; }
            set { m_AllowActivationOnMobileDevice = value; }
        }

        public float inputActionsPerSecond
        {
            get { return m_InputActionsPerSecond; }
            set { m_InputActionsPerSecond = value; }
        }

        /// <summary>
        /// Name of the horizontal axis for movement (if axis events are used).
        /// </summary>
        public string horizontalAxis
        {
            get { return m_HorizontalAxis; }
            set { m_HorizontalAxis = value; }
        }

        /// <summary>
        /// Name of the vertical axis for movement (if axis events are used).
        /// </summary>
        public string verticalAxis
        {
            get { return m_VerticalAxis; }
            set { m_VerticalAxis = value; }
        }

        public string submitButton
        {
            get { return m_SubmitButton; }
            set { m_SubmitButton = value; }
        }

        public string cancelButton
        {
            get { return m_CancelButton; }
            set { m_CancelButton = value; }
        }

        public override void UpdateModule()
        {
			activeController = OVRInputHelpers.GetControllerForButton (OVRInput.Button.PrimaryIndexTrigger, activeController);

            m_LastMousePosition = m_MousePosition;
            m_MousePosition = Input.mousePosition;
        }

        public override bool IsModuleSupported()
        {
            // Check for mouse presence instead of whether touch is supported,
            // as you can connect mouse to a tablet and in that case we'd want
            // to use StandaloneInputModule for non-touch input events.
            return m_AllowActivationOnMobileDevice || Input.mousePresent;
        }

        public override bool ShouldActivateModule()
        {
            if (!base.ShouldActivateModule())
                return false;

            var shouldActivate = Input.GetButtonDown(m_SubmitButton);
            shouldActivate |= Input.GetButtonDown(m_CancelButton);
            shouldActivate |= !Mathf.Approximately(Input.GetAxisRaw(m_HorizontalAxis), 0.0f);
            shouldActivate |= !Mathf.Approximately(Input.GetAxisRaw(m_VerticalAxis), 0.0f);
            shouldActivate |= (m_MousePosition - m_LastMousePosition).sqrMagnitude > 0.0f;
            shouldActivate |= Input.GetMouseButtonDown(0);
            return shouldActivate;
        }

        public override void ActivateModule()
        {
            base.ActivateModule();
            m_MousePosition = Input.mousePosition;
            m_LastMousePosition = Input.mousePosition;

            var toSelect = eventSystem.currentSelectedGameObject;
            if (toSelect == null)
                toSelect = eventSystem.firstSelectedGameObject;

            eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
        }

        public override void DeactivateModule()
        {
            base.DeactivateModule();
            ClearSelection();
        }

        

        /// <summary>
        /// Process submit keys.
        /// </summary>
        private bool SendSubmitEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;

            var data = GetBaseEventData();
            if (Input.GetButtonDown(m_SubmitButton))
                UnityEngine.EventSystems.ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, UnityEngine.EventSystems.ExecuteEvents.submitHandler);

            if (Input.GetButtonDown(m_CancelButton))
                UnityEngine.EventSystems.ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, UnityEngine.EventSystems.ExecuteEvents.cancelHandler);
            return data.used;
        }

        private bool AllowMoveEventProcessing(float time)
        {
            bool allow = Input.GetButtonDown(m_HorizontalAxis);
            allow |= Input.GetButtonDown(m_VerticalAxis);
            allow |= (time > m_NextAction);
            return allow;
        }

        private Vector2 GetRawMoveVector()
        {
            Vector2 move = Vector2.zero;
            move.x = Input.GetAxisRaw(m_HorizontalAxis);
            move.y = Input.GetAxisRaw(m_VerticalAxis);

            if (Input.GetButtonDown(m_HorizontalAxis))
            {
                if (move.x < 0)
                    move.x = -1f;
                if (move.x > 0)
                    move.x = 1f;
            }
            if (Input.GetButtonDown(m_VerticalAxis))
            {
                if (move.y < 0)
                    move.y = -1f;
                if (move.y > 0)
                    move.y = 1f;
            }
            return move;
        }

        /// <summary>
        /// Process keyboard events.
        /// </summary>
        private bool SendMoveEventToSelectedObject()
        {
            float time = Time.unscaledTime;

            if (!AllowMoveEventProcessing(time))
                return false;

            Vector2 movement = GetRawMoveVector();
            // Debug.Log(m_ProcessingEvent.rawType + " axis:" + m_AllowAxisEvents + " value:" + "(" + x + "," + y + ")");
            var axisEventData = GetAxisEventData(movement.x, movement.y, 0.6f);
            if (!Mathf.Approximately(axisEventData.moveVector.x, 0f)
                || !Mathf.Approximately(axisEventData.moveVector.y, 0f))
            {
                UnityEngine.EventSystems.ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, axisEventData, UnityEngine.EventSystems.ExecuteEvents.moveHandler);
            }
            m_NextAction = time + 1f / m_InputActionsPerSecond;
            return axisEventData.used;
        }

        

        

        private bool SendUpdateEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;

            var data = GetBaseEventData();
            UnityEngine.EventSystems.ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, UnityEngine.EventSystems.ExecuteEvents.updateSelectedHandler);
            return data.used;
        }

        /// <summary>
        /// Process the current mouse press.
        /// </summary>
        private void ProcessMousePress(MouseButtonEventData data)
        {
            var pointerEvent = data.buttonData;
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if (data.PressedThisFrame())
            {
                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pressPosition = pointerEvent.position;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;
                
                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                var newPressed = UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);

                // didnt find a press handler... search for a click handler
                if (newPressed == null)
                    newPressed = UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(currentOverGo);

                // Debug.Log("Pressed: " + newPressed);

                float time = Time.unscaledTime;

                if (newPressed == pointerEvent.lastPress)
                {
                    var diffTime = time - pointerEvent.clickTime;
                    if (diffTime < 0.3f)
                        ++pointerEvent.clickCount;
                    else
                        pointerEvent.clickCount = 1;

                    pointerEvent.clickTime = time;
                }
                else
                {
                    pointerEvent.clickCount = 1;
                }

                pointerEvent.pointerPress = newPressed;
                pointerEvent.rawPointerPress = currentOverGo;

                pointerEvent.clickTime = time;

                // Save the drag handler as well
                pointerEvent.pointerDrag = UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IDragHandler>(currentOverGo);

                if (pointerEvent.pointerDrag != null)
                    UnityEngine.EventSystems.ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, UnityEngine.EventSystems.ExecuteEvents.initializePotentialDrag);
            }

            // PointerUp notification
            if (data.ReleasedThisFrame())
            {
                // Debug.Log("Executing pressup on: " + pointer.pointerPress);
                UnityEngine.EventSystems.ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);

                // Debug.Log("KeyCode: " + pointer.eventData.keyCode);

                // see if we mouse up on the same element that we clicked on...
                var pointerUpHandler = UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(currentOverGo);

                // PointerClick and Drop events
                if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
                {
                    UnityEngine.EventSystems.ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                }
                else if (pointerEvent.pointerDrag != null)
                {
                    UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, UnityEngine.EventSystems.ExecuteEvents.dropHandler);
                }

                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.rawPointerPress = null;

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                    UnityEngine.EventSystems.ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, UnityEngine.EventSystems.ExecuteEvents.endDragHandler);

                pointerEvent.dragging = false;
                pointerEvent.pointerDrag = null;

                // redo pointer enter / exit to refresh state
                // so that if we moused over somethign that ignored it before
                // due to having pressed on something else
                // it now gets it.
                if (currentOverGo != pointerEvent.pointerEnter)
                {
                    HandlePointerExitAndEnter(pointerEvent, null);
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                }
            }
        }
#endregion
#region Modified StandaloneInputModule methods
        
        /// <summary>
        /// Process all mouse events. This is the same as the StandaloneInputModule version except that
        /// it takes MouseState as a parameter, allowing it to be used for both Gaze and Mouse 
        /// pointerss.
        /// </summary>
        private void ProcessMouseEvent(MouseState mouseData)
        {
            var pressed = mouseData.AnyPressesThisFrame();
            var released = mouseData.AnyReleasesThisFrame();

            var leftButtonData = mouseData.GetButtonState(UnityEngine.EventSystems.PointerEventData.InputButton.Left).eventData;

            if (!UseMouse(pressed, released, leftButtonData.buttonData))
                return;

            // Process the first mouse button fully
            ProcessMousePress(leftButtonData);
            ProcessMove(leftButtonData.buttonData);
            ProcessDrag(leftButtonData.buttonData);

            // Now process right / middle clicks
            ProcessMousePress(mouseData.GetButtonState(UnityEngine.EventSystems.PointerEventData.InputButton.Right).eventData);
            ProcessDrag(mouseData.GetButtonState(UnityEngine.EventSystems.PointerEventData.InputButton.Right).eventData.buttonData);
            ProcessMousePress(mouseData.GetButtonState(UnityEngine.EventSystems.PointerEventData.InputButton.Middle).eventData);
            ProcessDrag(mouseData.GetButtonState(UnityEngine.EventSystems.PointerEventData.InputButton.Middle).eventData.buttonData);

            if (!Mathf.Approximately(leftButtonData.buttonData.scrollDelta.sqrMagnitude, 0.0f))
            {
                var scrollHandler = UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IScrollHandler>(leftButtonData.buttonData.pointerCurrentRaycast.gameObject);
                UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(scrollHandler, leftButtonData.buttonData, UnityEngine.EventSystems.ExecuteEvents.scrollHandler);
            }
        }
        
        /// <summary>
        /// Process this InputModule. Same as the StandaloneInputModule version, except that it calls
        /// ProcessMouseEvent twice, once for gaze pointers, and once for mouse pointers.
        /// </summary>
        public override void Process()
        {
            bool usedEvent = SendUpdateEventToSelectedObject();

            if (eventSystem.sendNavigationEvents)
            {
                if (!usedEvent)
                    usedEvent |= SendMoveEventToSelectedObject();

                if (!usedEvent)
                    SendSubmitEventToSelectedObject();
            }

            ProcessMouseEvent(GetGazePointerData());
#if !UNITY_ANDROID
            ProcessMouseEvent(GetCanvasPointerData());
#endif
        }
        /// <summary>
        /// Decide if mouse events need to be processed this frame. Same as StandloneInputModule except
        /// that the IsPointerMoving method from this class is used, instead of the method on PointerEventData
        /// </summary>
       private static bool UseMouse(bool pressed, bool released, UnityEngine.EventSystems.PointerEventData pointerData)
        {
            if (pressed || released || IsPointerMoving(pointerData) || pointerData.IsScrolling())
                return true;

            return false;
        }
#endregion

        
        /// <summary>
        /// Convenience function for cloning PointerEventData
        /// </summary>
        /// <param name="from">Copy this value</param>
        /// <param name="to">to this object</param>
        protected void CopyFromTo(OVRRayPointerEventData @from, OVRRayPointerEventData @to)
        {
            @to.position = @from.position;
            @to.delta = @from.delta;
            @to.scrollDelta = @from.scrollDelta;
            @to.pointerCurrentRaycast = @from.pointerCurrentRaycast;
            @to.pointerEnter = @from.pointerEnter;
            @to.worldSpaceRay = @from.worldSpaceRay;
        }
        /// <summary>
        /// Convenience function for cloning PointerEventData
        /// </summary>
        /// <param name="from">Copy this value</param>
        /// <param name="to">to this object</param>
        protected new void CopyFromTo(UnityEngine.EventSystems.PointerEventData @from, UnityEngine.EventSystems.PointerEventData @to)
        {
            @to.position = @from.position;
            @to.delta = @from.delta;
            @to.scrollDelta = @from.scrollDelta;
            @to.pointerCurrentRaycast = @from.pointerCurrentRaycast;
            @to.pointerEnter = @from.pointerEnter;
        }
        

        // In the following region we extend the PointerEventData system implemented in PointerInputModule
        // We define an additional dictionary for ray(e.g. gaze) based pointers. Mouse pointers still use the dictionary
        // in PointerInputModule
#region PointerEventData pool

        // Pool for OVRRayPointerEventData for ray based pointers
        protected Dictionary<int, OVRRayPointerEventData> m_VRRayPointerData = new Dictionary<int, OVRRayPointerEventData>();

        
        protected bool GetPointerData(int id, out OVRRayPointerEventData data, bool create)
        {
            if (!m_VRRayPointerData.TryGetValue(id, out data) && create)
            {
                data = new OVRRayPointerEventData(eventSystem)
                {
                    pointerId = id,
                };

                m_VRRayPointerData.Add(id, data);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clear pointer state for both types of pointer
        /// </summary>
        protected new void ClearSelection()
        {
            var baseEventData = GetBaseEventData();

            foreach (var pointer in m_PointerData.Values)
            {
                // clear all selection
                HandlePointerExitAndEnter(pointer, null);
            }
            foreach (var pointer in m_VRRayPointerData.Values)
            {
                // clear all selection
                HandlePointerExitAndEnter(pointer, null);
            }

            m_PointerData.Clear();
            eventSystem.SetSelectedGameObject(null, baseEventData);
        }
#endregion

        /// <summary>
        /// For RectTransform, calculate it's normal in world space
        /// </summary>
        static Vector3 GetRectTransformNormal(RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector3 BottomEdge = corners[3] - corners[0];
            Vector3 LeftEdge = corners[1] - corners[0];
            rectTransform.GetWorldCorners(corners);
            return Vector3.Cross(LeftEdge, BottomEdge).normalized;
        }
       
        private readonly MouseState m_MouseState = new MouseState();
        // Overridden so that we can process the two types of pointer separately


        // The following 2 functions are equivalent to PointerInputModule.GetMousePointerEventData but are customized to
        // get data for ray pointers and canvas mouse pointers.
        
        /// <summary>
        /// State for a pointer controlled by a world space ray. E.g. gaze pointer
        /// </summary>
        /// <returns></returns>
        protected MouseState GetGazePointerData()
        {
            // Get the OVRRayPointerEventData reference
            OVRRayPointerEventData leftData;
            GetPointerData(kMouseLeftId, out leftData, true );
            leftData.Reset();


			leftData.worldSpaceRay = OVRInputHelpers.GetSelectionRay(activeController, trackingSpace);
            leftData.scrollDelta = GetExtraScrollDelta();

            //Populate some default values
            leftData.button = UnityEngine.EventSystems.PointerEventData.InputButton.Left;
            leftData.useDragThreshold = true;
            // Perform raycast to find intersections with world
            eventSystem.RaycastAll(leftData, m_RaycastResultCache);
            var raycast = FindFirstRaycast(m_RaycastResultCache);
            leftData.pointerCurrentRaycast = raycast;
            m_RaycastResultCache.Clear();

			OVRRaycaster ovrRaycaster = raycast.module as OVRRaycaster;
            // We're only interested in intersections from OVRRaycasters
            if (ovrRaycaster) 
            {
                // The Unity UI system expects event data to have a screen position
                // so even though this raycast came from a world space ray we must get a screen
                // space position for the camera attached to this raycaster for compatability
                leftData.position = ovrRaycaster.GetScreenPosition(raycast);
                

                // Find the world position and normal the Graphic the ray intersected
                RectTransform graphicRect = raycast.gameObject.GetComponent<RectTransform>();
                if (graphicRect != null)
                {
                    // Set are gaze indicator with this world position and normal
                   // Vector3 worldPos = raycast.worldPosition;
                    //Vector3 normal = GetRectTransformNormal(graphicRect);
					
                    if (OnSelectionRayHit != null) {
                        OnSelectionRayHit(raycast.worldPosition, raycast.worldNormal);
                    }
                }
            }
            OVRPhysicsRaycaster physicsRaycaster = raycast.module as OVRPhysicsRaycaster;
            if (physicsRaycaster)
            {
                leftData.position = physicsRaycaster.GetScreenPos(raycast.worldPosition);

                if (OnSelectionRayHit != null) {
                    OnSelectionRayHit(raycast.worldPosition, raycast.worldNormal);
                }
            }

            // Stick default data values in right and middle slots for compatability

            // copy the apropriate data into right and middle slots
            OVRRayPointerEventData rightData;
            GetPointerData(kMouseRightId, out rightData, true );
            CopyFromTo(leftData, rightData);
            rightData.button = UnityEngine.EventSystems.PointerEventData.InputButton.Right;

            OVRRayPointerEventData middleData;
            GetPointerData(kMouseMiddleId, out middleData, true );
            CopyFromTo(leftData, middleData);
            middleData.button = UnityEngine.EventSystems.PointerEventData.InputButton.Middle;


            m_MouseState.SetButtonState(UnityEngine.EventSystems.PointerEventData.InputButton.Left, GetGazeButtonState(), leftData);
            m_MouseState.SetButtonState(UnityEngine.EventSystems.PointerEventData.InputButton.Right, UnityEngine.EventSystems.PointerEventData.FramePressState.NotChanged, rightData);
            m_MouseState.SetButtonState(UnityEngine.EventSystems.PointerEventData.InputButton.Middle, UnityEngine.EventSystems.PointerEventData.FramePressState.NotChanged, middleData);
            return m_MouseState;
        }

        /// <summary>
        /// Get state for pointer which is a pointer moving in world space across the surface of a world space canvas.
        /// </summary>
        /// <returns></returns>
        protected MouseState GetCanvasPointerData()
        {
            // Get the OVRRayPointerEventData reference
            UnityEngine.EventSystems.PointerEventData leftData;
            GetPointerData(kMouseLeftId, out leftData, true );
            leftData.Reset();
            
            // Setup default values here. Set position to zero because we don't actually know the pointer
            // positions. Each canvas knows the position of its canvas pointer.
            leftData.position = Vector2.zero;
            leftData.scrollDelta = Input.mouseScrollDelta;
            leftData.button = UnityEngine.EventSystems.PointerEventData.InputButton.Left;

            if (activeGraphicRaycaster)
            {
                // Let the active raycaster find intersections on its canvas
                activeGraphicRaycaster.RaycastPointer(leftData, m_RaycastResultCache);
                var raycast = FindFirstRaycast(m_RaycastResultCache);
                leftData.pointerCurrentRaycast = raycast;
                m_RaycastResultCache.Clear();
                
				OVRRaycaster ovrRaycaster = raycast.module as OVRRaycaster;
                if (ovrRaycaster) // raycast may not actually contain a result
                {
                    // The Unity UI system expects event data to have a screen position
                    // so even though this raycast came from a world space ray we must get a screen
                    // space position for the camera attached to this raycaster for compatability
                    Vector2 position = ovrRaycaster.GetScreenPosition(raycast);
                    
                    leftData.delta = position - leftData.position;
                    leftData.position = position;
                }
            }

            // copy the apropriate data into right and middle slots
            UnityEngine.EventSystems.PointerEventData rightData;
            GetPointerData(kMouseRightId, out rightData, true );
            CopyFromTo(leftData, rightData);
            rightData.button = UnityEngine.EventSystems.PointerEventData.InputButton.Right;

            UnityEngine.EventSystems.PointerEventData middleData;
            GetPointerData(kMouseMiddleId, out middleData, true );
            CopyFromTo(leftData, middleData);
            middleData.button = UnityEngine.EventSystems.PointerEventData.InputButton.Middle;

            m_MouseState.SetButtonState(UnityEngine.EventSystems.PointerEventData.InputButton.Left, StateForMouseButton(0), leftData);
            m_MouseState.SetButtonState(UnityEngine.EventSystems.PointerEventData.InputButton.Right, StateForMouseButton(1), rightData);
            m_MouseState.SetButtonState(UnityEngine.EventSystems.PointerEventData.InputButton.Middle, StateForMouseButton(2), middleData);
            return m_MouseState;
        }

        /// <summary>
        /// New version of ShouldStartDrag implemented first in PointerInputModule. This version differs in that
        /// for ray based pointers it makes a decision about whether a drag should start based on the angular change
        /// the pointer has made so far, as seen from the camera. This also works when the world space ray is 
        /// translated rather than rotated, since the beginning and end of the movement are considered as angle from
        /// the same point.
        /// </summary>
        private bool ShouldStartDrag(UnityEngine.EventSystems.PointerEventData pointerEvent)
        {
            if (!pointerEvent.useDragThreshold)
                return true;

            if (pointerEvent as OVRRayPointerEventData == null)
            {
                 // Same as original behaviour for canvas based pointers
                return (pointerEvent.pressPosition - pointerEvent.position).sqrMagnitude >= eventSystem.pixelDragThreshold * eventSystem.pixelDragThreshold;
            }
            else
            {
                // When it's not a screen space pointer we have to look at the angle it moved rather than the pixels distance
                // For gaze based pointing screen-space distance moved will always be near 0
                Vector3 cameraPos = pointerEvent.pressEventCamera.transform.position;
                Vector3 pressDir = (pointerEvent.pointerPressRaycast.worldPosition - cameraPos).normalized;
                Vector3 currentDir = (pointerEvent.pointerCurrentRaycast.worldPosition - cameraPos).normalized;
                return Vector3.Dot(pressDir, currentDir) < Mathf.Cos(Mathf.Deg2Rad * (angleDragThreshold));
            }
        }

        /// <summary>
        /// The purpose of this function is to allow us to switch between using the standard IsPointerMoving
        /// method for mouse driven pointers, but to always return true when it's a ray based pointer. 
        /// All real-world ray-based input devices are always moving so for simplicity we just return true
        /// for them. 
        /// 
        /// If PointerEventData.IsPointerMoving was virtual we could just override that in
        /// OVRRayPointerEventData.
        /// </summary>
        /// <param name="pointerEvent"></param>
        /// <returns></returns>
        static bool IsPointerMoving(UnityEngine.EventSystems.PointerEventData pointerEvent)
        {
            OVRRayPointerEventData rayPointerEventData = pointerEvent as OVRRayPointerEventData;
            if (rayPointerEventData != null)
                return true;
            else
                return pointerEvent.IsPointerMoving();
        }

        /// <summary>
        /// Exactly the same as the code from PointerInputModule, except that we call our own
        /// IsPointerMoving.
        /// 
        /// This would also not be necessary if PointerEventData.IsPointerMoving was virtual
        /// </summary>
        /// <param name="pointerEvent"></param>
        protected override void ProcessDrag(UnityEngine.EventSystems.PointerEventData pointerEvent)
        {
            bool moving = IsPointerMoving(pointerEvent);
            if (moving && pointerEvent.pointerDrag != null
                && !pointerEvent.dragging
                && ShouldStartDrag(pointerEvent))
            {
                UnityEngine.EventSystems.ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, UnityEngine.EventSystems.ExecuteEvents.beginDragHandler);
                pointerEvent.dragging = true;
            }

            // Drag notification
            if (pointerEvent.dragging && moving && pointerEvent.pointerDrag != null)
            {
                // Before doing drag we should cancel any pointer down state
                // And clear selection!
                if (pointerEvent.pointerPress != pointerEvent.pointerDrag)
                {
                    UnityEngine.EventSystems.ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);

                    pointerEvent.eligibleForClick = false;
                    pointerEvent.pointerPress = null;
                    pointerEvent.rawPointerPress = null;
                }
                UnityEngine.EventSystems.ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, UnityEngine.EventSystems.ExecuteEvents.dragHandler);
            }
        }
       
        /// <summary>
        /// Get state of button corresponding to gaze pointer
        /// </summary>
        /// <returns></returns>
        protected UnityEngine.EventSystems.PointerEventData.FramePressState GetGazeButtonState()
        {
            var pressed = false;
            var released = false;

            if (activeController != OVRInput.Controller.None) {
                pressed = OVRInput.GetDown(joyPadClickButton, activeController);
                released = OVRInput.GetUp(joyPadClickButton, activeController);
            }
            else {
                pressed = OVRInput.GetDown(joyPadClickButton);
                released = OVRInput.GetUp(joyPadClickButton);
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            pressed |= Input.GetMouseButtonDown(0);
            released |= Input.GetMouseButtonUp(0);
#endif

			if (pressed && released) {
				//Debug.Log ("pressed & released");
				return UnityEngine.EventSystems.PointerEventData.FramePressState.PressedAndReleased;
			}
			if (pressed) {
				//Debug.Log ("pressed");
				return UnityEngine.EventSystems.PointerEventData.FramePressState.Pressed;
			}
			if (released) {
				//Debug.Log ("released");
				return UnityEngine.EventSystems.PointerEventData.FramePressState.Released;
			}

			return UnityEngine.EventSystems.PointerEventData.FramePressState.NotChanged;
        }
        
        /// <summary>
        /// Get extra scroll delta from gamepad
        /// </summary>
        protected Vector2 GetExtraScrollDelta()
        {
            Vector2 scrollDelta = new Vector2();
            if (useLeftStickScroll)
            {
                float x = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x;
                float y = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
                if (Mathf.Abs(x) < leftStickDeadZone) x = 0;
                if (Mathf.Abs(y) < leftStickDeadZone) y = 0;
                scrollDelta = new Vector2 (x,y);   
            }
            return scrollDelta;
        }
    };
}