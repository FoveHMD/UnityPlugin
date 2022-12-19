# FOVE Unity Plugin Quickstart Guide

Nobody likes to read a whole document. We get it. We'll try to keep this brief.

## Things to know

* Prefabs are in `FoveUnityPlugin/Prefabs`.
* Either drag a prefab in your scene, or add the `FoveInterface` component to your camera object in the scene.
* If you want the camera NOT to be near (0, 0, 0), the camera needs to be a child object. You can also use the `Fove Rig` prefab.

## Doing Eye Tracking

You have three general ways to get user gaze info. For the last two, you will need a reference in-code to a `FoveInterface`. (You can either grab this at runtime or -- even better -- add a public `FoveInterface` to your custom `MonoBehaviour` and attach the interface via the Inspector view.)

1. To know which object of your application the user is currently gazing at:
   * Add the `GazableObject` component to game objects that you want to be tracked. 
   * Get the currently gazed object using `Result<GameObject> FoveManager.GetGazedObject()`.
2. To detect gaze collision with colliders of the scene,
   *  Use the GazeCast functions of the FoveInterface instance.
   * `bool isLooking = yourFoveInterface.Gazecast(someCollider);`
   * There are a lot of GazeCast variants -- we mostly have parity with Unity's own `RayCast` functions.
3. To get more precise gaze information, you can query gaze rays from a FoveInterface instance:
   * `Result<Ray> gazeRay = yourFoveInterface.GetCombinedGazeRay();`: get the gaze ray in the world space obtained by combining both eye ray information
   * `Result<float> gazeDepth = yourFoveInterface.GetCombinedGazeDepth();`:  get the depth at which the user is looking along the combined gaze ray
   * `Result<Ray> eyeRay = yourFoveInterface.GetGazeRay(eye);`: get the gaze ray of the specified eye
