using Fove.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandleAndDisplayEvents : MonoBehaviour
{
	public Text hdwEventsText;
	public Text CalibEventsText;
	public Text AttentionEventsText;
	public Text EyeStateEventsText;
	public Text UserPresentEventsText;
	public Text HmdAdjustEventsText;
	public Text asyncEventsText;

	void Awake()
	{
		FoveManager.HardwareConnected += () => HandleEvent(hdwEventsText, "Hardware connected");
		FoveManager.HardwareDisconnected += () => HandleEvent(hdwEventsText, "Hardware disconnected");
		FoveManager.EyeTrackingCalibrationStarted += () => HandleEvent(CalibEventsText, "Calibration Started");
		FoveManager.EyeTrackingCalibrationEnded += (s) => HandleEvent(CalibEventsText, "Calibration Ended: " + s);
		FoveManager.IsUserShiftingAttentionChanged += (b) => HandleEvent(AttentionEventsText, "Shifted Attention: " + b);
		FoveManager.EyeStateChanged += (eye, c) => HandleEvent(EyeStateEventsText, "Eye state: " + eye + "=" + c);
		FoveManager.UserPresenceChanged += (c) => HandleEvent(UserPresentEventsText, "User present: " + c);
		FoveManager.HmdAdjustmentGuiVisibilityChanged += (c) => HandleEvent(HmdAdjustEventsText, "Hmd GUI Visible: " + c);
	}

    void Start()
	{
        FoveManager.RegisterCapabilities(Fove.ClientCapabilities.EyeTracking);
        FoveManager.RegisterCapabilities(Fove.ClientCapabilities.UserPresence);
        FoveManager.RegisterCapabilities(Fove.ClientCapabilities.UserAttentionShift);
        FoveManager.RegisterCapabilities(Fove.ClientCapabilities.EyeBlink);

		StartCoroutine(PrintConnectedStatus());
		StartCoroutine(WaitForAndPrint(FoveManager.WaitForEyeTrackingCalibrated, "Calibrated"));
        StartCoroutine(PrintCalibrationStarted());
        StartCoroutine(PrintUserPresent());
	}

    void AppendEventText(Text textBlock, string evtText)
    {
        var lines = textBlock.text.Split('\n');
        var s = textBlock.GetComponent<RectTransform>().rect.height;
        var fs = textBlock.fontSize;
        var lc = lines.Length;
        var endIdx = (lc + 1) * 1.1 * fs > s ? 1 : 0;

        var str = evtText;
        for (int i = 0; i < lines.Length - endIdx; ++i)
            str += "\n" + lines[i];

        textBlock.text = str;
    }

	private void HandleEvent(Text textBlock, string text)
	{
		Debug.Log("Fove Event triggered: " + text);
        AppendEventText(textBlock, text);

    }

    private IEnumerator WaitForAndPrint(IEnumerator enumerator, string text)
	{
		yield return enumerator;
		Debug.Log("Coroutine awaked: " + text);
        AppendEventText(asyncEventsText, text);
    }

    private IEnumerator PrintConnectedStatus()
    {
        yield return WaitForAndPrint(FoveManager.WaitForHardwareConnected, "Hardware Connected");
        yield return PrintDisconnectedStatus();
    }

    private IEnumerator PrintDisconnectedStatus()
    {
        yield return WaitForAndPrint(FoveManager.WaitForHardwareDisconnected, "Hardware Disconnected");
        yield return PrintConnectedStatus();
    }

    private IEnumerator PrintCalibrationStarted()
    {
        yield return WaitForAndPrint(FoveManager.WaitForEyeTrackingCalibrationStart, "Calibration started");
        yield return PrintCalibrationEnded();
    }

    private IEnumerator PrintCalibrationEnded()
    {
        yield return WaitForAndPrint(FoveManager.WaitForEyeTrackingCalibrationEnd, "Calibration ended");
        yield return WaitForAndPrint(FoveManager.WaitForEyeTrackingCalibrated, "Calibrated");
        yield return PrintCalibrationStarted();
    }

    private IEnumerator PrintUserPresent()
    {
        yield return WaitForAndPrint(FoveManager.WaitForUser, "User Present");
        yield return PrintUserLeft();

    }

    private IEnumerator PrintUserLeft()
    {
        yield return WaitForAndPrint(new WaitUntil(() => !FoveManager.IsUserPresent()), "User Left");
        yield return PrintUserPresent();
    }
}
