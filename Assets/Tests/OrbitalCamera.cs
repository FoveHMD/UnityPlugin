using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitalCamera : MonoBehaviour {

    public float OrbitRadius = 10;

    public float Speed = 0.1f;

	void Update () {
        transform.position = OrbitRadius * new Vector3(Mathf.Cos(Speed * Time.time), 0, Mathf.Sin(Speed * Time.time));
        transform.rotation = Quaternion.LookRotation(-transform.position, Vector3.up);
	}
}
