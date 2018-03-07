using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using Fove.Managed;

[RequireComponent(typeof(Camera))]
[AddComponentMenu("")]  // hide FoveEyeCamera from mono behaviour menus to discourage people adding it by hand
public class FoveEyeCamera : MonoBehaviour {

	[SerializeField]
	public EFVR_Eye whichEye = EFVR_Eye.Left;

	[SerializeField]
	public bool suppressProjectionUpdates = true;

	[SerializeField]
	[Range(0.1f, 4f)]
	public float resolutionScale = 1.4f;
	public int antiAliasing = 1;

	private float _lastScale;
	private int _lastAA;

	private FVRHeadset _headset = null;
	private Camera _cam = null;
	private FVRCompositor _compositor;
	public FoveInterfaceBase foveInterfaceBase;

	private SFVR_CompositorLayerSubmitInfo _layerSubmitInfo;
	
	private static bool couldUseNewMatrices;
	private static Matrix4x4 leftProjectionMatrix;
	private static Matrix4x4 rightProjectionMatrix;

	private class FoveCameraPair
	{
		public SFVR_CompositorLayer layer;

		public FoveEyeCamera left;
		public FoveEyeCamera right;

		// Private so nobody can make one this way
		private FoveCameraPair() {}

		public FoveCameraPair(FVRCompositor compositor, FoveInterfaceBase xface)
		{
			left = null;
			right = null;

			var createInfo = new SFVR_CompositorLayerCreateInfo
			{
				alphaMode = EFVR_AlphaMode.Auto,
				disableDistortion = xface.DistortionDisabled,
				disableTimewarp = xface.TimewarpDisabled,
				disableFading = xface.FadingDisabled,
				type = xface.LayerType
			};

			var err = compositor.CreateLayer(createInfo, out layer);
			if (err != EFVR_ErrorCode.None)
			{
				Debug.LogError("[FOVE] Error getting layer: " + err);
			}

			Debug.Log("[FOVE] Layer requested no distortion? " + createInfo.disableDistortion);
		}

		public void SetCamera(EFVR_Eye whichEye, FoveEyeCamera cam)
		{
			switch (whichEye)
			{
				case EFVR_Eye.Left:
					left = cam;
					break;
				case EFVR_Eye.Right:
					right = cam;
					break;
			}
		}

		public bool CanUseCamera(EFVR_Eye whichEye, FoveEyeCamera cam)
		{
			switch (whichEye)
			{
				case EFVR_Eye.Left:
					return left == null || left == cam;
				case EFVR_Eye.Right:
					return right == null || right == cam;
			}

			return false;
		}
	}

	private static List<FoveCameraPair> _layerCameraPairs = new List<FoveCameraPair>();

	private static FoveCameraPair GetNextLayerPair(EFVR_Eye whichEye, FoveEyeCamera cam)
	{
		if (whichEye != EFVR_Eye.Left && whichEye != EFVR_Eye.Right)
			return null;

		foreach (var pair in _layerCameraPairs)
		{
			if (pair.CanUseCamera(whichEye, cam))
				return pair;
		}

		var p = new FoveCameraPair(cam._compositor, cam.foveInterfaceBase);
		_layerCameraPairs.Add(p);

		return p;
	}

	public FVRCompositor Compositor
	{
		set
		{
			_compositor = value;
		}
	}

	private static bool _isProjectionErrorFree;

	private void UpdateProjectionMatrix()
	{
		// Get the projection matrix from the FOVE SDK and apply it to the camera
		if (!_isProjectionErrorFree || couldUseNewMatrices)
		{
			SFVR_Matrix44 fove_mx_left, fove_mx_right;
			var err = _headset.GetProjectionMatricesRH(_cam.nearClipPlane, _cam.farClipPlane, out fove_mx_left,
				out fove_mx_right);
			if (err != EFVR_ErrorCode.None)
			{
				_isProjectionErrorFree = false;
			}
			else
			{
				_isProjectionErrorFree = true;
				leftProjectionMatrix = FoveUnityUtils.GetUnityMx(fove_mx_left);
				rightProjectionMatrix = FoveUnityUtils.GetUnityMx(fove_mx_right);

				couldUseNewMatrices = false;
			}
		}

		switch (whichEye)
		{
			case EFVR_Eye.Left:
				_cam.projectionMatrix = leftProjectionMatrix;
				break;
			case EFVR_Eye.Right:
				_cam.projectionMatrix = rightProjectionMatrix;
				break;
			default:
				Debug.Log("Fove eye camera not set to left or right");
				break;
		}

	}

	private void SetSubmitBounds(ref SFVR_CompositorLayerEyeSubmitInfo info)
	{
		info.bounds.left = 0.0f;
		info.bounds.bottom = 0.0f;
		info.bounds.right = 1.0f;
		info.bounds.top = 1.0f;
	}

	void Start() {
		_headset = FoveInterfaceBase.GetFVRHeadset();

		_lastScale = resolutionScale;
		_lastAA = antiAliasing;

		FoveCameraPair myPair = GetNextLayerPair(whichEye, this);
		myPair.SetCamera(whichEye, this);
		SFVR_CompositorLayer layer = myPair.layer;
		SFVR_Vec2i res = layer.idealResolutionPerEye;

		var rt = new RenderTexture((int)(res.x * resolutionScale), (int)(res.y * resolutionScale), 24);
		rt.antiAliasing = antiAliasing;

		_cam = gameObject.GetComponent<Camera>();
		_cam.targetTexture = rt;
		_cam.enabled = false;

		switch (whichEye)
		{
			case EFVR_Eye.Left:
				SetSubmitBounds(ref _layerSubmitInfo.left);
				_layerSubmitInfo.left.texInfo.colorSpace = EFVR_ColorSpace.Linear;
				break;
			case EFVR_Eye.Right:
				SetSubmitBounds(ref _layerSubmitInfo.right);
				_layerSubmitInfo.left.texInfo.colorSpace = EFVR_ColorSpace.Linear;
				break;
		}

		_layerSubmitInfo.layerId = myPair.layer.layerId;
	}

	void Update()
	{
		// For live updating of resolution scale and antialiasing settings
		const float tolerance = 0.0001f;
		if (Math.Abs(resolutionScale - _lastScale) > tolerance || antiAliasing != _lastAA)
		{
			var rt = new RenderTexture((int)(1280 * resolutionScale), (int)(1440 * resolutionScale), 24);
			rt.antiAliasing = antiAliasing;

			_cam.targetTexture.Release();
			_cam.targetTexture = rt;

			_lastScale = resolutionScale;
			_lastAA = antiAliasing;
		}
	}

	void OnPreCull() {
		UpdateProjectionMatrix();
	}
	
	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		Graphics.Blit(source, destination);

		if (_compositor == null)
			return;

		IntPtr texPtr = source.GetNativeTexturePtr();
		if (texPtr != IntPtr.Zero)
		{
			_layerSubmitInfo.pose = FoveInterface.GetLastPose();
			switch (whichEye)
			{
				case EFVR_Eye.Left:
					_layerSubmitInfo.left.texInfo.pTexture = texPtr;
					_layerSubmitInfo.right.texInfo.pTexture = IntPtr.Zero;
					break;
				case EFVR_Eye.Right:
					_layerSubmitInfo.left.texInfo.pTexture = IntPtr.Zero;
					_layerSubmitInfo.right.texInfo.pTexture = texPtr;
					break;
				default:
					Debug.LogError("[FOVE] Camera set to " + whichEye + " which isn't supported.");
					return;
			}

			var result = _compositor.Submit(ref _layerSubmitInfo);
			if (result != EFVR_ErrorCode.None)
			{
				Debug.LogWarning("[FOVE] Submit returned unexpected " + result);
			}

			GL.Flush();
		}
		else
		{
			Debug.LogWarning("RenderTexture native pointer is null; cannot submit null texture pointers.");
		}

		if (!suppressProjectionUpdates)
		{
			couldUseNewMatrices = true;
		}
	}
}
