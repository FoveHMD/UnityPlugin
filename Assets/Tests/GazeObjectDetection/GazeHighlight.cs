using UnityEngine;
using System.Collections;
using Fove.Unity;
using UnityEngine.UI;

public class GazeHighlight : MonoBehaviour {

    public GameObject gazedReference;
    private Material mat;

    // Use this for initialization
    void Start () 
    {
        if (gazedReference == null)
            gazedReference = gameObject;

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            mat = GetComponent<Renderer>().material;
        else
            mat = GetComponent<Image>().material;
    }
    
    // Update is called once per frame
    void Update () 
    {
        if (FoveManager.GetGazedObject() == gazedReference)
        {
            mat.color = Color.yellow;
        }
        else
        {
            mat.color = Color.gray;
        }
    }
}
