using UnityEngine;
using UnityEngine.UI;
using Fove.Unity;
using System.Collections.Generic;

public class ProfileManager : MonoBehaviour {

	public VerticalLayoutGroup layout;
	public GameObject profileItemPrefab;
	public Text dataPathText;

	private int selectedIndex;
	private List<ProfileItemController> controllers = new List<ProfileItemController>();

	void Start () 
	{
		var profileNames = FoveManager.ListProfiles().value;
		foreach (var profile in profileNames)
		{
			var item = GameObject.Instantiate(profileItemPrefab);
			item.transform.SetParent(layout.transform, false);

			var controller = item.GetComponent<ProfileItemController>();
			controller.ProfileName = profile;
			controllers.Add(controller);
		}
	}
	
	void Update ()
	{
		if (Input.GetKeyDown(KeyCode.Delete))
		{
			var controller = controllers[selectedIndex];
			FoveManager.DeleteProfile(controller.ProfileName);
			Destroy(controller.gameObject);
			controllers.RemoveAt(selectedIndex);
		}
		else if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			validateSelectedItem(false);
			++selectedIndex;
		}
		else if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			validateSelectedItem(false);
			--selectedIndex;
		}
		else if (Input.GetKeyDown(KeyCode.F2))
		{
			controllers[selectedIndex].EnterNameEditMode();
		}
		else if (Input.GetKeyDown(KeyCode.Insert))
		{
			var item = GameObject.Instantiate(profileItemPrefab);
			item.transform.SetParent(layout.transform, false);

			var controller = item.GetComponent<ProfileItemController>();
			controller.EnterNameEditMode();
			controllers.Add(controller);

			selectedIndex = controllers.Count - 1;
		}
		else if (Input.GetKeyDown(KeyCode.Return))
		{
			validateSelectedItem(true);
		}

		if (controllers.Count == 0)
			return;

		// else if RENAME
		// else if CREATE
		selectedIndex = (selectedIndex + controllers.Count) % controllers.Count;

		string selectedProfile = controllers[selectedIndex].ProfileName;
		string selectedProfileDataPath = "";
		
		var currProfile = FoveManager.GetCurrentProfile().value;
		if (!string.IsNullOrEmpty(selectedProfile))
			selectedProfileDataPath = FoveManager.GetProfileDataPath(selectedProfile);

		dataPathText.text = "Data Path: \"" + selectedProfileDataPath + "\"";

		for (int i=0; i<controllers.Count; i++)
		{
			var controller = controllers[i];
			controller.isSelected = i == selectedIndex;
			controller.isCurrent = controller.ProfileName == currProfile;
		}
	}

	void validateSelectedItem(bool setCurrent)
	{
		var controller = controllers[selectedIndex];
		controller.ValidateName();
		
		var newProfileName = controller.GetEditValue();

		if (setCurrent && controller.ProfileName == newProfileName) // it is a set
			FoveManager.SetCurrentProfile(newProfileName);
		else if (string.IsNullOrEmpty(controller.ProfileName)) // create case
			FoveManager.CreateProfile(newProfileName);
		else
			FoveManager.RenameProfile(controller.ProfileName, newProfileName);

		controller.ProfileName = newProfileName;
	}
}
