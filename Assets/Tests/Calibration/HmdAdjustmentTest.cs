using UnityEngine;
using Fove.Unity;
using UnityEngine.UI;

public class HmdAdjustmentTest : MonoBehaviour 
{
	public CustomHmdAdjustmentRenderer customRenderer;

	public Text renderMode;
	public Text translation;
	public Text rotation;
	public Text isNeeded;
	public Text timeout;
	public Text visibility;
	public Text lazy;

	private bool isLazy = true;

	// Update is called once per frame
	void Update()
	{
		if (Input.GetKeyUp(KeyCode.Space))
        {
			var result = FoveManager.StartHmdAdjustmentProcess(isLazy);
			if (result.Failed)
				Debug.LogError("Failed to start HMD adjustment process. Error=" + result.error);
        }

		if (Input.GetKeyUp(KeyCode.R))
			customRenderer.gameObject.SetActive(!customRenderer.gameObject.activeSelf);

		if (Input.GetKeyUp(KeyCode.L))
			isLazy = !isLazy;

		var guiTimeout = FoveManager.HasHmdAdjustmentGuiTimeout();
		var guiVisible = FoveManager.IsHmdAdjustmentGuiVisible();

		var isCustom = customRenderer.gameObject.activeInHierarchy;
		renderMode.text = "Render Mode: "+ (isCustom ? "Custom" : "Default(companion)");
		translation.text = "Translation: " + (isCustom ? customRenderer.translationText : "N/A");
		rotation.text = "Rotation: " + (isCustom ? customRenderer.rotationText : "N/A");
		isNeeded.text = "Adjustment Needed: " + (isCustom ? customRenderer.isNeededText : "N/A");
		timeout.text = "Adjustment Timeout: " + guiTimeout.value;
		visibility.text = "Adjustment GUI Visible: " + guiVisible.value;
		lazy.text = "Lazy Adjustment: " + isLazy;

		if (!guiTimeout.IsValid)
			Debug.LogError("Gui timeout query failed. Error: " + guiTimeout.error);
		if (!guiVisible.IsValid)
			Debug.LogError("Gui visible query failed. Error: " + guiVisible.error);
	}
}
