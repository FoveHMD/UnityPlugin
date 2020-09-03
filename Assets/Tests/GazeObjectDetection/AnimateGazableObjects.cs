using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimateGazableObjects : MonoBehaviour {

    public List<Transform> objects = new List<Transform>();

    public Vector3 spinSpeed = new Vector3(0.025f, 0.1f, 0.025f);
    public float minScale = 0.25f;
    public float maxScale = 2.5f;

    public float distance = 10;
    public float radius = 5;
    public float rotationSpeed = 0.75f;
    
    // Update is called once per frame
    void Update () 
    {
        var s = Time.deltaTime * spinSpeed;

        var phaseStep = 2 * Mathf.PI / objects.Count;

        var deltaS = maxScale - minScale;
        var meanS = (maxScale + minScale) / 2;

        for (var i=0; i< objects.Count; ++i)
        {
            var objT = objects[i];

            // the rotation
            objT.localRotation *= Quaternion.Euler(s.x, s.y, s.z);

            // the scale
            var sinS1 = Mathf.Sin(Time.time);
            var sinS2 = Mathf.Sin(Time.time + 2 * Mathf.PI / 3);
            var sinS3 = Mathf.Sin(Time.time + 4 * Mathf.PI / 3);
            if (objT.name == "Sphere")
            {
                // non-uniform scales are not supported for sphere
                sinS2 = sinS1; 
                sinS3 = sinS1;
            }
            objT.localScale = meanS * Vector3.one + deltaS / 2 * new Vector3(sinS1, sinS2, sinS3);

            // the position
            var cosP = Mathf.Cos(rotationSpeed * Time.time + phaseStep * i);
            var sinP = Mathf.Sin(rotationSpeed * Time.time + phaseStep * i);
            objT.localPosition = new Vector3(radius * cosP, radius * sinP, distance);
        }
    }
}
