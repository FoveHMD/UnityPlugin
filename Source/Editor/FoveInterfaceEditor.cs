using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FoveInterface))]
[InitializeOnLoad]
public class FoveInterfaceEditor : FoveInterfaceBaseEditor
{
	// Intermediate camera setups
	SerializedProperty _usesCameraPrefab;
	SerializedProperty _cameraPrototype;

	// Advanced camera setups
	SerializedProperty _usesCameraOverride;
	SerializedProperty _leftCameraOverride;
	SerializedProperty _rightCameraOverride;
	
	SerializedProperty _usesAAOverride;
	SerializedProperty _aaSamples;
	private string[] _aaOptions = { "1x", "2x", "4x", "8x" };
	private int[] _aaOptionValues = { 1, 2, 4, 8 };

	// Compositor properties
	SerializedProperty _compositorRenderingEnabled;

	private bool showOverrides;

	protected override void EnableProperties()
	{
		base.EnableProperties();

		_usesCameraPrefab = serializedObject.FindProperty("useCameraPrefab");
		CheckForNull(_usesCameraPrefab, "_usesCameraPrefab");
		_usesCameraOverride = serializedObject.FindProperty("useCameraOverride");
		CheckForNull(_usesCameraOverride, "_usesCameraOverride");

		_cameraPrototype = serializedObject.FindProperty("eyeCameraPrototype");
		CheckForNull(_cameraPrototype, "_cameraPrototype");
		_leftCameraOverride = serializedObject.FindProperty("leftEyeOverride");
		CheckForNull(_leftCameraOverride, "_leftCameraOverride");
		_rightCameraOverride = serializedObject.FindProperty("rightEyeOverride");
		CheckForNull(_rightCameraOverride, "_rightCameraOverride");

		_aaSamples = serializedObject.FindProperty("antialiasSampleCount");
		CheckForNull(_aaSamples, "_aaSamples");
		_usesAAOverride = serializedObject.FindProperty("overrideAntialiasing");
		CheckForNull(_usesAAOverride, "_usesAAOverride");

		// Compositor props
		_compositorRenderingEnabled = serializedObject.FindProperty("enableRendering");
		CheckForNull(_compositorRenderingEnabled, "_compositorRenderingEnabled");
	}

	protected override void DrawLocalGUIEditor()
	{
		bool isPlaying = EditorApplication.isPlaying;

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Legacy Settings", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;

		// Prevent editing fields that don't update while actually playing in the editor
		GUI.enabled = !isPlaying;

		EditorGUILayout.PropertyField(_usesCameraPrefab);
		GUI.enabled = _usesCameraPrefab.boolValue & !isPlaying;
		EditorGUILayout.PropertyField(_cameraPrototype);
		GUI.enabled = true & !isPlaying;

		EditorGUILayout.Space();

		GUI.enabled = true;
		showOverrides = EditorGUILayout.Foldout(showOverrides, "Legacy Overrides");
		if (showOverrides)
		{
			EditorGUI.indentLevel++;

			EditorGUILayout.PropertyField(_compositorRenderingEnabled);

			EditorGUILayout.PropertyField(_usesAAOverride);
			GUI.enabled = _usesAAOverride.boolValue & !isPlaying; // not modifiable in play mode
			{
				EditorGUI.indentLevel++;
				_aaSamples.intValue = EditorGUILayout.IntPopup(_aaSamples.intValue, _aaOptions, _aaOptionValues);
				int samples = _aaSamples.intValue;
				if (samples > 8)
					samples = 8;
				else if (samples > 4)
					samples = 4;
				else if (samples < 1)
					samples = 1;
				EditorGUI.indentLevel--;
			}

			GUI.enabled = !isPlaying;
			EditorGUILayout.PropertyField(_usesCameraOverride);
			GUI.enabled = _usesCameraOverride.boolValue & !isPlaying;  // not modifiable in play mode
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(_leftCameraOverride);
				EditorGUILayout.PropertyField(_rightCameraOverride);
				EditorGUI.indentLevel--;
			}
			
			if (_usesCameraOverride.boolValue && _usesCameraPrefab.boolValue)
			{
				// Don't use EditorGUILayout.Label()
				GUILayout.Label(
					"WARNING: Having camera prefab and camera override enabled is an error; camera prefab will be used and override will be ignored."
					, helpStyle
					, GUILayout.ExpandWidth(true));
			}
			
			EditorGUI.indentLevel--;
		}

		EditorGUI.indentLevel--;
	}
}