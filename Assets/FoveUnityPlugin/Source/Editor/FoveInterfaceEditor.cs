using UnityEditor;
using UnityEngine;
using Fove.Unity;
using Fove;

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

    SerializedProperty _registerCameraObject;
    SerializedProperty _gazeCastCullMask;

    // Compositor properties
    //SerializedProperty _compositorLayerType;
    SerializedProperty _compositorDisableTimewarp;
    //SerializedProperty _compositorDisableDistortion;

    SerializedProperty _dismissCameraEnabledWarning;
    SerializedProperty _dismissCameraDisabledWarning;

    private static bool _showGazeCast;
    private static bool _showCullingMasks;
    private static bool _showCompositorAttribs;
    
    private GUIContent fetchPositionLabel = new GUIContent("- position");
    private GUIContent fetchGazeLabel = new GUIContent("- gaze");
    private GUIContent fetchOrientationLabel = new GUIContent("- orientation");
    private GUIContent castCullMaskLabel = new GUIContent("Cull Mask");
    private GUIContent registerCameraLabel = new GUIContent("Register Camera");

    protected SerializedProperty GetAndCheckSeriazedProperty(string name)
    {
        var ret = serializedObject.FindProperty(name);
        if (ret == null)
            Debug.LogWarning("Can't find '" + name + "' property");

        return ret;
    }

    protected void OnEnable()
    {
        EnableProperties();
    }

    protected virtual void EnableProperties()
    {
        _gaze = GetAndCheckSeriazedProperty("fetchGaze");
        _orientation = GetAndCheckSeriazedProperty("fetchOrientation");
        _position = GetAndCheckSeriazedProperty("fetchPosition");
        
        _eyeTargets = GetAndCheckSeriazedProperty("eyeTargets");
        _poseType = GetAndCheckSeriazedProperty("poseType");

        _cullMaskLeft = GetAndCheckSeriazedProperty("cullMaskLeft");
        _cullMaskRight = GetAndCheckSeriazedProperty("cullMaskRight");

        _registerCameraObject = GetAndCheckSeriazedProperty("registerCameraObject");
        _gazeCastCullMask = GetAndCheckSeriazedProperty("gazeCastCullMask");

        //_compositorLayerType = GetAndCheckSeriazedProperty("layerType");
        _compositorDisableTimewarp = GetAndCheckSeriazedProperty("disableTimewarp");
        //_compositorDisableDistortion = GetAndCheckSeriazedProperty("disableDistortion");

        _dismissCameraEnabledWarning = GetAndCheckSeriazedProperty("cameraEnabledWarningDismissed");
        _dismissCameraDisabledWarning = GetAndCheckSeriazedProperty("cameraDisabledWarningDismissed");
    }

    // Currently does not support editing of multiple objects at once.
    // I do not anticipate any problems with this since you should only
    // ever really have one per scene anyway.
    public sealed override void OnInspectorGUI()
    {
        // A decent style.  Light grey text inside a border.
        var helpStyle = new GUIStyle(GUI.skin.box);
        helpStyle.wordWrap = true;
        helpStyle.alignment = TextAnchor.UpperLeft;
        helpStyle.normal.textColor = Color.red;

        var warningLabelStyle = new GUIStyle(EditorStyles.label);
        warningLabelStyle.normal.textColor = Color.red;
        warningLabelStyle.wordWrap = true;

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

        GUI.enabled = true;
        _showGazeCast = EditorGUILayout.Foldout(_showGazeCast, "Gaze Object Detection");
        if (_showGazeCast)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_registerCameraObject, registerCameraLabel);
            EditorGUILayout.PropertyField(_gazeCastCullMask, castCullMaskLabel);
            EditorGUI.indentLevel--;
        }

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
            //EditorGUILayout.PropertyField(_compositorDisableDistortion);

            EditorGUI.indentLevel--;
        }

        GUI.enabled = true;
        if (isPlaying && GUILayout.Button("Ensure calibration"))
        {
            Debug.Log("Manually triggering eye tracking calibration check from inspector...");
            FoveManager.StartEyeTrackingCalibration(new CalibrationOptions { lazy = true });
        }

        if (Application.targetFrameRate != -1)
        {
            GUILayout.Label(
                "WARNING: Your target framerate is set to " + Application.targetFrameRate + ". Having a target framerate can artificially slow down FOVE frame submission. We recommend disabling this."
                , helpStyle
                , GUILayout.ExpandWidth(true));
        }

        var isPrefabAsset =
#if UNITY_2018_3_OR_NEWER
            PrefabUtility.IsPartOfPrefabAsset(target);
#else
            PrefabUtility.GetPrefabType(target) == PrefabType.Prefab;
#endif

        var cam = ((FoveInterface)target).GetComponent<Camera>();
        if (cam != null && !isPrefabAsset)
        {
            if (FoveSettings.UseVRStereoViewOnPC)
            {
                if (cam.enabled && !_dismissCameraEnabledWarning.boolValue)
                {
                    EditorGUILayout.Space();
                    GUILayout.BeginVertical(helpStyle);
                    GUILayout.Label("WARNING: the camera component associated with this Fove Interface is enabled while your project is using the VR stereo view on the PC. " +
                        "This will cause your scene to render one extra time and lower the performance of your application.\n" +
                        "Click on 'Disable camera' to fix the issue.\n" +
                        "If it is actually intented, you can click on the 'Dismiss warning' button to permatently hide this warning", warningLabelStyle);
                    EditorGUILayout.Space();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Disable camera", GUILayout.Width(150)))
                        cam.enabled = false;
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Dismiss warning", GUILayout.Width(150)))
                        _dismissCameraEnabledWarning.boolValue = true;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                    GUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }
            else
            {
                if (!cam.enabled && !_dismissCameraDisabledWarning.boolValue)
                {
                    EditorGUILayout.Space();
                    GUILayout.BeginVertical(helpStyle);
                    GUILayout.Label("WARNING: the camera component associated with this Fove Interface is disabled while your project is not using the VR stereo view on the PC. " +
                        "This camera won't render to your PC view.\n" +
                        "Click on 'Enable camera' to fix the issue.\n" +
                        "If it is actually intented, you can click on the 'Dismiss warning' button to permatently hide this warning", warningLabelStyle);
                    EditorGUILayout.Space();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Enable camera", GUILayout.Width(150)))
                        cam.enabled = true;
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Dismiss warning", GUILayout.Width(150)))
                        _dismissCameraDisabledWarning.boolValue = true;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                    GUILayout.EndVertical();
                    EditorGUILayout.Space();

                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
