
using Fove.Unity;
using System;
using UnityEngine;
using UnityEngine.UI;

public class UpdateEyesInfo : MonoBehaviour
{
    [Serializable]
    public struct EyeInfoTexts
    {
        public Text eyeText;
        public Text irisText;
        public Text pupilText;
        public Text closedText;
    }

    public EyeInfoTexts leftEyeInfoTexts;
    public EyeInfoTexts rightEyeInfoTexts;
    public Text ipdText;
    public Text iodText;    

    static string toMilliString(float sizeInMeter)
    {
        return (sizeInMeter * 1000f).ToString("F1") + "mm";
    }

    static string getEyeClosedString(Fove.Eye eye)
    {
        var closedEyes = FoveManager.CheckEyesClosed();
        var eyeClosed = (eye & closedEyes) != 0;
        return eyeClosed.ToString();
    }

    private void UpdateEyeTexts(Fove.ResearchGaze gaze, Fove.Eye eye)
    {
        var isLeft = eye == Fove.Eye.Left;
        var infoTexts = isLeft ? leftEyeInfoTexts : rightEyeInfoTexts;
        var eyeData = isLeft ? gaze.eyeDataLeft : gaze.eyeDataRight;

        // eye closed or open
        infoTexts.closedText.text = getEyeClosedString(eye);

        // Eye radius
        infoTexts.eyeText.text = toMilliString(eyeData.eyeballRadius);
        infoTexts.irisText.text = toMilliString(eyeData.irisRadius);
        infoTexts.pupilText.text = toMilliString(eyeData.pupilRadius);
    }

    // Update is called once per frame
    void Update()
    {
        var gaze = FoveResearch.GetResearchGaze().value;

        // IOD & IPD
        iodText.text = toMilliString(gaze.iod);
        ipdText.text = toMilliString(gaze.ipd);

        // Eye radius
        UpdateEyeTexts(gaze, Fove.Eye.Left);
        UpdateEyeTexts(gaze, Fove.Eye.Right);
    }
}
