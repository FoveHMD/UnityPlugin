using UnityEngine;

namespace FoveSettings
{
	public enum InterfaceChoice
	{
		DualCameras,
		VrRenderPath,
	}

	public class FoveSettings : ScriptableObject
	{
		public InterfaceChoice interfaceChoice;
		public bool showHelp = true;
		public bool showAutomatically;
	}
}