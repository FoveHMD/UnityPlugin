using UnityEngine;

public class LoadGlobalPrefab : MonoBehaviour
{
    public GameObject prefab;

    private void Awake()
    {
        var instance = Instantiate(prefab);
        DontDestroyOnLoad(instance);
    }
}
