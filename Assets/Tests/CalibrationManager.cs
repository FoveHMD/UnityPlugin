using Fove.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrationManager : MonoBehaviour {

	public bool ensureCalibration = true;

	// Use this for initialization
	void Start () {
		if (ensureCalibration)
			FoveManager.StartEyeTrackingCalibration(new Fove.CalibrationOptions { lazy = true });
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
