using UnityEngine;
using System.Collections;

public class MoveObjectHorizontally : MonoBehaviour {

    public float speed = 1f;

    public float amplitude = 5f;

    public float distance = 10f;

    // Use this for initialization
    void Start () {
    
    }
    
    // Update is called once per frame
    void Update () {
        transform.position = new Vector3(amplitude * Mathf.Sin(speed * Time.time), 0, distance);
    }
}
