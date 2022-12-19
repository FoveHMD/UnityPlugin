
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
        public Text stateText;
        public Text torsionText;
    }

    public EyeInfoTexts leftEyeInfoTexts;
    public EyeInfoTexts rightEyeInfoTexts;
    public Text ipdText;
    public Text iodText;

    static string toMilliString(Fove.Result<float> sizeInMeter)
    {
        if (!sizeInMeter.IsValid)
            return sizeInMeter.error.ToString();

        return (sizeInMeter * 1000f).ToString("F1") + "mm";
    }

    static string getEyeStateString(Fove.Eye eye)
    {
        var state = FoveManager.GetEyeState(eye).value;
        return state.ToString();
    }

    void Start()
    {
        var caps = Fove.ClientCapabilities.EyeTorsion
            | Fove.ClientCapabilities.EyeballRadius
            | Fove.ClientCapabilities.IrisRadius
            | Fove.ClientCapabilities.PupilRadius
            | Fove.ClientCapabilities.UserIOD
            | Fove.ClientCapabilities.UserIPD;

        FoveManager.RegisterCapabilities(caps);
    }

    private void UpdateEyeTexts(Fove.Eye eye)
    {
        var isLeft = eye == Fove.Eye.Left;
        var infoTexts = isLeft ? leftEyeInfoTexts : rightEyeInfoTexts;

        // eye closed or open
        infoTexts.stateText.text = getEyeStateString(eye);

        // Eye radius
        infoTexts.eyeText.text = toMilliString(FoveManager.GetEyeballRadius(eye));
        infoTexts.irisText.text = toMilliString(FoveManager.GetIrisRadius(eye));
        infoTexts.pupilText.text = toMilliString(FoveManager.GetPupilRadius(eye));

        var torsion = FoveManager.GetEyeTorsion(eye);
        var torsionText = torsion.IsValid ? torsion.value.ToString("F2") : torsion.error.ToString();
        infoTexts.torsionText.text = torsionText;
    }

    // Update is called once per frame
    void Update()
    {
        // IOD & IPD
        iodText.text = toMilliString(FoveManager.GetUserIOD());
        ipdText.text = toMilliString(FoveManager.GetUserIPD());

        // Eye radius
        UpdateEyeTexts(Fove.Eye.Left);
        UpdateEyeTexts(Fove.Eye.Right);
    }
}
