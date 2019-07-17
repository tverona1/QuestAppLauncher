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

public class BackButtonLogic : MonoBehaviour {
    bool buttonPressed = false;

    void Update() {
        if (OVRInput.GetDown(OVRInput.Button.Back) || Input.GetKeyDown(KeyCode.Escape)) {
            buttonPressed = true;
        }

        if (buttonPressed && (OVRInput.GetUp(OVRInput.Button.Back) || Input.GetKeyUp(KeyCode.Escape))) {
            buttonPressed = false;
            Scene scene = SceneManager.GetActiveScene();

            if (scene.name == "main") {
#if UNITY_ANDROID && !UNITY_EDITOR
		        OVRManager.PlatformUIConfirmQuit();
#endif
            }
            else {
                SceneManager.LoadScene("main", LoadSceneMode.Single);
            }
        }

        if (OVRInput.GetUp(OVRInput.Button.Back) || Input.GetKeyUp(KeyCode.Escape)) {
            buttonPressed = false;
        }
    }
}
