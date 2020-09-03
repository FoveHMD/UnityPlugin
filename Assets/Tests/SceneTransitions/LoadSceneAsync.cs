using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneAsync : MonoBehaviour
{
    public SceneField sceneToLoad;

    void Update ()
    {
        if (Input.GetKeyUp(KeyCode.Space))
            SceneManager.LoadSceneAsync(sceneToLoad);
    }
}
