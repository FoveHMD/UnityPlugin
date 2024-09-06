using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeStage : MonoBehaviour {

	public string[] sceneName;

	private int currentIndex = 0;

	void Start()
	{
		DontDestroyOnLoad(this.gameObject);
	}

	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown(KeyCode.Space))
		{
			currentIndex++;
			if(currentIndex >= sceneName.Length)
			{
				currentIndex = 0;
			}
			SceneManager.LoadScene(sceneName[currentIndex]);
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Destroy(this.gameObject);
		}
	}
}
