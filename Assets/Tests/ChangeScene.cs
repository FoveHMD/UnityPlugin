using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour {

    [SerializeField]
    private string[] sceneName;

	public void SwitchToScene(string name)
	{
        SceneManager.LoadScene(name);
    }

	// Update is called once per frame
	void Update () {

		if (Input.GetKeyDown(KeyCode.Alpha1))
            SwitchToScene(sceneName[0]);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SwitchToScene(sceneName[1]);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SwitchToScene(sceneName[2]);
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            SwitchToScene(sceneName[3]);
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            SwitchToScene(sceneName[4]);
        else if (Input.GetKeyDown(KeyCode.Alpha6))
            SwitchToScene(sceneName[5]);
        else if (Input.GetKeyDown(KeyCode.Alpha7))
            SwitchToScene(sceneName[6]);
        else if (Input.GetKeyDown(KeyCode.Alpha8))
            SwitchToScene(sceneName[7]);
        else if (Input.GetKeyDown(KeyCode.Alpha9))
            SwitchToScene(sceneName[8]);
        else if (Input.GetKeyDown(KeyCode.Alpha0))
            SwitchToScene(sceneName[9]);

        if (Input.GetKeyDown(KeyCode.Alpha1) && Input.GetKey(KeyCode.LeftShift))
            SwitchToScene(sceneName[10]);
        else if (Input.GetKeyDown(KeyCode.Alpha2) && Input.GetKey(KeyCode.LeftShift))
            SwitchToScene(sceneName[11]);
        else if (Input.GetKeyDown(KeyCode.Alpha3) && Input.GetKey(KeyCode.LeftShift))
            SwitchToScene(sceneName[12]);

        if (Input.GetKeyDown(KeyCode.Alpha1) && Input.GetKey(KeyCode.LeftAlt))
            SwitchToScene(sceneName[13]);
        else if (Input.GetKeyDown(KeyCode.Alpha2) && Input.GetKey(KeyCode.LeftAlt))
            SwitchToScene(sceneName[14]);
        else if (Input.GetKeyDown(KeyCode.Alpha3) && Input.GetKey(KeyCode.LeftAlt))
            SwitchToScene(sceneName[15]);
        else if (Input.GetKeyDown(KeyCode.Alpha4) && Input.GetKey(KeyCode.LeftAlt))
            SwitchToScene(sceneName[16]);

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
