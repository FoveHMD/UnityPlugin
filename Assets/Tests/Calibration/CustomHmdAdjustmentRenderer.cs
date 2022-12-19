using UnityEngine;
using UnityEngine.UI;
using Fove.Unity;
using Fove;

public class CustomHmdAdjustmentRenderer : MonoBehaviour 
{
    [HideInInspector]
    public string translationText;
    [HideInInspector]
    public string rotationText;
    [HideInInspector]
    public string isNeededText;

    public CanvasGroup hmdAdjustmentGroup;

    public Text text;
    public Image hmdImage;
    public Image[] eyeImages = new Image[2];

    private float fadeInTime;
    private float waitTime = -1;
    private bool adjustmentWasNeeded = false;

    private const float fadeDuration = 0.5f;

    void Hide()
    {
        text.enabled = false;
        hmdImage.enabled = false;
        eyeImages[0].enabled = false;
        eyeImages[1].enabled = false;
    }

    // Use this for initialization
    void Start () 
    {
        Hide();
    }
    
    // Update is called once per frame
    void Update () 
    {
        var isVisible = eyeImages[0].enabled || eyeImages[1].enabled || text.enabled || hmdImage.enabled;
        var dt = waitTime < 0 ? Time.deltaTime : 0;

        // tick and pull calibration state
        var result = FoveManager.TickHmdAdjustmentProcess(dt, isVisible);
        if (!result.IsValid)
        {
            Hide();
            text.enabled = true;

            var errorMsg = "Failed to tick hmd adjustment. Error:" + result.error;
            text.text = errorMsg;
            Debug.LogError(errorMsg);

            return;
        }

        waitTime -= Time.deltaTime;
        fadeInTime -= Time.deltaTime;

        var adjustData = result.value;
        var eulerRotation = adjustData.rotation * Mathf.Rad2Deg;
        if (adjustData.adjustmentNeeded && adjustmentWasNeeded)
        {

            if (waitTime > 0)
            {
                text.enabled = true;
                text.text = "HMD Adjustment will start in " + Mathf.CeilToInt(waitTime) + "s.";
                hmdAdjustmentGroup.alpha = Mathf.Min(1, 1 - fadeInTime / fadeDuration);
            }
            else
            {
                text.enabled = false;
                hmdImage.enabled = true;
                eyeImages[0].enabled = true;
                eyeImages[1].enabled = true;

                hmdImage.transform.localPosition = adjustData.translation * 200;
                hmdImage.transform.localEulerAngles = new Vector3(0, 0, eulerRotation);
            }
        }
        else if (adjustData.adjustmentNeeded != adjustmentWasNeeded)
        {
            fadeInTime = fadeDuration;
            waitTime = 3;
        }
        else
        {
            if (waitTime > 0)
            {
                hmdImage.enabled = false;
                eyeImages[0].enabled = false;
                eyeImages[1].enabled = false;
                text.enabled = true;
                text.text = adjustData.hasTimeout? "HMD Adjustment Failed (Timeout)" : "HMD Adjustement succeeded";
                hmdAdjustmentGroup.alpha = Mathf.Min(1, waitTime / fadeDuration);
            }
            else
            {
                Hide();
            }
        }

        adjustmentWasNeeded = adjustData.adjustmentNeeded;

        translationText = adjustData.translation.ToString();
        rotationText = eulerRotation.ToString();
        isNeededText = adjustData.adjustmentNeeded.ToString();
    }
}
