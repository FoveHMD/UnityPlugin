using UnityEngine;
using UnityEngine.SceneManagement;
using Fove.Unity;

public class CreateGazableObjects : MonoBehaviour {

    // Use this for initialization
    void Start () {
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
            GazableObject.CreateFromColliders(root);
    }
}
