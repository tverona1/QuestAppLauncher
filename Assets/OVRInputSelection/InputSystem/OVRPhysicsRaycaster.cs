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
using UnityEngine;
using System.Collections.Generic;

namespace ControllerSelection 
{
    /// <summary>
    /// Simple event system using physics raycasts.
    /// </summary>
    [RequireComponent(typeof(OVRCameraRig))]
    public class OVRPhysicsRaycaster : UnityEngine.EventSystems.BaseRaycaster
    {
        /// <summary>
        /// Const to use for clarity when no event mask is set
        /// </summary>
        protected const int kNoEventMaskSet = -1;
        

        /// <summary>
        /// Layer mask used to filter events. Always combined with the camera's culling mask if a camera is used.
        /// </summary>
        [SerializeField]
        protected LayerMask m_EventMask = kNoEventMaskSet;

        protected OVRPhysicsRaycaster()
        { }

        public override Camera eventCamera
        {
            get
            {
                return GetComponent<OVRCameraRig>().leftEyeCamera;
            }
        }

        /// <summary>
        /// Depth used to determine the order of event processing.
        /// </summary>
        public virtual int depth
        {
            get { return (eventCamera != null) ? (int)eventCamera.depth : 0xFFFFFF; }
        }

        /// <summary>
        /// Event mask used to determine which objects will receive events.
        /// </summary>
        public int finalEventMask
        {
            get { return (eventCamera != null) ? eventCamera.cullingMask & m_EventMask : kNoEventMaskSet; }
        }

        /// <summary>
        /// Layer mask used to filter events. Always combined with the camera's culling mask if a camera is used.
        /// </summary>
        public LayerMask eventMask
        {
            get { return m_EventMask; }
            set { m_EventMask = value; }
        }
       

        /// <summary>
        /// Perform a raycast using the worldSpaceRay in eventData.
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="resultAppendList"></param>
        public override void Raycast(UnityEngine.EventSystems.PointerEventData eventData, List<UnityEngine.EventSystems.RaycastResult> resultAppendList)
        {
            // This function is closely based on PhysicsRaycaster.Raycast

            if (eventCamera == null)
                return;

            OVRRayPointerEventData rayPointerEventData = eventData as OVRRayPointerEventData;
            if (rayPointerEventData == null)
                return;

            var ray = rayPointerEventData.worldSpaceRay;

            float dist = eventCamera.farClipPlane - eventCamera.nearClipPlane;

            var hits = Physics.RaycastAll(ray, dist, finalEventMask);

            if (hits.Length > 1)
                System.Array.Sort(hits, (r1, r2) => r1.distance.CompareTo(r2.distance));

            if (hits.Length != 0)
            {
                for (int b = 0, bmax = hits.Length; b < bmax; ++b)
                {
                    var result = new UnityEngine.EventSystems.RaycastResult
                    {
                        gameObject = hits[b].collider.gameObject,
                        module = this,
                        distance = hits[b].distance,
                        index = resultAppendList.Count,
                        worldPosition = hits[0].point,
                        worldNormal = hits[0].normal,
                    };
                    resultAppendList.Add(result);
                }
            }
        }

        /// <summary>
        ///  Perform a Spherecast using the worldSpaceRay in eventData.
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="resultAppendList"></param>
        /// <param name="radius">Radius of the sphere</param>
        public void Spherecast(UnityEngine.EventSystems.PointerEventData eventData, List<UnityEngine.EventSystems.RaycastResult> resultAppendList, float radius)
        {
            if (eventCamera == null)
                return;

            OVRRayPointerEventData rayPointerEventData = eventData as OVRRayPointerEventData;
            if (rayPointerEventData == null)
                return;

            var ray = rayPointerEventData.worldSpaceRay;

            float dist = eventCamera.farClipPlane - eventCamera.nearClipPlane;

            var hits = Physics.SphereCastAll(ray, radius, dist, finalEventMask);

            if (hits.Length > 1)
                System.Array.Sort(hits, (r1, r2) => r1.distance.CompareTo(r2.distance));

            if (hits.Length != 0)
            {
                for (int b = 0, bmax = hits.Length; b < bmax; ++b)
                {
                    var result = new UnityEngine.EventSystems.RaycastResult
                    {
                        gameObject = hits[b].collider.gameObject,
                        module = this,
                        distance = hits[b].distance,
                        index = resultAppendList.Count,
                        worldPosition = hits[0].point,
                        worldNormal = hits[0].normal,
                    };
                    resultAppendList.Add(result);
                }
            }
        }
        /// <summary>
        /// Get screen position of this world position as seen by the event camera of this OVRPhysicsRaycaster
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public Vector2 GetScreenPos(Vector3 worldPosition)
        {
            // In future versions of Uinty RaycastResult will contain screenPosition so this will not be necessary
            return eventCamera.WorldToScreenPoint(worldPosition);
        }
    }
}
