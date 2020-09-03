using UnityEngine;
using Fove.Unity;

public class UpdateGazePointer : MonoBehaviour {

    public float distance = 1;
    public bool useGazeConvergenceDepth = false;

    private Renderer pointRenderer;

    void Start()
    {
        pointRenderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update() {
        var gazeConv = FoveManager.GetHMDGazeConvergence().value;
        var dist = useGazeConvergenceDepth ? gazeConv.distance : distance;
        if (float.IsInfinity(dist) || float.IsNaN(dist))
            dist = distance;

        transform.localPosition = gazeConv.ray.origin + dist * gazeConv.ray.direction;
        pointRenderer.enabled = !FoveManager.IsEyeTrackingCalibrating();
    }
}
