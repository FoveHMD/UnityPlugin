#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace Fove.Unity
{
	public class FoveSettings : ScriptableObject
	{
		[SerializeField]
		private bool showHelp = true;
		[SerializeField]
		private bool showAutomatically = true;
		
		[SerializeField]
		private bool forceCalibration = false;
		[SerializeField]
		private bool customDesktopView = false;
		[SerializeField]
		private float worldScale = 1.0f;
		[SerializeField]
		private float renderScale = 1.0f;

		private static FoveSettings _instance;
		private static FoveSettings Instance
		{
			get
			{
				if (_instance == null)
					_instance = Resources.Load<FoveSettings>("FOVE Settings");

				// On first use, there may not yet be a FOVE Settings asset created. Fallback here.
				if (_instance == null)
					_instance = CreateInstance<FoveSettings>();

				return _instance;
			}
		}

#if UNITY_EDITOR
		public static SerializedObject GetSerializedObject()
		{
			return new SerializedObject(Instance);
		}
#endif

		public static bool ShouldShowHelp
		{
			get { return Instance.showHelp; }
		}

		public static bool ShouldShowAutomatically
		{
			get { return Instance.showAutomatically; }
		}

		public static bool ShouldForceCalibration
		{
			get { return Instance.forceCalibration; }
		}

		public static bool CustomDesktopView
		{
			get { return Instance.customDesktopView; }
		}

		public static float WorldScale
		{
			get { return Instance.worldScale; }
		}

		public static float RenderScale
		{
			get { return Instance.renderScale; }
		}
	}
}