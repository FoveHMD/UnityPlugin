using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Fove.Unity;

public class LoadScene1 : MonoBehaviour {

    // Use this for initialization
    void Start () {
    
    }
    
    // Update is called once per frame
    void Update () {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var sceneName = "Tests/GazeObjectDetection/8b_MultiScenes";
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }
}
