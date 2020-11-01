using UnityEngine;
using Fove.Unity;
using UnityEngine.UI;

public class TestCompositorOptions : MonoBehaviour {

    public FoveInterface fove;
    public Text status;
    public Renderer checkerRenderer;

    public int targetFPS = 20;

    void Start ()
    {
        Application.targetFrameRate = targetFPS;
    }
    
    // Update is called once per frame
    void Update () {
        //if (Input.GetKeyDown(KeyCode.D))
        //    fove.DistortionDisabled = !fove.DistortionDisabled;
        if (Input.GetKeyDown(KeyCode.T))
            fove.TimewarpDisabled = !fove.TimewarpDisabled;
        //if (Input.GetKeyDown(KeyCode.F))
        //    fove.FadingDisabled = !fove.FadingDisabled;

        status.text = "Status:"
            //+ "\n- Distortion: " + GetStatusText(fove.DistortionDisabled)
            + "\n- Timewarp: " + GetStatusText(fove.TimewarpDisabled)
            //+ "\n- Fading: " + GetStatusText(fove.FadingDisabled)
            ;

        // update the material color
        checkerRenderer.material.color = new Color(
            /*fove.DistortionDisabled*/ false? 0: 1, 
            fove.TimewarpDisabled ? 0 : 1, 
            /*fove.FadingDisabled*/ false ? 0 : 1);
    }

    private static string GetStatusText(bool disabled)
    {
        return disabled ? "OFF" : "ON";
    }

    void OnApplicationQuit()
    {
        Application.targetFrameRate = -1;
    }
}
