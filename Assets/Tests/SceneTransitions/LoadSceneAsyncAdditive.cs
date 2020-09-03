using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneAsyncAdditive : MonoBehaviour
{
    public SceneField sceneToLoad;

    void Update ()
    {
        if (Input.GetKeyUp(KeyCode.Space))
            SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
    }
}
