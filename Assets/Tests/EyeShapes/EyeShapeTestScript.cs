using UnityEngine;
using Fove.Unity;
using UnityEngine.UI;

public class EyeShapeTestScript : MonoBehaviour 
{
    public Renderer EyeTextureRenderer;
    public GameObject eyePointPrefab;

    public Transform eyePointRootLeft;
    public Transform eyePointRootRight;

    private GameObject[] eyePointsLeft = new GameObject[EyeShape.OutlinePointCount];
    private GameObject[] eyePointsRight = new GameObject[EyeShape.OutlinePointCount];

    // Use this for initialization
    void Start () 
    {
        FoveManager.RegisterCapabilities(Fove.ClientCapabilities.EyesImage | Fove.ClientCapabilities.EyeShape);

        for (var i = 0; i < EyeShape.OutlinePointCount; ++i)
        {
            eyePointsLeft[i] = (GameObject)Instantiate(eyePointPrefab, eyePointRootLeft, false);
            eyePointsRight[i] = (GameObject)Instantiate(eyePointPrefab, eyePointRootRight, false);

            var size = 7f;
            eyePointsLeft[i].transform.localScale = size * new Vector3(1, -1, 1);
            eyePointsRight[i].transform.localScale = size * new Vector3(1, -1, 1);

            eyePointsLeft[i].GetComponentInChildren<Text>().text = i.ToString();
            eyePointsRight[i].GetComponentInChildren<Text>().text = i.ToString();
        }
    }
    
    // Update is called once per frame
    void Update () 
    {
        EyeTextureRenderer.material.mainTexture = FoveManager.GetEyesImage();
        UpdateEyeShape(Fove.Eye.Left);
        UpdateEyeShape(Fove.Eye.Right);
    }

    void UpdateEyeShape(Fove.Eye eye)
    {
        var shapeResult = FoveManager.GetEyeShape(eye);
        if (shapeResult.Failed)
        {
            Debug.LogWarning("Failed to retrieve eye shape");
            return;
        }

        var shapes = shapeResult.value;
        var eyePoints = eye == Fove.Eye.Left ? eyePointsLeft : eyePointsRight;

        int i = 0;
        foreach (var point in shapes.Outline)
            eyePoints[i++].transform.localPosition = new Vector3(point.x, point.y, 0);
    }
}
