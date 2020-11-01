using UnityEngine;
using UnityEngine.UI;
using Fove.Unity;

public class CustomCalibrationRenderer : MonoBehaviour 
{
    public SpriteRenderer[] spriteRenders = new SpriteRenderer[2];

    private Text text;
    private Canvas canvas;

    private float timeSinceCompletion;

    // Use this for initialization
    void Start () 
    {
        text = GetComponentInChildren<Text>();
        canvas = GetComponentInChildren<Canvas>();

        canvas.enabled = false;
        spriteRenders[0].enabled = false;
        spriteRenders[1].enabled = false;
        timeSinceCompletion = float.PositiveInfinity;
    }
    
    // Update is called once per frame
    void Update () 
    {
        var isVisible = spriteRenders[0].enabled || spriteRenders[1].enabled || canvas.enabled;

        // tick and pull calibration state
        var result = FoveManager.TickEyeTrackingCalibration(Time.deltaTime, isVisible);
        if (result.Failed)
        {
            Debug.LogError("Failed to tick calibration. Error:" + result.error);
            spriteRenders[0].enabled = false;
            spriteRenders[1].enabled = false;
            canvas.enabled = false;
            return;
        }

        // update the target position
        var calibrationData = result.value;
        var targets = calibrationData.targets;

        for (int i=0; i<targets.ElementCount; i++)
        {
            var target = calibrationData.targets[i];
            var spriteRenderer = spriteRenders[i];

            if (target.recommendedSize > 0)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.transform.localPosition = target.position;
                spriteRenderer.transform.localScale = 4f * target.recommendedSize * Vector3.one;
            }
            else
            {
                spriteRenderer.enabled = false;
            }
        }

        // update the text
        canvas.enabled = true;
        switch (calibrationData.state)
        {
            case Fove.CalibrationState.WaitingForUser:
                text.text = "Look at the target";
                break;
            case Fove.CalibrationState.ProcessingData:
                text.text = "Processing...";
                break;
            case Fove.CalibrationState.Successful_HighQuality:
                text.text = "Successful high quality!";
                break;
            case Fove.CalibrationState.Successful_MediumQuality:
                text.text = "Successful medium quality";
                break;
            case Fove.CalibrationState.Successful_LowQuality:
                text.text = "Successful low quality";
                break;
            case Fove.CalibrationState.Failed_Aborted:
                text.text = "Calibration Aborted";
                break;
            case Fove.CalibrationState.Failed_InaccurateData:
                text.text = "Calibration failed: Inaccurate data";
                break;
            case Fove.CalibrationState.Failed_NoRenderer:
                text.text = "Calibration failed: no renderer";
                break;
            case Fove.CalibrationState.Failed_NoUser:
                text.text = "Calibration failed: no user";
                break;
            case Fove.CalibrationState.Failed_Unknown:
                text.text = "Calibration failed: unknown issue";
                break;
            case Fove.CalibrationState.NotStarted:
            case Fove.CalibrationState.CollectingData:
            default:
                canvas.enabled = false;
                break;
        }

        if (calibrationData.state.IsSuccessful() || calibrationData.state.IsFailure())
        {
            timeSinceCompletion += Time.deltaTime;
            if (timeSinceCompletion > 1.5)
                canvas.enabled = false;
        }
        else
        {
            timeSinceCompletion = 0;
        }
    }
}
