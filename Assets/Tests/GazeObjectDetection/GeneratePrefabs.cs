using Fove.Unity;
using UnityEngine;
using UnityEngine.UI;

public class GeneratePrefabs : MonoBehaviour {

    public GameObject cubePrefab;

    public int countToInstantiate = 1000;
    public float amplitude = 10;

    public Text ObjectCountText;

    // Use this for initialization
    void Start () {
        var cubeRoot = Mathf.CeilToInt(Mathf.Pow(countToInstantiate, 0.3333f));
        var isEven = cubeRoot % 2 == 1;
        var halfRoot = cubeRoot / 2f;
        for (int i=0; i<cubeRoot; ++i)
        {
            for (int j=0; j<cubeRoot; ++j)
            {
                for (int k=0; k<cubeRoot; ++k)
                {
                    if (isEven && i == cubeRoot / 2 && j == cubeRoot / 2 && k == cubeRoot / 2)
                        continue;

                    var go = (GameObject)Instantiate(cubePrefab, transform);
                    go.transform.position = amplitude * (new Vector3(i, j, k) - halfRoot * Vector3.one);
                }
            }
        }
        GazableObject.CreateFromColliders(gameObject);

        ObjectCountText.text = "Prefab Object Count: " + cubeRoot * cubeRoot * cubeRoot;
    }
}
