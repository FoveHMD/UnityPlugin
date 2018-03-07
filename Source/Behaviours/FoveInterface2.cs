using System;
using Fove.Managed;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
#endif

namespace UnityEngine
{
	/// <summary>
	/// FoveInterface2 takes over the non-HMD render pipeline, using orientation, position, and projection
	/// matrices from the FOVE interface to position the scene camera and the split-screen stereo projections
	/// so that image data can be rendered which takes advantage of Unity's internal stereo rendering
	/// optimizations. It is currently experimental, but internal tests suggest it works fairly well for the
	/// most part.
	/// </summary>
	[RequireComponent(typeof(Camera))]
	public sealed class FoveInterface2 : FoveInterfaceBase
	{
		private enum EyeState : int
		{
			PreRender = 0,
			DidLeft = 1,
			DidRight = 2,
		}

		private EyeState _eyeState;
		private Camera _camera;
		private bool _projectionErrorFree = false;

		private SFVR_Vec2i _idealResolution;
		private int _desiredRenderHeight;
		private bool _needsResolutionUpdate;
		private SFVR_CompositorLayerSubmitInfo _layerSubmitInfo;

		private void SetOptimalRenderScale()
		{
#if UNITY_2017_2_OR_NEWER
			float scale = _idealResolution.y * XRSettings.eyeTextureResolutionScale * oversamplingRatio / _camera.pixelHeight;
			XRSettings.eyeTextureResolutionScale = scale;
#else
			float scale = _idealResolution.y * VRSettings.renderScale * oversamplingRatio / _camera.pixelHeight;
			VRSettings.renderScale = scale;
#endif
			_needsResolutionUpdate = false;
		}

		public override bool RefreshSetup()
		{
#if UNITY_2017_2_OR_NEWER
			if (!XRSettings.enabled)
#else
			if (!VRSettings.enabled)
#endif
				Debug.LogError("FoveInterface2 requires VR to be enabled in project settings to work correctly.");

			if (!base.RefreshSetup())
				return false;

			if (!_isAuthoritative)
			{
				return true;
			}

			if (_camera == null)
				_camera = GetComponent<Camera>();

			if (!ConnectCompositor())
				return false;

			_idealResolution = _compositorLayer.idealResolutionPerEye;
			_desiredRenderHeight = (int)(_idealResolution.y * oversamplingRatio);
			_needsResolutionUpdate = true;

			_layerSubmitInfo.left.bounds = new SFVR_TextureBounds
			{
				left = 0,
				right = 1,
				top = 1,
				bottom = 0
			};
			_layerSubmitInfo.right.bounds = new SFVR_TextureBounds
			{
				left = 0,
				right = 1,
				top = 1,
				bottom = 0
			};
			_layerSubmitInfo.layerId = _compositorLayer.layerId;

			return true;
		}

		void OnPreCull()
		{
			if (!_isAuthoritative)
			{
				return;
			}

			if (null == _compositor)
			{
				return;
			}

			if (_needsResolutionUpdate)
				SetOptimalRenderScale();

			WaitForRenderPose_IfNeeded();
			
			SFVR_Matrix44 lViewMat = new SFVR_Matrix44();
			SFVR_Matrix44 rViewMat = new SFVR_Matrix44();

			Matrix4x4 scale = Matrix4x4.Scale(new Vector3(worldScale, worldScale, worldScale));

			// Something about the camera projections or relative translations means that I have to swap what the
			// x-axis values so positive-x is left (not right, as it typically should be).
			Vector3 left_translation = new Vector3()
			{
				x = usedIOD * 0.5f,
				y = eyeHeight,
				z = eyeForward
			} * worldScale;

			Vector3 right_translation = new Vector3()
			{
				x = -usedIOD * 0.5f,
				y = eyeHeight,
				z = eyeForward
			} * worldScale;

			Matrix4x4 lxform = Matrix4x4.TRS(left_translation, Quaternion.identity, new Vector3(1, 1, -1));
			Matrix4x4 rxform = Matrix4x4.TRS(right_translation, Quaternion.identity, new Vector3(1, 1, -1));

			_camera.SetStereoViewMatrix(Camera.StereoscopicEye.Left, lxform * transform.worldToLocalMatrix);
			_camera.SetStereoViewMatrix(Camera.StereoscopicEye.Right, rxform * transform.worldToLocalMatrix);

			// Protection junk
			SFVR_Matrix44 lProjMat = new SFVR_Matrix44();
			SFVR_Matrix44 rProjMat = new SFVR_Matrix44();

			if (_projectionErrorFree && suppressProjectionUpdates)
				return;

			bool foundProjectionErrors = false;
			EFVR_ErrorCode err;
			err = _sHeadset.GetEyeToHeadMatrices(out lViewMat, out rViewMat);
			foundProjectionErrors |= err != EFVR_ErrorCode.None;

			err = _sHeadset.GetProjectionMatricesRH(_camera.nearClipPlane, _camera.farClipPlane,
				out lProjMat, out rProjMat);
			foundProjectionErrors |= err != EFVR_ErrorCode.None;

			if (foundProjectionErrors)
			{
				_projectionErrorFree = false;
				Debug.Log("Giving up on flawed projection matrices...");
				return;
			}
			_projectionErrorFree = true;

			_camera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Left,
				FoveUnityUtils.GetUnityMx(lProjMat));
			_camera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Right,
				FoveUnityUtils.GetUnityMx(rProjMat));
		}

		/****************************************************************************************************\
		 * Interface Overrides
		\****************************************************************************************************/
		protected override Vector3 GetLeftEyePosition()
		{
			return new Vector3(-usedIOD * 0.5f, eyeHeight, eyeForward) * worldScale + transform.position;
		}

		protected override Vector3 GetRightEyePosition()
		{
			return new Vector3(usedIOD * 0.5f, eyeHeight, eyeForward) * worldScale + transform.position;
		}

		//
		// Rendering Callbacks
		//
		private void HandleSubmitResult(EFVR_ErrorCode result)
		{
		}

		void LateUpdate()
		{
			_eyeState = EyeState.PreRender;
		}

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			Graphics.Blit(source, destination);

			if (null == _compositor)
				return;

			if (source.height != _desiredRenderHeight)
			{
				Debug.LogWarning("Source height (" + source.height + ") does not match desired height (" + _desiredRenderHeight + ")");
				_needsResolutionUpdate = true;
			}

			_layerSubmitInfo.pose = GetLastPose();

			IntPtr texPtr = source.GetNativeTexturePtr();
			if (texPtr != IntPtr.Zero)
			{
				// Eyes are rendered in sequence when stereo offsets (etc) are specified, but the eye used is not indicated
				// so we have to keep track of that ourselves and hope that Unity doesn't change this on us in the future.
				EFVR_ErrorCode result;
				switch (_eyeState)
				{
					case EyeState.PreRender:
						_layerSubmitInfo.left.texInfo.pTexture = texPtr;
						_layerSubmitInfo.right.texInfo.pTexture = IntPtr.Zero;

						result = _compositor.Submit(ref _layerSubmitInfo);
						HandleSubmitResult(result);
						break;
					case EyeState.DidLeft:
						_layerSubmitInfo.left.texInfo.pTexture = IntPtr.Zero;
						_layerSubmitInfo.right.texInfo.pTexture = texPtr;

						result = _compositor.Submit(ref _layerSubmitInfo);
						HandleSubmitResult(result);
						break;
					case EyeState.DidRight:
						Debug.Log("NOTE: Got more than 2 render notifications");
						break;
				}
				_eyeState++;
			}
			else
			{
				Debug.LogWarning("RenderTexture native pointer is null; cannot submit null texture pointers.");
			}
		}
	}
}
