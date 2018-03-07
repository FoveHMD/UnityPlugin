using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FoveInterfaceBase), true)]
public class FoveInterfaceBaseEditor : Editor
{
	// World scale
	SerializedProperty _worldScale;

	// Custom position scale values
	SerializedProperty _useCustomPositionScaling;
	SerializedProperty _posScales;

	// Eye positioning overrides
	SerializedProperty _usesCustomPlacement;

	SerializedProperty _IOD;
	SerializedProperty _eyeHeight;
	SerializedProperty _eyeForward;

	// Disable frame-by-frame update of eye projections
	SerializedProperty _suppressProjectionUpdates;

	// Fove SDK Enable/Disable components\
	SerializedProperty _gaze;

	SerializedProperty _orientation;
	SerializedProperty _position;

	// Texture info fields
	SerializedProperty _oversamplingRatio;

	// Skip automatic calibration
	SerializedProperty _skipAutoCalibrationCheck;

	// Compositor properties
	SerializedProperty _compositorLayerType;
	SerializedProperty _compositorDisableTimewarp;
	SerializedProperty _compositorDisableDistortion;

	private static bool _showOverrides;
	private static bool _showCompositorAttribs;

	protected GUIStyle helpStyle;

	//===========================
	// Preferences
	//
	private static bool prefsLoaded = false;

	// Prefs values
	public static bool prefs_EnableVsyncCheck = true;

	// Prefs keys
	const string prefsKey_enableVsyncCheck = "FOVE_enableVsyncCheck";

	[PreferenceItem("FOVE Plugin")]
	public static void FOVE_PreferencesGUI() {
		// Load the preferences
		if (!prefsLoaded) {
			prefs_EnableVsyncCheck = EditorPrefs.GetBool(prefsKey_enableVsyncCheck, true);
			prefsLoaded = true;
		}

		// Preferences GUI
		prefs_EnableVsyncCheck = EditorGUILayout.Toggle("Enable Vsync Check", prefs_EnableVsyncCheck);

		// Save the preferences
		if (GUI.changed)
			EditorPrefs.SetBool(prefsKey_enableVsyncCheck, prefs_EnableVsyncCheck);
	}

	// Startup Checks
	static FoveInterfaceBaseEditor() {
		EditorApplication.update += CheckSettings;
	}

	static void CheckSettings() {
		EditorApplication.update -= CheckSettings;

		string[] qualityNames = QualitySettings.names;
		bool hasVsyncOn = false;

		int currentLevel = QualitySettings.GetQualityLevel();

		for (int i = 0; i < qualityNames.Length; i++) {
			QualitySettings.SetQualityLevel(i);
			if (QualitySettings.vSyncCount > 0) {
				hasVsyncOn = true;
			}
		}

		QualitySettings.SetQualityLevel(currentLevel);

		prefs_EnableVsyncCheck = EditorPrefs.GetBool(prefsKey_enableVsyncCheck);
		if (hasVsyncOn && prefs_EnableVsyncCheck) {
			int choice = EditorUtility.DisplayDialogComplex("Vsync Detected", "One or more of your quality levels has Vsync enabled, which will cause performance problems.", "Disable Vsync", "Ignore", "Don't Remind Me");
			switch (choice) {
				case 0:
					for (int i = 0; i < qualityNames.Length; i++) {
						QualitySettings.SetQualityLevel(i);
						QualitySettings.vSyncCount = 0;
					}

					QualitySettings.SetQualityLevel(currentLevel);
					break;
				case 1:
					break;
				case 2:
					EditorPrefs.SetBool(prefsKey_enableVsyncCheck, false);
					prefs_EnableVsyncCheck = false;
					break;
			}
		}
	}

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
		_worldScale = serializedObject.FindProperty("worldScale");
		CheckForNull(_worldScale, "_worldScale");

		_useCustomPositionScaling = serializedObject.FindProperty("useCustomPositionScaling");
		CheckForNull(_useCustomPositionScaling, "_useCustomPositionScaling");
		_posScales = serializedObject.FindProperty("positionScales");
		CheckForNull(_posScales, "_posScales");

		_usesCustomPlacement = serializedObject.FindProperty("useCustomEyePlacement");
		CheckForNull(_usesCustomPlacement, "_usesCustomPlacement");
		_IOD = serializedObject.FindProperty("interOcularDistance");
		CheckForNull(_IOD, "_IOD");
		_eyeHeight = serializedObject.FindProperty("eyeHeight");
		CheckForNull(_eyeHeight, "_eyeHeight");
		_eyeForward = serializedObject.FindProperty("eyeForward");
		CheckForNull(_eyeForward, "_eyeForward");

		_suppressProjectionUpdates = serializedObject.FindProperty("suppressProjectionUpdates");
		CheckForNull(_suppressProjectionUpdates, "suppressProjectionUpdates");

		_gaze = serializedObject.FindProperty("gaze");
		CheckForNull(_gaze, "_gaze");
		_orientation = serializedObject.FindProperty("orientation");
		CheckForNull(_orientation, "_orientation");
		_position = serializedObject.FindProperty("position");
		CheckForNull(_position, "_position");

		_oversamplingRatio = serializedObject.FindProperty("oversamplingRatio");
		CheckForNull(_oversamplingRatio, "_oversamplingRatio");

		_skipAutoCalibrationCheck = serializedObject.FindProperty("skipAutoCalibrationCheck");

		_compositorLayerType = serializedObject.FindProperty("layerType");
		CheckForNull(_compositorLayerType, "_compositorLayerType");
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

		EditorGUILayout.PropertyField(_worldScale);

		// Cache the editor's playing state so we can prevent editing fields that shouldn't update during
		// a live play session.
		bool isPlaying = EditorApplication.isPlaying;

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Client uses...", EditorStyles.boldLabel);
		EditorGUI.BeginChangeCheck();
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_gaze);
			EditorGUILayout.PropertyField(_orientation);
			EditorGUILayout.PropertyField(_position);
			EditorGUI.indentLevel--;
		}
		if (EditorGUI.EndChangeCheck())
		{
			FoveInterfaceBase xface = target as FoveInterfaceBase;
			if (xface != null)
			{
				xface.ReloadFoveClient();
			}
		}

		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(_skipAutoCalibrationCheck);

		EditorGUILayout.PropertyField(_oversamplingRatio);

		GUI.enabled = true;
		_showOverrides = EditorGUILayout.Foldout(_showOverrides, "Headset Overrides");
		if (_showOverrides)
		{
			EditorGUI.indentLevel++;

			EditorGUILayout.PropertyField(_useCustomPositionScaling);
			GUI.enabled = _useCustomPositionScaling.boolValue;
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(_posScales);
				EditorGUI.indentLevel--;
			}

			GUI.enabled = true;
			EditorGUILayout.PropertyField(_usesCustomPlacement);
			GUI.enabled = _usesCustomPlacement.boolValue;// & !isPlaying;  // not modifiable in play mode
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(_IOD);
				EditorGUILayout.PropertyField(_eyeHeight);
				EditorGUILayout.PropertyField(_eyeForward);
				EditorGUI.indentLevel--;
			}

			GUI.enabled = true;
			EditorGUILayout.PropertyField(_suppressProjectionUpdates);

			EditorGUI.indentLevel--;
		}

		GUI.enabled = true;
		_showCompositorAttribs = EditorGUILayout.Foldout(_showCompositorAttribs, "Compositor options");
		if (_showCompositorAttribs)
		{
			GUI.enabled = !isPlaying;
			EditorGUI.indentLevel++;

			EditorGUILayout.PropertyField(_compositorLayerType);
			EditorGUILayout.PropertyField(_compositorDisableTimewarp);
			EditorGUILayout.PropertyField(_compositorDisableDistortion);

			EditorGUI.indentLevel--;
		}

		GUI.enabled = true;
		if (isPlaying && GUILayout.Button("Ensure calibration"))
		{
			Debug.Log("Manually triggering eye tracking calibration check from inspector...");
			FoveInterface.EnsureEyeTrackingCalibration();
		}

		if (Application.targetFrameRate != -1)
		{
			GUILayout.Label(
				"WARNING: Your target framerate is set to " + Application.targetFrameRate + ". Having a target framerate can artificially slow down FOVE frame submission. We recommend disabling this."
				, helpStyle
				, GUILayout.ExpandWidth(true));
		}

		DrawLocalGUIEditor();

		serializedObject.ApplyModifiedProperties();

		// Tell a live FoveInterfaceBase object to try to update itself
		if (isPlaying && GUI.changed)
		{
			FoveInterfaceBase xface = target as FoveInterfaceBase;
			if (xface != null)
			{
				xface.RefreshSetup();
			}
		}
	}

	protected virtual void DrawLocalGUIEditor()
	{
	}
}
