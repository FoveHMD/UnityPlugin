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
    public Text eyeTorsionText;
    public Text methodText;
    public Text renderModeText;
    public Text customHmdAdjustment;
    public Text calibStateInfo;
    public Text runningMethod;
    public Text calibPoints;

    public GameObject customCalibrationRoot;

    public CustomCalibrationRenderer calibRenderer;
    public CustomHmdAdjustmentRenderer hmdAdjustRenderer;

    private CalibrationOptions calibOptions = new CalibrationOptions();

    // Use this for initialization
    void Start () {
        FoveManager.TareOrientation();
    }
    
    // Update is called once per frame
    void Update ()
    {
        if (Input.GetKeyDown(KeyCode.T) && Input.GetKey(KeyCode.LeftControl))
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
        if (Input.GetKeyDown(KeyCode.T))
        {
            calibOptions.eyeTorsion = (EyeTorsionCalibration)(((int)calibOptions.eyeTorsion + 1) % Enum.GetNames(typeof(EyeTorsionCalibration)).Length);
            RestartCalibrationIfRunning();
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            hmdAdjustRenderer.gameObject.SetActive(!hmdAdjustRenderer.gameObject.activeSelf);
        }

        var calibStateDetails = FoveManager.GetEyeTrackingCalibrationStateDetails();

        lazyText.text = "- Lazy Calibration: " + calibOptions.lazy;
        restartText.text = "- Restart Calibration: " + calibOptions.restart;
        eyeByEyeText.text = "- Eye-by-eye Calibration: " + calibOptions.eyeByEye;
        eyeTorsionText.text = "- Eye Torsion Calibration: " + calibOptions.eyeTorsion;
        methodText.text = "- Calibration Method: " + calibOptions.method;
        renderModeText.text = "- Skinned CalibrationRendering: " + calibRenderer.enabled;
        customHmdAdjustment.text = "- Skinned Hmd adjustment: " + hmdAdjustRenderer.gameObject.activeInHierarchy;

        isCalibratedText.text = "- IsCalibrated: " + FoveManager.IsEyeTrackingCalibrated().value;
        isCalibratingText.text = "- IsCalibrating: " + FoveManager.IsEyeTrackingCalibrating().value;
        runningMethod.text = "- Running Method: " + calibStateDetails.value.method;
        stateText.text = "- Calibration State: " + FoveManager.GetEyeTrackingCalibrationState().value;
        calibStateInfo.text = "- State Info: " + calibStateDetails.value.stateInfo;
        calibPoints.text = "- Calib Points:"
            + "\n\t- L: " + Format(calibStateDetails.value.targets.left)
            + "\n\t- R: " + Format(calibStateDetails.value.targets.right);
    }

    private static string Format(Fove.Unity.CalibrationTarget target)
    {
        return "p=" + target.position + ", s=" + target.recommendedSize;
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
            eyeTorsion = calibOptions.eyeTorsion
        };
        FoveManager.StartEyeTrackingCalibration(restartOpts);
    }
}
