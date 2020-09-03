using UnityEngine;
using UnityEngine.UI;

public class ProfileItemController : MonoBehaviour 
{
	public InputField profileNameField;
	public Image selectedOverlay;
	public Image currentOverlay;

	[HideInInspector]
	public string ProfileName
	{
		get { return profileName; }
		set 
		{
			if (profileName == value)
				return;

			profileName = value;
			profileNameField.text = value;
		}
	}
	private string profileName;

	[HideInInspector]
	public bool isSelected;

	[HideInInspector]
	public bool isCurrent;

	public void EnterNameEditMode()
	{
		profileNameField.ActivateInputField();
	}

	public void ValidateName()
	{
		profileNameField.DeactivateInputField();
	}

	public string GetEditValue()
	{
		return profileNameField.text;
	}

	void Update () 
	{
		selectedOverlay.enabled = isSelected;
		currentOverlay.enabled = isCurrent;
	}
}
