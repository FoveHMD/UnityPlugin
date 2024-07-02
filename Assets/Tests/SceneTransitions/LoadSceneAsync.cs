using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneAsync : MonoBehaviour
{
    public SceneField sceneToLoad;
    public LoadSceneMode Mode = LoadSceneMode.Single;

    void Update ()
    {
        if (Input.GetKeyUp(KeyCode.Space))
            SceneManager.LoadSceneAsync(sceneToLoad, Mode);
    }
}
