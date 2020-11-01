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
        var gazeRay = FoveManager.GetHmdCombinedGazeRay().value;
        var gazeDepth = FoveManager.GetCombinedGazeDepth().value;
        var dist = useGazeConvergenceDepth ? gazeDepth : distance;
        if (float.IsInfinity(dist) || float.IsNaN(dist))
            dist = distance;

        transform.localPosition = gazeRay.origin + dist * gazeRay.direction;
        pointRenderer.enabled = !FoveManager.IsEyeTrackingCalibrating();
    }
}
