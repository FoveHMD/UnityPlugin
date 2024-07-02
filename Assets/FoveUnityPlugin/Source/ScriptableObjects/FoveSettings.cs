#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Linq;
using UnityEngine;

namespace Fove.Unity
{
    public class FoveSettings : ScriptableObject
    {
        [SerializeField]
        private bool showAutomatically = true;

        [SerializeField]
        private bool ensureCalibration = false;
        [SerializeField]
        private bool customDesktopView = false; // Do not rename this variable for backward compability as it is serialized
        [SerializeField]
        private float worldScale = 1.0f;
        [SerializeField]
        private float renderScale = 1.0f;
        [SerializeField]
        private bool automaticObjectRegistration = false;

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

        public static bool ShouldShowAutomatically
        {
            get { return Instance.showAutomatically; }
        }
#endif

        public static bool EnsureCalibration
        {
            get { return Instance.ensureCalibration; }
        }

        public static bool UseVRStereoViewOnPC
        {
            get { return !Instance.customDesktopView; }
        }

        public static float WorldScale
        {
            get { return Instance.worldScale; }
        }

        public static float RenderScale
        {
            get { return Instance.renderScale; }
        }

        public static bool AutomaticObjectRegistration
        {
            get { return Instance.automaticObjectRegistration; }
        }

        public static bool IsUsingOpenVR
        {
            get
            {
                bool vrEnabled;
                string[] vrSupportedDevices;

                vrEnabled = UnityEngine.XR.XRSettings.enabled;
                vrSupportedDevices = UnityEngine.XR.XRSettings.supportedDevices;

                return vrEnabled && vrSupportedDevices.Any(d => d == "OpenVR");
            }
        }
    }
}
