using UnityEditor;
using UnityEngine;

namespace FoveSettings
{
	public abstract class SuggestedProjectFix
	{
		public string Description;
		public string HelpText;
		public void Fix(FoveSettings settings)
		{
			RealFix(settings);
		}

		private bool m_CachedCheck = false;
		private bool m_hasChecked = false;
		public bool IsOkay(FoveSettings settings, bool force = false) // return false if the suggestion applies
		{
			if (force || !m_hasChecked)
			{
				m_CachedCheck = RealIsOkay(settings);
				m_hasChecked = true;
			}

			return m_CachedCheck;
		}

		protected abstract bool RealIsOkay(FoveSettings settings);
		protected abstract void RealFix(FoveSettings settings);
	}

	// Require that VR be enabled when using the experimental FoveInterface2 setting
	public class RequireVR_Suggestion : SuggestedProjectFix
	{
		public RequireVR_Suggestion()
		{
			Description = "VR is not enabled";
			HelpText = "The selected FOVE interface requires VR to be enabled in your project settings " +
				"in order to function. This allows it to take advantage of Unity's internal VR optimisations." +
				"\n\n" +
				"Be aware that this setting is experimental. There may be some performance " +
				"issues and you may see Unity-engine warnings or errors reported.";
		}

		protected override bool RealIsOkay(FoveSettings settings)
		{
			return PlayerSettings.virtualRealitySupported || settings.interfaceChoice != InterfaceChoice.VrRenderPath;
		}

		protected override void RealFix(FoveSettings settings)
		{
			PlayerSettings.virtualRealitySupported = true;

			var splitDevice_Requirement = new RequireSplitVrDevice_Suggestion();
			if (!splitDevice_Requirement.IsOkay(settings, true))
			{
				splitDevice_Requirement.Fix(settings);
			}
		}
	}

	// Require that the "split" stereo device exist when using the experimental FoveInterface2
	public class RequireSplitVrDevice_Suggestion : SuggestedProjectFix
	{
		public RequireSplitVrDevice_Suggestion()
		{
			Description = "FOVE needs the \"split\" VR device";
			HelpText = "FOVE uses the built-in \"Split Stereo Display (non head-mounted)\" VR device to take " +
				"advantage of Unity's stereo-rendering optimzations, even though it claims to not be for " +
				"head-mounted displays." +
				"\n\n" +
				"Your selected FOVE interface requires that this device be present in your VR device list.";
		}

		// Helper function to keep the call site cleaner now that we need the version #ifs
		private string[] GetSupportedDevices()
		{
#if UNITY_2017_2_OR_NEWER
			string[] devices = UnityEngine.XR.XRSettings.supportedDevices;
#else
			string[] devices = UnityEngine.VR.VRSettings.supportedDevices;
#endif
			return devices;
		}

		protected override bool RealIsOkay(FoveSettings settings)
		{
			string[] devices = GetSupportedDevices();
			if (settings.interfaceChoice != InterfaceChoice.VrRenderPath)
				return true;
			if (PlayerSettings.virtualRealitySupported != true)
				return true; // we cannot determine if this suggestion applies when VR is disabled

			for (int i = 0; i < devices.Length; i++)
			{
				if (devices[i] == "split")
				{
					return true;
				}
			}

			return false;
		}

		protected override void RealFix(FoveSettings settings)
		{
			string[] devices = GetSupportedDevices();
			string[] new_devices;

			bool skipCopy = false;
			if (devices.Length == 1 && devices[0] == "None")
			{
				new_devices = new string[1];
				skipCopy = true;
			}
			else
			{
				new_devices = new string[devices.Length + 1];
			}

			int i = 1;
			new_devices[0] = "split";
			if (!skipCopy)
			{
				foreach (var device in devices)
				{
					new_devices[i++] = device;
				}
			}

#if UNITY_5_5_OR_NEWER
				UnityEditorInternal.VR.VREditor.SetVREnabledDevicesOnTargetGroup(BuildTargetGroup.Standalone, new_devices);
#else
				UnityEditorInternal.VR.VREditor.SetVREnabledDevices(BuildTargetGroup.Standalone, new_devices);
#endif
		}
	}

	// Require that VR be disabled when using the stable FoveInterface
	public class NoVr_Suggestion : SuggestedProjectFix
	{
		public NoVr_Suggestion()
		{
			Description = "VR should not be enabled";
			HelpText = "The selected FOVE interface works best when VR is disabled, and having VR enabled could " +
				"cause performance problems and graphical anomalies.";
		}

		protected override bool RealIsOkay(FoveSettings settings)
		{
			return !PlayerSettings.virtualRealitySupported || settings.interfaceChoice != InterfaceChoice.DualCameras;
		}

		protected override void RealFix(FoveSettings settings)
		{
			PlayerSettings.virtualRealitySupported = false;
		}
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

		protected override bool RealIsOkay(FoveSettings settings)
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

		protected override void RealFix(FoveSettings settings)
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

		protected override bool RealIsOkay(FoveSettings settings)
		{
			return EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64;
		}

		protected override void RealFix(FoveSettings settings)
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

		protected override bool RealIsOkay(FoveSettings settings)
		{
			return PlayerSettings.runInBackground;
		}

		protected override void RealFix(FoveSettings settings)
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

		protected override bool RealIsOkay(FoveSettings settings)
		{
			return PlayerSettings.visibleInBackground;
		}

		protected override void RealFix(FoveSettings settings)
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

		protected override bool RealIsOkay(FoveSettings settings)
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

		protected override void RealFix(FoveSettings settings)
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

		protected override bool RealIsOkay(FoveSettings settings)
		{
			return QualitySettings.antiAliasing >= 4;
		}

		protected override void RealFix(FoveSettings settings)
		{
			QualitySettings.antiAliasing = 4;
		}
	}

	// Suggest that Unity use MSAA 4x
	public class SinglePassRendering_Suggestion : SuggestedProjectFix
	{
		public SinglePassRendering_Suggestion()
		{
			Description = "Use single-pass rendering";
			HelpText = "Single-pass rendering enables Unity to go over the scene graph only once, drawing " +
				"objects to both eyes. This saves time on occlusion culling and shadow rendering, among other " +
				"areas. If you are using non-default full-screen image effects, you should check with your " +
				"plugin developer to make sure they work with single-pass rendering before enabling it.";
		}

		protected override bool RealIsOkay(FoveSettings settings)
		{
			if (settings.interfaceChoice == InterfaceChoice.DualCameras)
				return true;

			return PlayerSettings.stereoRenderingPath == StereoRenderingPath.SinglePass
#if UNITY_5_5_OR_NEWER
				|| PlayerSettings.stereoRenderingPath == StereoRenderingPath.Instancing
#endif
				;
		}

		protected override void RealFix(FoveSettings settings)
		{
			PlayerSettings.stereoRenderingPath = StereoRenderingPath.SinglePass;
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

		protected override bool RealIsOkay(FoveSettings settings)
		{
			return PlayerSettings.displayResolutionDialog == ResolutionDialogSetting.Disabled;
		}

		protected override void RealFix(FoveSettings settings)
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

		protected override bool RealIsOkay(FoveSettings settings)
		{
			return PlayerSettings.resizableWindow == false;
		}

		protected override void RealFix(FoveSettings settings)
		{
			PlayerSettings.resizableWindow = false;
		}
	}
}
