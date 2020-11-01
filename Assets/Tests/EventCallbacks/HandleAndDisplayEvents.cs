using Fove.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandleAndDisplayEvents : MonoBehaviour
{
	public Text eventsText;
	public Text coroutineText;

	private class EventInfo
	{
		public float triggeredTime;
		public string eventText;
	}

	private Queue<EventInfo> eventsInfo = new Queue<EventInfo>();

	void Awake()
	{
		FoveManager.HardwareConnected += () => HandleEvent("Hardware connected");
		FoveManager.HardwareDisconnected += () => HandleEvent("Hardware disconnected");
		FoveManager.HardwareIsReady += () => HandleEvent("Hardware ready");
		FoveManager.EyeTrackingCalibrationStarted += () => HandleEvent("Calibration Started");
		FoveManager.EyeTrackingCalibrationEnded += (s) => HandleEvent("Calibration Ended: " + s);
		FoveManager.IsUserShiftingAttentionChanged += (b) => HandleEvent("User Shifting Attention changed: " + b);
		FoveManager.EyeStateChanged += (eye, c) => HandleEvent("Eye state changed: " + eye + "=" + c);
		FoveManager.UserPresenceChanged += (c) => HandleEvent("User presence changed: " + c);
		FoveManager.HmdAdjustmentGuiVisibilityChanged += (c) => HandleEvent("Hmd GUI Visibility Changed: " + c);
	}

	void Start()
	{
		StartCoroutine(WaitForAndPrint(FoveManager.WaitForHardwareConnected, "Hardware Connected"));
		StartCoroutine(WaitForAndPrint(FoveManager.WaitForHardwareDisconnected, "Hardware Disconnected"));
		StartCoroutine(WaitForAndPrint(FoveManager.WaitForHardwareReady, "Hardware Ready"));
		StartCoroutine(WaitForAndPrint(FoveManager.WaitForEyeTrackingCalibrationStart, "Calibration started"));
		StartCoroutine(WaitForAndPrint(FoveManager.WaitForEyeTrackingCalibrationEnd, "Calibration ended"));
		StartCoroutine(WaitForAndPrint(FoveManager.WaitForEyeTrackingCalibrated, "Calibrated"));
		StartCoroutine(WaitForAndPrint(FoveManager.WaitForUser, "User Present"));
	}

	void Update ()
	{
		while (eventsInfo.Count > 0 && (Time.time - eventsInfo.Peek().triggeredTime) > 1.5)
			eventsInfo.Dequeue();

		var str = "";
		foreach (var eventInfo in eventsInfo)
			str += eventInfo.eventText + "\n";

		eventsText.text = str;
	}

	private void HandleEvent(string text)
	{
		Debug.Log("Fove Event triggered: " + text);
		eventsInfo.Enqueue(new EventInfo { eventText = text, triggeredTime = Time.time });
	}

	private IEnumerator WaitForAndPrint(IEnumerator enumerator, string text)
	{
		yield return enumerator;
		Debug.Log("Coroutine awaked: " + text);
		coroutineText.text += text + "\n";
	}
}
