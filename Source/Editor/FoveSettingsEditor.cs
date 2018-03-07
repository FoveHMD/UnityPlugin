using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System.Collections.Generic;

namespace FoveSettings
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
		private static FoveSettings m_Settings;
		private static GUIStyle m_WordWrapLabel;

		private static readonly string[] InterfaceDescriptions =
		{
			"Dual Camera (FoveInterface)",
			"Experimental (FoveInterface2)",
		};
		private static List<SuggestedProjectFix> m_FixList;

		static FoveSettingsWindow()
		{
			m_FixList = new List<SuggestedProjectFix>(new SuggestedProjectFix[] {
				new RequireWin64Bit_Suggestion(),
				new RequireVR_Suggestion(),
				new RequireSplitVrDevice_Suggestion(),
				new NoVr_Suggestion(),
				new VsyncOff_Suggestion(),
				new RunInBackground_Suggestion(),
				new VisibleInBackground_Suggestion(),
				new ForwardRendering_Suggestion(),
				new Msaa4x_Suggestion(),
				new SinglePassRendering_Suggestion(),
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
				if (!suggestion.IsOkay(m_Settings))
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

			m_Settings = GetSettings();
			// In order to show settings:
			// * showAutomatically must be true OR no settings file exists
			// * at least one suggestion must be applicable
			if ((m_Settings == null || m_Settings.showAutomatically) && NeedsAnySuggestions())
			{
				EditSettings();
			}
		}

		private static FoveSettings GetSettings()
		{
			var result = Resources.Load<FoveSettings>("FOVE Settings");
			if (!result)
			{
				Debug.Log("[FOVE] Creating FOVE settings file...");
				result = CreateInstance<FoveSettings>();
				AssetDatabase.CreateAsset(result, "Assets/FoveUnityPlugin/Resources/FOVE Settings.asset");
			}
			
			return result;
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
		private Vector2 m_FixListScrollPos = new Vector2();
		private static readonly string DefaultHelpMessage = "Mouse-over suggestions for more detail.";
		private string m_HelpMessage = DefaultHelpMessage;
		private bool m_HelpMessageWasSet = false;
		private double m_LastUpdateTime;
		private bool m_forceCheck = true;

		private void OnEnable()
		{
			m_Settings = GetSettings();
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
			
			EditorGUILayout.BeginHorizontal();
			{
				m_Settings.interfaceChoice = (InterfaceChoice)EditorGUILayout.Popup("FOVE interface version", (int)m_Settings.interfaceChoice, InterfaceDescriptions, GUILayout.Width(350));
				if (GUI.changed)
					m_forceCheck = true;
				if (MouseInLastElement())
					SetHelpMessage("Which FOVE interface do you use in your project? You should only use one of them, and this will help us inform what optimizations will work best for you.");

				GUILayout.FlexibleSpace();
				
				m_Settings.showHelp = GUILayout.Toggle(m_Settings.showHelp, "Show Help", GUILayout.ExpandWidth(false));
				EditorGUILayout.Space();
			}
			EditorGUILayout.EndHorizontal();

			// Main section for showing fixes and help
			EditorGUILayout.BeginHorizontal();
			{
				// Contains fixes scroll view and "Fix All" button
				EditorGUILayout.BeginVertical();
				{
					bool hasSuggestions = false;
					m_FixListScrollPos = EditorGUILayout.BeginScrollView(m_FixListScrollPos, "box");
					{
						bool needsCheck = false;
						foreach (var suggestion in m_FixList)
						{
							if (suggestion.IsOkay(m_Settings, m_forceCheck))
								continue;

							if (HandleSuggestion(suggestion))
							{
								suggestion.Fix(m_Settings);
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
					}
					EditorGUILayout.EndScrollView();

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
									if (fix.IsOkay(m_Settings, true))
										continue;

									fix.Fix(m_Settings);
								}
								m_forceCheck = true;
							}
							if (MouseInLastElement())
								SetHelpMessage("Implement all available optimizations for the selected FOVE interface version.");
						} // hasSuggestions
					}
					EditorGUILayout.EndHorizontal();
				}
				EditorGUILayout.EndVertical();
				
				if (m_Settings.showHelp)
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

			m_Settings.showAutomatically = GUILayout.Toggle(m_Settings.showAutomatically, "Always Show Suggestions", GUILayout.ExpandWidth(false));
			if (MouseInLastElement())
				SetHelpMessage("Whether or not to check for available optimizations every time the plugin is reloaded (typically just on the initial project load).");
			EditorGUILayout.EndHorizontal();

			if (!m_HelpMessageWasSet)
				ResetHelpMessage();
		}
	}
}