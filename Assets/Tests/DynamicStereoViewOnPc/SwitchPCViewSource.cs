using Fove.Unity;
using UnityEngine;

public class SwitchPCViewSource : MonoBehaviour {

    public Camera customPCViewCamera;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyUp(KeyCode.Space))
        {
            FoveManager.UseVRStereoViewOnPC = !FoveManager.UseVRStereoViewOnPC;
            customPCViewCamera.enabled = !FoveManager.UseVRStereoViewOnPC;
        }
	}
}
