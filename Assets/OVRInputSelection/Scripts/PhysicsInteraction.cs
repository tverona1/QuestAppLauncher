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
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;


namespace ControllerSelection {
    public class PhysicsInteraction : MonoBehaviour {
        public NavMeshAgent agent;
        public const float navMeshSampleDistance = 4f;
        public UnityEngine.UI.Text outText;

        private void Awake() {
            agent.updateRotation = false;
        }

        public void OnGroundClick(BaseEventData data) {
            OVRRayPointerEventData pData = (OVRRayPointerEventData)data;
            Vector3 destinationPosition = Vector3.zero;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(pData.pointerCurrentRaycast.worldPosition, out hit, navMeshSampleDistance, NavMesh.AllAreas)) {
                destinationPosition = hit.position;
            }
            else {
                destinationPosition = pData.pointerCurrentRaycast.worldPosition;
            }

            agent.isStopped = true;
            agent.SetDestination(destinationPosition);
            agent.isStopped = false;

            if (outText != null) {
                outText.text = "<b>Last Interaction:</b>\nNavigate to: (" + destinationPosition.x + ", " + destinationPosition.y + ", " + destinationPosition.z + ")";
            }
        }

        public void OnBackClick(BaseEventData data) {
            SceneManager.LoadScene("main", LoadSceneMode.Single);
        }
    }
}