﻿using UnityEngine;
using Fove.Unity;

public class ApplyGazeForce : MonoBehaviour 
{
	public FoveInterface fove;
	
	// Update is called once per frame
	void FixedUpdate() {
		var obj = FoveManager.GetGazedObject().value;
		if (obj == null)
			return;

		var rbody = obj.GetComponent<Rigidbody>();
		if (rbody == null)
			return;

		var gazeRay = fove.GetCombinedGazeRay().value;
		rbody.AddForce(25 * gazeRay.direction);
	}
}
