using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneSync : MonoBehaviour
{
	public SceneField sceneToLoad;

	void Update ()
	{
		if (Input.GetKeyUp(KeyCode.Space))
			SceneManager.LoadScene(sceneToLoad);
	}
}
