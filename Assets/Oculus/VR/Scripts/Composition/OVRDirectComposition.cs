/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus Utilities SDK License Version 1.31 (the "License"); you may not use
the Utilities SDK except in compliance with the License, which is provided at the time of installation
or download, or which otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at
https://developer.oculus.com/licenses/utilities-1.31

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using UnityEngine;
using System.Collections;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

public class OVRDirectComposition : OVRCameraComposition
{
	public GameObject directCompositionCameraGameObject;
	public Camera directCompositionCamera;
	public RenderTexture boundaryMeshMaskTexture = null;

	public override OVRManager.CompositionMethod CompositionMethod() { return OVRManager.CompositionMethod.Direct; }

	public OVRDirectComposition(GameObject parentObject, Camera mainCamera, OVRManager.CameraDevice cameraDevice, bool useDynamicLighting, OVRManager.DepthQuality depthQuality)
		: base(parentObject, mainCamera, cameraDevice, useDynamicLighting, depthQuality)
	{
		Debug.Assert(directCompositionCameraGameObject == null);
		directCompositionCameraGameObject = new GameObject();
		directCompositionCameraGameObject.name = "MRDirectCompositionCamera";
		directCompositionCameraGameObject.transform.parent = cameraInTrackingSpace ? cameraRig.trackingSpace : parentObject.transform;
		directCompositionCamera = directCompositionCameraGameObject.AddComponent<Camera>();
		directCompositionCamera.stereoTargetEye = StereoTargetEyeMask.None;
		directCompositionCamera.depth = float.MaxValue;
		directCompositionCamera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
		directCompositionCamera.clearFlags = mainCamera.clearFlags;
		directCompositionCamera.backgroundColor = mainCamera.backgroundColor;
		directCompositionCamera.cullingMask = mainCamera.cullingMask & (~OVRManager.instance.extraHiddenLayers);
		directCompositionCamera.nearClipPlane = mainCamera.nearClipPlane;
		directCompositionCamera.farClipPlane = mainCamera.farClipPlane;


		if (!hasCameraDeviceOpened)
		{
			Debug.LogError("Unable to open camera device " + cameraDevice);
		}
		else
		{
			Debug.Log("DirectComposition activated : useDynamicLighting " + (useDynamicLighting ? "ON" : "OFF"));
			CreateCameraFramePlaneObject(parentObject, directCompositionCamera, useDynamicLighting);
		}
	}

	public override void Update(Camera mainCamera)
	{
		if (!hasCameraDeviceOpened)
		{
			return;
		}

		if (!OVRPlugin.SetHandNodePoseStateLatency(OVRManager.instance.handPoseStateLatency))
		{
			Debug.LogWarning("HandPoseStateLatency is invalid. Expect a value between 0.0 to 0.5, get " + OVRManager.instance.handPoseStateLatency);
		}

		directCompositionCamera.clearFlags = mainCamera.clearFlags;
		directCompositionCamera.backgroundColor = mainCamera.backgroundColor;
		directCompositionCamera.cullingMask = mainCamera.cullingMask & (~OVRManager.instance.extraHiddenLayers);
		directCompositionCamera.nearClipPlane = mainCamera.nearClipPlane;
		directCompositionCamera.farClipPlane = mainCamera.farClipPlane;

		if (OVRMixedReality.useFakeExternalCamera || OVRPlugin.GetExternalCameraCount() == 0)
		{
			OVRPose trackingSpacePose = new OVRPose();
			trackingSpacePose.position = OVRManager.instance.trackingOriginType == OVRManager.TrackingOrigin.EyeLevel ? 
				OVRMixedReality.fakeCameraEyeLevelPosition : 
				OVRMixedReality.fakeCameraFloorLevelPosition;
			trackingSpacePose.orientation = OVRMixedReality.fakeCameraRotation;
			directCompositionCamera.fieldOfView = OVRMixedReality.fakeCameraFov;
			directCompositionCamera.aspect = OVRMixedReality.fakeCameraAspect;
			if (cameraInTrackingSpace)
			{
				directCompositionCamera.transform.FromOVRPose(trackingSpacePose, true);
			}
			else
			{
				OVRPose worldSpacePose = new OVRPose();
				worldSpacePose = OVRExtensions.ToWorldSpacePose(trackingSpacePose);
				directCompositionCamera.transform.FromOVRPose(worldSpacePose);
			}
		}
		else
		{
			OVRPlugin.CameraExtrinsics extrinsics;
			OVRPlugin.CameraIntrinsics intrinsics;
			OVRPlugin.Posef calibrationRawPose;

			// So far, only support 1 camera for MR and always use camera index 0
			if (OVRPlugin.GetMixedRealityCameraInfo(0, out extrinsics, out intrinsics, out calibrationRawPose))
			{
				float fovY = Mathf.Atan(intrinsics.FOVPort.UpTan) * Mathf.Rad2Deg * 2;
				float aspect = intrinsics.FOVPort.LeftTan / intrinsics.FOVPort.UpTan;
				directCompositionCamera.fieldOfView = fovY;
				directCompositionCamera.aspect = aspect;
				if (cameraInTrackingSpace)
				{
					OVRPose trackingSpacePose = ComputeCameraTrackingSpacePose(extrinsics, calibrationRawPose);
					directCompositionCamera.transform.FromOVRPose(trackingSpacePose, true);
				}
				else
				{
					OVRPose worldSpacePose = ComputeCameraWorldSpacePose(extrinsics, calibrationRawPose);
					directCompositionCamera.transform.FromOVRPose(worldSpacePose);
				}
			}
			else
			{
				Debug.LogWarning("Failed to get external camera information");
			}
		}

		if (hasCameraDeviceOpened)
		{
			if (boundaryMeshMaskTexture == null || boundaryMeshMaskTexture.width != Screen.width || boundaryMeshMaskTexture.height != Screen.height)
			{
				boundaryMeshMaskTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.R8);
				boundaryMeshMaskTexture.Create();
			}
			UpdateCameraFramePlaneObject(mainCamera, directCompositionCamera, boundaryMeshMaskTexture);
			directCompositionCamera.GetComponent<OVRCameraFrameCompositionManager>().boundaryMeshMaskTexture = boundaryMeshMaskTexture;
		}
	}

	public override void Cleanup()
	{
		base.Cleanup();

		OVRCompositionUtil.SafeDestroy(ref directCompositionCameraGameObject);
		directCompositionCamera = null;

		Debug.Log("DirectComposition deactivated");
	}
}

#endif
