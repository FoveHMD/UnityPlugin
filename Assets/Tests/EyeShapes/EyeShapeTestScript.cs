using UnityEngine;
using Fove.Unity;
using UnityEngine.UI;

public class EyeShapeTestScript : MonoBehaviour 
{
    public Renderer EyeTextureRenderer;
    public GameObject eyePointUpPrefab;
    public GameObject eyePointDownPrefab;
    public GameObject pupilPrefab;

    public Transform eyePointRootLeft;
    public Transform eyePointRootRight;

    public bool displayEyeShape = true;
    public bool displayPupilShape = true;

    private GameObject pupilLeft;
    private GameObject pupilRight;

    private GameObject[] eyePointsLeft = new GameObject[EyeShape.OutlinePointCount];
    private GameObject[] eyePointsRight = new GameObject[EyeShape.OutlinePointCount];

    // Use this for initialization
    void Start () 
    {
        FoveManager.RegisterCapabilities(Fove.ClientCapabilities.EyesImage);

        if (displayEyeShape)
        {
            FoveManager.RegisterCapabilities(Fove.ClientCapabilities.EyeShape);
            for (var i = 0; i < EyeShape.OutlinePointCount; ++i)
            {
                var prefab = i < 7 ? eyePointDownPrefab : eyePointUpPrefab;
                eyePointsLeft[i] = (GameObject)Instantiate(prefab, eyePointRootLeft, false);
                eyePointsRight[i] = (GameObject)Instantiate(prefab, eyePointRootRight, false);

                var size = 7f;
                eyePointsLeft[i].transform.localScale = size * new Vector3(1, -1, 1);
                eyePointsRight[i].transform.localScale = size * new Vector3(1, -1, 1);

                eyePointsLeft[i].GetComponentInChildren<Text>().text = i.ToString();
                eyePointsRight[i].GetComponentInChildren<Text>().text = i.ToString();
            }
        }
        if (displayPupilShape)
        {
            FoveManager.RegisterCapabilities(Fove.ClientCapabilities.PupilShape);
            pupilLeft = (GameObject)Instantiate(pupilPrefab, eyePointRootLeft, false);
            pupilRight = (GameObject)Instantiate(pupilPrefab, eyePointRootRight, false);
        }
    }
    
    // Update is called once per frame
    void Update () 
    {
        EyeTextureRenderer.material.mainTexture = FoveManager.GetEyesImage();
        if (displayEyeShape)
        {
            UpdateEyeShape(Fove.Eye.Left);
            UpdateEyeShape(Fove.Eye.Right);
        }
        if (displayPupilShape)
        {
            UpdatePupilShape(Fove.Eye.Left);
            UpdatePupilShape(Fove.Eye.Right);
        }
    }

    void UpdateEyeShape(Fove.Eye eye)
    {
        var shapeResult = FoveManager.GetEyeShape(eye);
        if (!shapeResult.IsValid)
        {
            Debug.LogWarning("Failed to retrieve eye shape: " + shapeResult.error);
            return;
        }

        var shapes = shapeResult.value;
        var eyePoints = eye == Fove.Eye.Left ? eyePointsLeft : eyePointsRight;

        int i = 0;
        foreach (var point in shapes.Outline)
            eyePoints[i++].transform.localPosition = new Vector3(point.x, point.y, 0);
    }

    void UpdatePupilShape(Fove.Eye eye)
    {
        var shapeResult = FoveManager.GetPupilShape(eye);
        if (!shapeResult.IsValid)
        {
            Debug.LogWarning("Failed to retrieve pupil shape: " + shapeResult.error);
            return;
        }

        var shape = shapeResult.value;
        var pupil = eye == Fove.Eye.Left ? pupilLeft : pupilRight;

        pupil.transform.localPosition = new Vector3(shape.center.x, shape.center.y, 0);
        pupil.transform.localScale = new Vector3(shape.size.x, shape.size.y, 1);
        pupil.transform.localRotation = Quaternion.Euler(0, 0, shape.angle);
    }
}
