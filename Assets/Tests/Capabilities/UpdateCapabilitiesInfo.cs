using Fove.Unity;
using UnityEngine;
using UnityEngine.UI;

public class UpdateCapabilitiesInfo : MonoBehaviour {

    public Text ManagerOrientText;
    public Text ManagerPosText;
    public Text ManagerGazeText;
    public Text InterfaceOrientText;
    public Text InterfacePosText;
    public Text InterfaceGazeText;

    public FoveInterface fove;

    // Update is called once per frame
    void Update () {
        var pose = FoveManager.GetHMDPose().value;
        var conv = FoveManager.GetHMDGazeConvergence().value;
        var foveConv = fove.GetGazeConvergence().value;
        ManagerOrientText.text = toFomattedString(pose.orientation.ToQuaternion());
        ManagerPosText.text = toFomattedString(pose.position.ToVector3());
        ManagerGazeText.text = toFomattedString(conv.ray.direction);
        InterfaceOrientText.text = toFomattedString(fove.transform.rotation);
        InterfacePosText.text = toFomattedString(fove.transform.position);
        InterfaceGazeText.text = toFomattedString(foveConv.ray.direction);
    }

    private string toFomattedString(Vector3 v)
    {
        return string.Format("({0:F2},{1:F2},{2:F2})", v.x, v.y, v.z);
    }

    private string toFomattedString(Quaternion q)
    {
        return string.Format("({0:F2},{1:F2},{2:F2},{3:F2})", q.x, q.y, q.z, q.w);
    }
}
