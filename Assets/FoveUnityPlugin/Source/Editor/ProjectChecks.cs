using UnityEditor;
using UnityEngine;

namespace Fove.Unity
{
	public abstract class SuggestedProjectFix
	{
		public string Description;
		public string HelpText;
		public void Fix()
		{
			RealFix();
		}

		private bool m_CachedCheck = false;
		private bool m_hasChecked = false;
		public bool IsOkay(bool force = false) // return false if the suggestion applies
		{
			if (force || !m_hasChecked)
			{
				m_CachedCheck = RealIsOkay();
				m_hasChecked = true;
			}

			return m_CachedCheck;
		}

		protected abstract bool RealIsOkay();
		protected abstract void RealFix();
	}

	// Require that Vsync be disabled
	public class VsyncOff_Suggestion : SuggestedProjectFix
	{
		public VsyncOff_Suggestion()
		{
			Description = "Vsync should be disabled";
			HelpText = "One or more of your Quality Settings has vsync on. The FOVE interface will automatically " +
				"manage the refresh rate of your scene to match the headset, which means that if you have Vsync " +
				"enabled is likely to disrupt smooth performance in VR." +
				"\n\n" +
				"We recommend disabling Vsync in all quality settings to ensure the best possible experience for " +
				"your users.";
		}

		protected override bool RealIsOkay()
		{
			string[] qualityNames = QualitySettings.names;
			for (int i = 0; i < qualityNames.Length; i++)
			{
				QualitySettings.SetQualityLevel(i);
				if (QualitySettings.vSyncCount > 0)
				{
					return false;
				}
			}

			return true;
		}

		protected override void RealFix()
		{
			string[] qualityNames = QualitySettings.names;
			for (int i = 0; i < qualityNames.Length; i++)
			{
				QualitySettings.SetQualityLevel(i);
				QualitySettings.vSyncCount = 0;
			}
		}
	}

	// Require Windows 64-bit build settings
	public class RequireWin64Bit_Suggestion : SuggestedProjectFix
	{
		public RequireWin64Bit_Suggestion()
		{
			Description = "Set build target to Windows 64-bit";
			HelpText = "The FOVE plugin currently only supports Windows 64-bit builds. Anything else will fail " +
				"to load our libraries and won't run properly.";
		}

		protected override bool RealIsOkay()
		{
			return EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64;
		}

		protected override void RealFix()
		{
			bool success = false;
#if UNITY_5_6_OR_NEWER
				success = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
#else
				success = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.StandaloneWindows64);
#endif

			if (!success)
			{
				EditorUtility.DisplayDialog("Error Changing Build Settings",
					"Switch settings function returned false...", "Okay");
			}
		}
	}

	// Suggest that Unity run in the background
	public class RunInBackground_Suggestion : SuggestedProjectFix
	{
		public RunInBackground_Suggestion()
		{
			Description = "Run in background should be enabled";
			HelpText = "By default, Unity pauses the engine when it isn't the frontmost executable, which can " +
				"lead to accidentally freezing your VR app. Run in background will keep things smoother" +
				"regardless of which program has focus.";
		}

		protected override bool RealIsOkay()
		{
			return PlayerSettings.runInBackground;
		}

		protected override void RealFix()
		{
			PlayerSettings.runInBackground = true;
		}
	}

	// Suggest that Unity run in the background
	public class VisibleInBackground_Suggestion : SuggestedProjectFix
	{
		public VisibleInBackground_Suggestion()
		{
			Description = "Make visible in background";
			HelpText = "If your project runs full screen but if loses focus, \"Visible in Background\" will " +
				"make sure that the VR experience continues uninterrupted. (This could happen from Windows" +
				"notification popups, for instance.)";
		}

		protected override bool RealIsOkay()
		{
			return PlayerSettings.visibleInBackground;
		}

		protected override void RealFix()
		{
			PlayerSettings.visibleInBackground = true;
		}
	}

	// Suggest that Unity use forward rendering
	public class ForwardRendering_Suggestion : SuggestedProjectFix
	{
		public ForwardRendering_Suggestion()
		{
			Description = "Use forward rendering";
			HelpText = "Forward rendering is better optimised for VR, and enables the use of MSAA rather than " +
				"requiring post-process antialiasing.";
		}

		protected override bool RealIsOkay()
		{
#if UNITY_5_5_OR_NEWER
			//var low_graphics = UnityEditor.Rendering.EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Standalone, UnityEngine.Rendering.GraphicsTier.Tier1);
			//var mid_graphics = UnityEditor.Rendering.EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Standalone, UnityEngine.Rendering.GraphicsTier.Tier2);
			var high_graphics = UnityEditor.Rendering.EditorGraphicsSettings.GetTierSettings(
				BuildTargetGroup.Standalone,
				UnityEngine.Rendering.GraphicsTier.Tier3);
			
			return high_graphics.renderingPath == RenderingPath.Forward;
#else
			return PlayerSettings.renderingPath == RenderingPath.Forward;
#endif
		}

		protected override void RealFix()
		{
#if UNITY_5_5_OR_NEWER
			var high_graphics = UnityEditor.Rendering.EditorGraphicsSettings.GetTierSettings(
				BuildTargetGroup.Standalone,
				UnityEngine.Rendering.GraphicsTier.Tier3);
			high_graphics.renderingPath = RenderingPath.Forward;
			UnityEditor.Rendering.EditorGraphicsSettings.SetTierSettings(
				BuildTargetGroup.Standalone,
				UnityEngine.Rendering.GraphicsTier.Tier3,
				high_graphics);
#else
			PlayerSettings.renderingPath = RenderingPath.Forward;
#endif
		}
	}

	// Suggest that Unity use MSAA 4x
	public class Msaa4x_Suggestion : SuggestedProjectFix
	{
		public Msaa4x_Suggestion()
		{
			Description = "Use at least 4x MSAA";
			HelpText = "Antialiasing is very useful for immersion in VR. 4x MSAA is a good compromise for " +
				"quality versus performance.";
		}

		protected override bool RealIsOkay()
		{
			return QualitySettings.antiAliasing >= 4;
		}

		protected override void RealFix()
		{
			QualitySettings.antiAliasing = 4;
		}
	}

	// Suggest that developers hide the resolution dialog from users
	public class HideResolutionDialog_Suggestion : SuggestedProjectFix
	{
		public HideResolutionDialog_Suggestion()
		{
			Description = "Hide resolution dialog on startup";
			HelpText = "Unity picks the idea resolution for VR on its own, so hiding the resolution dialog " +
				"on startup will help to reduce confusion of users, and it allows your game to just start " +
				"up when launched (which is useful for Steam integration, for instance).";
		}

		protected override bool RealIsOkay()
		{
			return PlayerSettings.displayResolutionDialog == ResolutionDialogSetting.Disabled;
		}

		protected override void RealFix()
		{
			PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Disabled;
		}
	}

	// Suggest that developers disable window resizing
	public class DisableResize_Suggestion : SuggestedProjectFix
	{
		public DisableResize_Suggestion()
		{
			Description = "Disable window resizing";
			HelpText = "Unity picks the idea resolution for VR on its own, so disabling window resizing will " +
				"help ensure that the view that appears on your monitor doesn't get distorted and more " +
				"accurately represents what your user is seeing in VR.";
		}

		protected override bool RealIsOkay()
		{
			return PlayerSettings.resizableWindow == false;
		}

		protected override void RealFix()
		{
			PlayerSettings.resizableWindow = false;
		}
	}
}
