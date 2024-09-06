using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour {

    [SerializeField]
    private string[] sceneName;

	public void SwtichToScene(string name)
	{
        SceneManager.LoadScene(name);
    }

	// Update is called once per frame
	void Update () {

		if (Input.GetKeyDown(KeyCode.Alpha1))
            SwtichToScene(sceneName[0]);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SwtichToScene(sceneName[1]);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SwtichToScene(sceneName[2]);
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            SwtichToScene(sceneName[3]);
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            SwtichToScene(sceneName[4]);
        else if (Input.GetKeyDown(KeyCode.Alpha6))
            SwtichToScene(sceneName[5]);
        else if (Input.GetKeyDown(KeyCode.Alpha7))
            SwtichToScene(sceneName[6]);
        else if (Input.GetKeyDown(KeyCode.Alpha8))
            SwtichToScene(sceneName[7]);
        else if (Input.GetKeyDown(KeyCode.Alpha9))
            SwtichToScene(sceneName[8]);
        else if (Input.GetKeyDown(KeyCode.Alpha0))
            SwtichToScene(sceneName[9]);

        if (Input.GetKeyDown(KeyCode.Alpha1) && Input.GetKey(KeyCode.LeftShift))
            SwtichToScene(sceneName[10]);
        else if (Input.GetKeyDown(KeyCode.Alpha2) && Input.GetKey(KeyCode.LeftShift))
            SwtichToScene(sceneName[11]);
        else if (Input.GetKeyDown(KeyCode.Alpha3) && Input.GetKey(KeyCode.LeftShift))
            SwtichToScene(sceneName[12]);

        if (Input.GetKeyDown(KeyCode.Alpha1) && Input.GetKey(KeyCode.LeftAlt))
            SwtichToScene(sceneName[13]);
        else if (Input.GetKeyDown(KeyCode.Alpha2) && Input.GetKey(KeyCode.LeftAlt))
            SwtichToScene(sceneName[14]);
        else if (Input.GetKeyDown(KeyCode.Alpha3) && Input.GetKey(KeyCode.LeftAlt))
            SwtichToScene(sceneName[15]);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
