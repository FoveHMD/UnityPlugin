using UnityEngine;
using Fove.Unity;
using UnityEngine.UI;
using Fove;
using System;

public class CalibrationTest : MonoBehaviour 
{
    public Text isCalibratedText;
    public Text isCalibratingText;
    public Text stateText;
    public Text lazyText;
    public Text restartText;
    public Text eyeByEyeText;
    public Text methodText;
    public Text renderModeText;

    public GameObject customCalibrationRoot;

    public CustomCalibrationRenderer calibRenderer;

    private CalibrationOptions calibOptions = new CalibrationOptions();

    // Use this for initialization
    void Start () {
        FoveManager.TareOrientation();
    }
    
    // Update is called once per frame
    void Update ()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            FoveManager.TareOrientation();
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            calibOptions.method = (CalibrationMethod)(((int)calibOptions.method + 1) % Enum.GetNames(typeof(CalibrationMethod)).Length);
            RestartCalibrationIfRunning();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            calibRenderer.enabled = !calibRenderer.enabled;
            RestartCalibrationIfRunning();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            calibOptions.eyeByEye = (EyeByEyeCalibration)(((int)calibOptions.eyeByEye + 1) % Enum.GetNames(typeof(EyeByEyeCalibration)).Length);
            RestartCalibrationIfRunning();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            calibOptions.restart = !calibOptions.restart;
            RestartCalibrationIfRunning();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            calibOptions.lazy = !calibOptions.lazy;
            RestartCalibrationIfRunning();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            FoveManager.StartEyeTrackingCalibration(calibOptions);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            FoveManager.StopEyeTrackingCalibration();
        }

        isCalibratedText.text = "IsCalibrated: " + FoveManager.IsEyeTrackingCalibrated().value;
        isCalibratingText.text = "IsCalibrating: " + FoveManager.IsEyeTrackingCalibrating().value;
        stateText.text = "Calibration State: " + FoveManager.GetEyeTrackingCalibrationState().value;
        lazyText.text = "Lazy Calibration: " + calibOptions.lazy;
        restartText.text = "Restart Calibration: " + calibOptions.restart;
        eyeByEyeText.text = "Eye-by-eye Calibration: " + calibOptions.eyeByEye;
        methodText.text = "Calibration Method: " + calibOptions.method;
        renderModeText.text = "Custom Calibration Rendering: " + calibRenderer.enabled;
    }

    private void RestartCalibrationIfRunning()
    {
        if (!FoveManager.IsEyeTrackingCalibrating())
            return;

        var restartOpts = new CalibrationOptions
        { 
            restart = true,
            eyeByEye = calibOptions.eyeByEye,
            method = calibOptions.method,
        };
        FoveManager.StartEyeTrackingCalibration(restartOpts);
    }
}
