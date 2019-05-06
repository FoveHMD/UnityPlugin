# FOVE Unity Plugin Quickstart Guide

Nobody likes to read a whole document. We get it. We'll try to keep this brief.

## Things to know

* Prefabs are in `FoveUnityPlugin/Prefabs`.
* Either drag a prefab in, or add `FoveInterface` to your camera object in the scene.
* If you want the camera to NOT be near (0, 0, 0), the camera needs to be a child object. You can also use the `Fove Rig` prefab.

## Doing Eye Tracking

You have two general ways to get gaze info. For both of these, you will need a reference in-code for a `FoveInterface`. (You can either grab this at runtime or -- even better -- add a public `FoveInterface` to your custom `MonoBehaviour` and attach the interface via the Inspector view.)

1. Detect gaze collision with colliders using GazeCast functions on each FoveInterface instance.
   * `bool isLooking = yourFoveInterface.Gazecast(someCollider);`
   * There are a lot of GazeCast variants -- we mostly have parity with Unity's own `RayCast` functions.
2. Get gaze convergence from a FoveInterface instance.
   * `GazeConvergenceData gaze = yourFoveInterface.GetGazeConvergence();`
   * The result has a Unity `Ray` object and a `float` value.
     * `ray` indicates the world point between both eyes and the direction of your gaze.
     * `distance` indicates how far along the ray the eyes converge.
     * Example: `Vector3 convergence = gaze.ray * ray.distance;`
