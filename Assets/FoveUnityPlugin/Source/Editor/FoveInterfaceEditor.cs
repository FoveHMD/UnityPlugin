using UnityEditor;
using UnityEngine;
using Fove.Unity;

[CustomEditor(typeof(FoveInterface), true)]
public class FoveInterfaceEditor : Editor
{
	// Fove SDK Enable/Disable components\
	SerializedProperty _gaze;
	SerializedProperty _orientation;
	SerializedProperty _position;

	SerializedProperty _eyeTargets;
	SerializedProperty _poseType;

	SerializedProperty _cullMaskLeft;
	SerializedProperty _cullMaskRight;

	SerializedProperty _gazeCastPolicy;

	// Compositor properties
	//SerializedProperty _compositorLayerType;
	SerializedProperty _compositorDisableTimewarp;
	SerializedProperty _compositorDisableDistortion;

	private static bool _showCullingMasks;
	private static bool _showCompositorAttribs;

	protected GUIStyle helpStyle;
	private GUIContent fetchPositionLabel = new GUIContent("- position");
	private GUIContent fetchGazeLabel = new GUIContent("- gaze");
	private GUIContent fetchOrientationLabel = new GUIContent("- orientation");

	protected void CheckForNull(System.Object obj, string name)
	{
		if (obj == null)
			Debug.LogWarning(name + " was null");
	}

	protected void OnEnable()
	{
		EnableProperties();
	}

	protected virtual void EnableProperties()
	{
		_gaze = serializedObject.FindProperty("fetchGaze");
		CheckForNull(_gaze, "_gaze");
		_orientation = serializedObject.FindProperty("fetchOrientation");
		CheckForNull(_orientation, "_orientation");
		_position = serializedObject.FindProperty("fetchPosition");
		CheckForNull(_position, "_position");
		
		_eyeTargets = serializedObject.FindProperty("eyeTargets");
		CheckForNull(_eyeTargets, "_eyeTargets");
		_poseType = serializedObject.FindProperty("poseType");
		CheckForNull(_poseType, "_poseType");

		_cullMaskLeft = serializedObject.FindProperty("cullMaskLeft");
		CheckForNull(_cullMaskLeft, "_cullMaskLeft");
		_cullMaskRight = serializedObject.FindProperty("cullMaskRight");
		CheckForNull(_cullMaskRight, "_cullMaskRight");

		_gazeCastPolicy = serializedObject.FindProperty("gazeCastPolicy");
		CheckForNull(_gazeCastPolicy, "gazeCastPolicy");

		//_compositorLayerType = serializedObject.FindProperty("layerType");
		//CheckForNull(_compositorLayerType, "_compositorLayerType");
		_compositorDisableTimewarp = serializedObject.FindProperty("disableTimewarp");
		CheckForNull(_compositorDisableTimewarp, "_compositorDisableTimewarp");
		_compositorDisableDistortion = serializedObject.FindProperty("disableDistortion");
		CheckForNull(_compositorDisableDistortion, "_compositorDisableDistortion");
	}

	// Currently does not support editing of multiple objects at once.
	// I do not anticipate any problems with this since you should only
	// ever really have one per scene anyway.
	public sealed override void OnInspectorGUI()
	{
		// A decent style.  Light grey text inside a border.
		helpStyle = new GUIStyle(GUI.skin.box);
		helpStyle.wordWrap = true;
		helpStyle.alignment = TextAnchor.UpperLeft;
		
		helpStyle.normal.textColor = Color.red;

		// Update the serializedobject
		serializedObject.Update();

		// Cache the editor's playing state so we can prevent editing fields that shouldn't update during
		// a live play session.
		bool isPlaying = EditorApplication.isPlaying;

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Client fetches and sync:", EditorStyles.boldLabel);
		EditorGUI.BeginChangeCheck();
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_gaze, fetchGazeLabel);
			EditorGUILayout.PropertyField(_orientation, fetchOrientationLabel);
			EditorGUILayout.PropertyField(_position, fetchPositionLabel);
			EditorGUI.indentLevel--;
		}

		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(_eyeTargets);
		EditorGUILayout.PropertyField(_poseType);
		EditorGUILayout.PropertyField(_gazeCastPolicy);

		GUI.enabled = true;
		_showCullingMasks = EditorGUILayout.Foldout(_showCullingMasks, "Per-Eye Culling Masks");
		if (_showCullingMasks)
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_cullMaskLeft);
			EditorGUILayout.PropertyField(_cullMaskRight);
			EditorGUI.indentLevel--;
		}
		
		GUI.enabled = true;
		_showCompositorAttribs = EditorGUILayout.Foldout(_showCompositorAttribs, "Compositor options");
		if (_showCompositorAttribs)
		{
			GUI.enabled = !isPlaying;
			EditorGUI.indentLevel++;

			//EditorGUILayout.PropertyField(_compositorLayerType);
			EditorGUILayout.PropertyField(_compositorDisableTimewarp);
			EditorGUILayout.PropertyField(_compositorDisableDistortion);

			EditorGUI.indentLevel--;
		}

		GUI.enabled = true;
		if (isPlaying && GUILayout.Button("Ensure calibration"))
		{
			Debug.Log("Manually triggering eye tracking calibration check from inspector...");
			FoveManager.EnsureEyeTrackingCalibration();
		}

		if (Application.targetFrameRate != -1)
		{
			GUILayout.Label(
				"WARNING: Your target framerate is set to " + Application.targetFrameRate + ". Having a target framerate can artificially slow down FOVE frame submission. We recommend disabling this."
				, helpStyle
				, GUILayout.ExpandWidth(true));
		}

		serializedObject.ApplyModifiedProperties();
	}
}
