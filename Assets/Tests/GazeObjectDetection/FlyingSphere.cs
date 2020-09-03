using Fove.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingSphere : MonoBehaviour
{
    private Vector3 center;
    private float speed = 0.5f;
    private float trajectoryRadius = 1f;
    void Start()
    {
        //Setup trajectory
        center = transform.position;
        trajectoryRadius = Random.Range(2f, 6.0f);
        speed = Random.Range(1f, 5f) / trajectoryRadius;
        transform.rotation = Random.rotationUniform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = center + trajectoryRadius*transform.TransformDirection(new Vector3(Mathf.Cos(Time.time * speed), Mathf.Sin(Time.time * speed), 0));
    }
}
