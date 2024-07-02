using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fove.Unity;

public class PrefabController : MonoBehaviour {

    public GameObject prefab1;
    public GameObject prefab2;

    public float maxDistance = 50;
    public float step = 2;

    private List<GameObject> instances = new List<GameObject>();

    // Use this for initialization
    void Start () {
    
    }
    
    // Update is called once per frame
    void Update () {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            var instance = (GameObject)Instantiate(prefab1, new Vector3(0, 0, maxDistance - instances.Count * step), Quaternion.identity);
            GazableObject.CreateFromColliders(instance);
            instances.Add(instance);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            var instance = (GameObject)Instantiate(prefab2, new Vector3(0, 0, maxDistance - instances.Count * step), Quaternion.identity);
            instances.Add(instance);
        }
        if (Input.GetKeyDown(KeyCode.Backspace) && instances.Count > 0)
        {
            Destroy(instances[instances.Count - 1]);
            instances.RemoveAt(instances.Count - 1);
        }
    }
}
