using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace Fove.Unity
{
	[CustomEditor(typeof(FoveSettings))]
	public class FoveSettingsEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			EditorGUILayout.LabelField("Please edit this object type using the FOVE Settings window.");
			if (GUILayout.Button("FOVE Settings"))
			{
				FoveSettingsWindow.EditSettings();
			}
		}
	}

	[InitializeOnLoad]
	public class FoveSettingsWindow : EditorWindow
	{
		private static readonly string[] Tabs = { "Fixes", "Settings" };
		
		private static GUIStyle m_WordWrapLabel;
		
		private static List<SuggestedProjectFix> m_FixList;

		static FoveSettingsWindow()
		{
			m_FixList = new List<SuggestedProjectFix>(new SuggestedProjectFix[] {
				new RequireWin64Bit_Suggestion(),
				new VsyncOff_Suggestion(),
				new RunInBackground_Suggestion(),
				new VisibleInBackground_Suggestion(),
				new ForwardRendering_Suggestion(),
				new Msaa4x_Suggestion(),
				new HideResolutionDialog_Suggestion(),
				new DisableResize_Suggestion()
			});

			// Unity won't deserialize assets properly until the normal update loop,
			// so we register for the delegate, which will unregister itself when it's called.
			EditorApplication.update += RunOnce;
		}

		static bool NeedsAnySuggestions()
		{
			foreach (var suggestion in m_FixList) {
				if (!suggestion.IsOkay())
				{
					return true;
				}
			}

			return false;
		}

		static void RunOnce()
		{
			// Only run this once from the delegate
			EditorApplication.update -= RunOnce;

			EnsureSettingsExists();
			// In order to show settings:
			// * showAutomatically must be true OR no settings file exists
			// * at least one suggestion must be applicable
			if ((FoveSettings.ShouldShowAutomatically) && NeedsAnySuggestions())
			{
				EditSettings();
			}
		}

		private static void EnsureSettingsExists()
		{
			if (Resources.Load<FoveSettings>("FOVE Settings") == null)
			{
				if (!System.IO.Directory.Exists("Assets/FoveUnityPlugin/Resources"))
				{
					AssetDatabase.CreateFolder("Assets/FoveUnityPlugin", "Resources");
				}

				var temp = CreateInstance<FoveSettings>();
				AssetDatabase.CreateAsset(temp, "Assets/FoveUnityPlugin/Resources/FOVE Settings.asset");
			}
		}

		[MenuItem("FOVE/Edit Settings")]
		public static void EditSettings()
		{
			FoveSettingsWindow window = GetWindow<FoveSettingsWindow>();

			GUIContent title = new GUIContent("FOVE Settings");
			window.titleContent = title;

			window.Show();
		}

		// NON-STATIC STUFF
		private Vector2 m_ListScrollPos = new Vector2();
		private static readonly string DefaultHelpMessage = "Mouse-over suggestions for more detail.";
		private string m_HelpMessage = DefaultHelpMessage;
		private bool m_HelpMessageWasSet = false;
		private bool m_forceCheck = true;
		private int m_selectedTab = 0;

		private void OnEnable()
		{
			EnsureSettingsExists();
			m_forceCheck = true;
			wantsMouseMove = true;
		}

		private bool MouseInLastElement()
		{
			return Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);
		}

		private void ResetHelpMessage()
		{
			m_HelpMessage = DefaultHelpMessage;
		}

		private void SetHelpMessage(string s)
		{
			m_HelpMessage = s;
			m_HelpMessageWasSet = true;
		}

		private bool HandleSuggestion(SuggestedProjectFix suggestion)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(suggestion.Description);
			GUILayout.FlexibleSpace();
			bool result = GUILayout.Button("Fix");
			GUILayout.EndHorizontal();

			if (MouseInLastElement())
				SetHelpMessage(suggestion.HelpText);

			return result;
		}

		//private void Update()
		//{
		//	double t = EditorApplication.timeSinceStartup;
		//	if (t - m_LastUpdateTime > 0.1)
		//	{
		//		Repaint();
		//		m_LastUpdateTime = t;
		//	}
		//}

		private void DrawFixSuggestions()
		{
			bool hasSuggestions = false;
			bool needsCheck = false;
			foreach (var suggestion in m_FixList)
			{
				if (suggestion.IsOkay(m_forceCheck))
					continue;

				if (HandleSuggestion(suggestion))
				{
					suggestion.Fix();
					needsCheck = true;
				}

				hasSuggestions = true;
			}
			m_forceCheck = needsCheck;

			// Need something here to make sure the view 
			if (!hasSuggestions)
			{
				GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("No fixes/suggestions.");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}

			GUILayout.FlexibleSpace();

			// "Fix All" button row
			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Refesh"))
					m_forceCheck = true;
				GUILayout.FlexibleSpace();

				if (hasSuggestions)
				{
					if (GUILayout.Button("Fix All"))
					{
						foreach (var fix in m_FixList)
						{
							if (fix.IsOkay(true))
								continue;

							fix.Fix();
						}
						m_forceCheck = true;
					}
					if (MouseInLastElement())
						SetHelpMessage("Implement all available optimizations for the selected FOVE interface version.");
				} // hasSuggestions
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawElementRow(string propName, string helpText, Action call)
		{
			EditorGUILayout.BeginHorizontal();
			{
				call();
				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal();
			if (MouseInLastElement())
				SetHelpMessage(helpText);
		}

		private void DrawSettings(SerializedObject serialized)
		{
			EditorGUILayout.LabelField("Project Settings", EditorStyles.miniBoldLabel);
			EditorGUI.indentLevel++;
			
			DrawElementRow(
				"forceCalibration",
				"Force eye tracking to recalibrate every time the game is launched (including pressing the \"Play\" button in the editor.\n\nRecommended: Leave this off except for builds where users will be changing frequently, e.g., public demos/exhibitions.",
				() => { serialized.FindProperty("forceCalibration").boolValue = EditorGUILayout.Toggle("Force Calibration", FoveSettings.ShouldForceCalibration, GUILayout.ExpandWidth(false)); });
			DrawElementRow(
				"customDesktopView",
				"Allow to have the destkop main display view to be different from the HMD. When enabled, any enabled camera renders to the desktop view while any enabled Fove Interface renderers to the HMD. Having a different view for desktop display requires to perform extra rendering and is slower.",
				() => { serialized.FindProperty("customDesktopView").boolValue = EditorGUILayout.Toggle("Custom Desktop View", FoveSettings.CustomDesktopView, GUILayout.ExpandWidth(true)); });
            DrawElementRow(
				"worldScale",
				"The multiplier for how many engine-world units are in one meter. Unity assumes 1 unit = 1 meter, so you will likely want to keep this the same. If you treat 10 units as a meter, you would set this to 10. If each world unit is 10 meters, you would set this to 0.1, and so forth.",
				() => { serialized.FindProperty("worldScale").floatValue = EditorGUILayout.FloatField("World Scale", FoveSettings.WorldScale, GUILayout.ExpandWidth(false)); });
			DrawElementRow(
				"renderScale",
				"A multiplier for the rendertexture width and height to increase or decrease the resolution. This is useful to increase oversampling on scenes which can afford it; or to reduce or even undersample for scenes which are more complex. This value can be adjusted real-time by changing FoveManager.RenderScale as well.",
				() => { serialized.FindProperty("renderScale").floatValue = EditorGUILayout.Slider("Render Scale", FoveSettings.RenderScale, 0.01f, 2.0f, GUILayout.ExpandWidth(false)); });

			EditorGUI.indentLevel--;
		}

		private void OnGUI()
		{
			if (Event.current.type == EventType.MouseMove)
				Repaint();

			if (m_WordWrapLabel == null)
			{
				m_WordWrapLabel = new GUIStyle(GUI.skin.label);
				m_WordWrapLabel.wordWrap = true;
			}

			// We can only check for mouse position on repaint, so we shouldn't reset this except on repaint
			if (Event.current.type == EventType.Repaint)
				m_HelpMessageWasSet = false;

			var serialized = FoveSettings.GetSerializedObject();
			
			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();

				m_selectedTab = GUILayout.Toolbar(m_selectedTab, Tabs);
				if (MouseInLastElement())
					SetHelpMessage("Select whether you want to view project settings suggestions (with options to automatically apply them) or project-level settings for the FOVE headset.");

				GUILayout.FlexibleSpace();

				serialized.FindProperty("showHelp").boolValue = GUILayout.Toggle(FoveSettings.ShouldShowHelp, "Show Help", GUILayout.ExpandWidth(false));
				EditorGUILayout.Space();
			}
			EditorGUILayout.EndHorizontal();

			// Main section for showing fixes and help
			EditorGUILayout.BeginHorizontal();
			{
				// Contains fixes scroll view and "Fix All" button
				EditorGUILayout.BeginVertical();
				{
					m_ListScrollPos = EditorGUILayout.BeginScrollView(m_ListScrollPos, "box");
					{
						switch (m_selectedTab)
						{
							case 0:
								DrawFixSuggestions();
								break;
							case 1:
								DrawSettings(serialized);
								break;
							default:
								EditorGUILayout.LabelField("Invalid tab selected somehow...");
								break;
						}
					}
					EditorGUILayout.EndScrollView();
				}
				EditorGUILayout.EndVertical();
				
				if (FoveSettings.ShouldShowHelp)
				{
					EditorGUILayout.LabelField(m_HelpMessage, m_WordWrapLabel, GUILayout.MaxWidth(position.width * 0.33f), GUILayout.ExpandHeight(true));
				}
			}
			EditorGUILayout.EndHorizontal();

			// Final row for "Exit" button and "always show" toggle
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Exit"))
			{
				Close();
			}
			if (MouseInLastElement())
				SetHelpMessage("Close the FOVE settings window.");

			serialized.FindProperty("showAutomatically").boolValue = GUILayout.Toggle(FoveSettings.ShouldShowAutomatically, "Always Show Suggestions", GUILayout.ExpandWidth(false));
			if (MouseInLastElement())
				SetHelpMessage("Whether or not to check for available optimizations every time the plugin is reloaded (typically just on the initial project load).");
			EditorGUILayout.EndHorizontal();

			if (!m_HelpMessageWasSet)
				ResetHelpMessage();

			serialized.ApplyModifiedProperties();
		}
	}
}