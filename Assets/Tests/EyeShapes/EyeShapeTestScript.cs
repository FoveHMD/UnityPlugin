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
        EyeTextureRenderer.material.mainTexture = FoveResearch.EyesTexture;

        var eyeShapesResult = FoveResearch.GetEyeShapes();
        if (eyeShapesResult.HasError)
        {
            Debug.LogWarning("Failed to retrieve eye shapes");
            return;
        }

        var shapes = eyeShapesResult.value;

        int i = 0;
        foreach(var point in shapes.left.Outline)
            eyePointsLeft[i++].transform.localPosition = new Vector3(point.x, point.y, 0);
        
        int j = 0;
        foreach (var point in shapes.right.Outline)
            eyePointsRight[j++].transform.localPosition = new Vector3(point.x, point.y, 0);
    }
}
