using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneSync : MonoBehaviour
{
    public SceneField sceneToLoad;
    public LoadSceneMode Mode = LoadSceneMode.Single;

    void Update ()
    {
        if (Input.GetKeyUp(KeyCode.Space))
            SceneManager.LoadScene(sceneToLoad, Mode);
    }
}
