# Fove Unity Plugin

This plugin automates setting up a rendering interface to the Fove Compositor from within Unity via one simple prefab. It also exposes eye-tracking information in a Unity-like way via the Fove interface component.

## Getting Started

1. Install the Fove runtime and make sure it's running.
2. Import the package into Unity.
3. Drag the _Fove Rig_ prefab from the `FoveUnityPlugin/Prefabs/` directory into your scene.
4. Press Play.

That's all you need to do for a simple setup. Of course, this only gets you the most basic setup, and there are many customizations you may want. We think we've figured out the majority of use cases, so check the rest of this guide before deciding you need to do something too extreme.

## What You Get

The Unity package adds a folder called _FoveUnityPlugin_ to your project. Within this folder, there are several items. We will go over them here.

* *Prefabs*: This folder contains a few prefabs for you to use to get started.
  * *Fove Interface*: This is the most basic Fove interface prefab. Use this prefab or attach a the Fove Interface script to an existing camera to be able to use the FOVE headset. [NOTE: The Fove Interface changes the position and orientation of the containing game object at runtime to match the pose of FOVE headset in the real world. If you want to modify the position of the camera in your game use a *Fove Rig* instead!]
  * *Fove Rig*: This is a Fove Interface incorporated into an additional root game object. Use this when you want to move the camera around in your game.
* *x86_64*: This folder contains required native libraries for FOVE to run. It's a standard name to indicate that these libraries are for 64-bit x86 machines.
  * *FoveClient.dll*: The magic library. Well, it's the FOVE library, anyway. This is just the publicly released client library that you can get off our website. We upgrade this with every release of the Unity plugin, as there may be new features which are specific to new client versions, so it's best to leave this guy alone. If you keep up-to-date with the Unity plugin, you will also stay caught up with the FoveClient library.
  * *FoveUnityFuncs.dll*: Contains a handful of extra functions that bind to Unity's native plugin interface for synchronizing frame submission and such with Unity's graphics thread.
* *FoveClient_CLR.dll*: This is the official managed C# bindings DLL. It essentially builds up a couple of C# classes which call into the C API bindings we expose in *FoveClient.dll*, and presents you the data and structs as close to raw as we can while still keeping it looking and feeling like healthy C#. If you must, you could use this directly, but you'll probably end up rewriting a lot of rather tedious code that we already wrote for you if you.
* *Source*: We are releasing the source for our plugin so that you can see what's going on, make changes if you don't like something, and suggest improvements if you feel so inclined. All our source for the plugin is contained beneath this folder.
  * *Behaviours*:
    * *FoveInterface*: This what you put onto camera objects in the scene. It has methods for rendering each eye's view correctly and provides you with gaze information.
    * *FoveManager*: This is a singleton behaviour which is automatically created whenever you try to get data from the FOVE SDK. Global settings and information can be accessed from this static class.
    * *GazableObject*: Coupled with a Unity collider, this component registers the object it is attached to into the Fove system to perform automatic gaze object detection.
    * *GazeRecorder*: Save all the gaze data to a disk file. Gaze data can either be sampled at 70fps or 120fps. This script is convenient for researchers who want to record patient gaze data for further offline analysis.
  * *Classes*: Several data structure classes used by the plugin. See the comments on the class itself for further information
  * *Editor*: This directory stores editor scripts used to show custom inspectors and the FOVE Settings window.
    * *FoveInterfaceEditor*: There are some important editable properties for `FoveInterface` (principally, setting the camera prefab) which this script adds to the base editor when a `FoveInterface` is selected.
    * *FoveSettingsEditor*: Contains all the core code for the "FOVE Settings" editor window.
    * *ProjectChecks*: A container for all of the performance checks and project recommendations done for the "FOVE Settings" window. We will continue to improve these checks, adding new ones and adjusting them as necessary with each plugin release so that you can be confident your projects will perform optimally.
  * *ScriptableObjects*: Contains code for any `ScriptableObject` classes used by our plugin.
    * *FoveSettings*: The container class for our per-project settings. Right now it does very little, but the plan is to expand it to make your life easier. We automatically create one instance of this which goes into the Resources folder and is the file referenced by the rest of the plugin.
  * *Utility*: Contains scripts with extra functionality -- mostly conversion functions between FOVE types and Unity types.

---

## The Fove classes in details

### The FOVE Interface component

This is the most important component of the plugin:
- It renders each eye and sends the images data to the headset, and, for this reason should always come along with a Unity camera. 
- It updates the camera local transformation to match the Fove headset pose (orientation and position)
- It converts raw gaze information into world space gaze data and provide convenience ray cast methods.

#### Properties and settings

The `FoveInterface` is quite configurable. Properties can either be set from the unity editor inspector or at runtime directly from the script. Names are not exactly the same in both cases but pretty similar. We will use the editor inspector names here but you should be able to easily find equivalent property on the component.

We tried to come up with helpful tooltips for each of the Inspector panel options, but they are admittedly a bit clunky, and you probably don't want multiple paragraphs of tooltip appearing for each item, so here's a bit more detail about each item in the Inspector for our interface component.

#### Client fetches and sync:

The next three checkboxes are essentially hint flags which indicate what pieces you intend to use. When you uncheck a box, the Fove interface stops fetching and updating the corresponding data. Typically you would use all three (gaze, orientation, and position), and so the default is for all three to be selected.

As an example for a case where you may not want to have all the attributes checked, if you have a constant in-place HUD overlay that you don't want to move as the player moves their head around, you may want to un-check all three. Or if you want to render a sky box, you wouldn't want to have position for sure, and you likely wouldn't want gaze on that interface, but I could see some fun effects having a skybox that responds to gaze...

#### Eye Targets

When set to `Left` or `Right` the attached camera renders only to the corresponding HMD screen, otherwise the it renders to both with proper eye position offsets.

#### Pose Type

Starting with version 0.15.0 of the Fove SDK, we have added two different pose settings: sitting and standing. 

Sitting is the same as in previous versions, and will tend to keep the user's view close to where the camera starts out.

Standing is the new mode, and it takes into account an offset of the position-tracking camera from the floor. In theory, using the standing mode, if the user has this value configured well, they should end up in similar places when using either Fove or SteamVR.

Assuming the setting has been configured properly, "Standing" mode offers a more accurate transfer of the user's view into VR to what their brains expect to see in terms of position off the floor and movement. However for simulations where the user is... well, sitting, we recommend using the "Sitting" mode which will allow you to position them more precisely in the world.
 
#### Gaze Object Detection

Properties related gaze-based scene object detection.

##### Camera Registration

If enabled, a camera object is automatically registered into the FOVE system for this `FoveInterface`.
Automatic gazed object detection can only be performed if at least one point of view (camera) is provided (e.g. registered into the system).

##### Cull Mask

Setting the gaze cast cull mask allow you to ignore certain layer of objects when performing automatic gazed object detection.

#### Per-Eye culling mask

Allow you to remove specific layer of object when rendering the specified eye image. Note that those per-eye culling masks are applied on top of the camera default culling mask. Modifying the eye culling mask of an object layer that is not initially rendered by the camera won't have any effect.

#### Compositor Options

There are a few possible options which may be useful to you once you start getting more involved in developing your game with regard to how each camera communicates with the compositor.

##### Disable Timewarp

A little background: In an ideal, real-time, deterministic operating system, one could theoretically guarantee that all frames reach the HMD before the HMD screen needs to refresh. These systems are very uncommon (and not generally considered useful for broad user consumption these days), and so even if your game runs quickly there's a good chance you'll end up with frame skipping every now and then. Time warp exists to catch those little skips and prevent them from becoming apparent to your users.

But if you are rendering an overlay that's attached to the camera, time warp during those little skips can actually make your supposedly-steady HUD appear to jump around rather a lot! For these instances, we have the *Disable Timewarp* toggle. If you're making a layer that's intended to stay in one place relative to the user's vision, definitely turn this on.

##### Disable Distortion 

This feature is not supported anymore and has been hidden from the GUI.

---

### The FOVE Settings

You can open the FOVE settings window by selecting the "FOVE" menu and selecting "Edit Settings..." There is a panel on the right which displays details about most options if you put your mouse cursor over them.

The main view displays any project settings suggestions that we've detected may improve your experience when using the FOVE plugin. You can press "Fix" next to any suggestions to automatically apply the change, or you can click "Fix All" to apply all recommended changes at once.

The "Settings" tab (near the top of the window) shows any global settings. The following settings are available:

* *Automatic Gaze Object Registration*: If checked, any game object having a collider will be automatically registered as gazable object as the beginning of the game. This allow you to query game object that the user is currently gazing at using the `FoveManager.GetGazedObject`. Read [Identify which object the user is looking at](#identify-which-object-the-user-is-looking-at) for more details.
* *Force Calibration*: Default off. If you check this, then every time you start play, it will force the FOVE runtime to recalibrate. Typically you would leave this off, however in some cases (such as trade show demos) it may be useful to have this.
* *Custom Desktop View*: Allow you to render a different view on your desktop display. See above [Display a different view on PC display](#display-a-different-view-on-pc-display) section for more details.
* *Gaze Cast Policy*: Specify how the different gaze cast functions should behave when the user is closing one or two eye. The default behavior is to dismiss collision detection when the user is closing both eye. Note that `Never dismiss` can be particularly useful during debug in order to be able to trigger collision without having to wear the headset.
* *World Scale*: Default 1. This is the number of Unity units which represent 1 meter in your game/simulation. Unity's physics engine assumes 1 as well, so if your assets are different and you use Unity's physics in any way, make sure you adjust the gravity constant for your scale. Changing this value is important because we have to adjust eye separation and HMD offset so that the world "feels" right inside VR.
* *Render Scale*: Allow you to increase or reduce the size of the internal render texture. This is useful to increase oversampling on scenes which can afford it. For more details, see the [Oversampling Ratio](#oversampling-ratio) section below.

---

### The Fove Manager class

This is a singleton class automatically created when starting your game. The singleton instance doesn't normally need to be accessed directly though. All usefully properly and function are static and can be accessed from `FoveManager` class directly. 

It allows you to modify the Fove settings at runtime, to retrieve any gaze information that is independent from cameras and control the eye & position tracking.
For example ,from the `FoveManager`, you can:
* Start/Stop Calibration
* Get Calibration status
* Trigger HMD adjustment
* Tare the Headset orientation/position
* Get EyeClosed status
* Get the Gazed object
* Get Raw gaze vectors
* Get Raw pose information
* Get use IPD/IOD
* Get eyes images
* Get Software versions
* etc.
---

## HOW-TOs

### Get eye gaze information (origin, direction, depth)

Gaze information in the world coordinate system can be retrieved from the `FoveInterface` class using the follow functions:
- `GetGazeRay(eye)`: returns the individual eye gaze ray (origin + direction)
- `GetCombinedGazeRay`: returns a more reliable gaze ray (origin + direction) that was obtained by combining the left and right eye rays
- `GetCombinedGazeDepth`: returns an approximation of the distance at which the user is looking by combining the left and right eye rays

Gaze information in the Headset (HMD) coordination system can be retrieved from the `FoveManager` class using the following static functions:
- `GetHmdGazeVector(eye)`: returns the individual eye gaze direction
- `GetHmdGazeVectorRaw(eye)`: returns the individual eye raw gaze direction (e.g. without any smoothing or other post-processing).
- `GetHmdCombinedGazeRay`: returns a more reliable gaze ray (origin + direction) that was obtained by combining the left and right eye rays
- `GetCombinedGazeDepth`: returns an approximation of the distance at which the user is looking by combining the left and right eye rays
- `GetGazeScreenPosition`: returns the position on the screen where the user is looking at (normalized in [-1, 1])

---

### Know the Object gazed by the user

There are mostly two ways to detect which object the user is looking at: a higher level API and a lower level API.
We recommend developers to use the higher level API as much as possible rather than performing manual ray casting as it is more robust and will be continuously improved.

#### Automatic gazed object detection

From version 3.2 of the plugin, we added the `GetGazedObject` function on the `FoveManager` class. This function returns the Unity game object that the user is currently looking at.

For this function to work properly, you need to add a `GazableObject` component as well as an Unity collider (currently we support only Sphere, Cube and Mesh colliders) on objects that we want to be gazable. The `GazableObject` script registers and updates the Unity game object into the FOVE system. Nevertheless, as Unity do not provide callbacks for object state changes, manual action is needed when you change the collider at runtime. In this case, call `GazableObject.RebuildShape` to update the shape of the game object registered into the FOVE system.

The `Velocity Source` parameter of the `GazableObject` specify where and how the calculation of object velocity should be performed:
- `Late Update`: this is the most common case. Use this if you modify the position of your object in the Unity `Update` function.
- `Fixed Update`: Use this if you modify the position of your object in the Unity `FixedUpdate` function.
- `Rigidbody`: Use this if the position of your object is coming from a rigidbody.

To disable the detection of specific objects you have several options:
- Using the `FoveInterface.GazeCastCullMask` you can disable detection of all objects of a given layer.
- By removing or disabling the collider of an object inside the editor before execution, you can permanently disable the detection for this specific object
- By removing or disabling the `GazableObject` component of the game object you can disable the detection at runtime

If you don't want to manually create and attach the `GazableObject` component to all objects of your scene, we also provide a few alternative ways to create them automatically from existing colliders:
- Fove settings: `Automatic Gaze Object Registration` automatically creates `GazableObject` components for all the collider objects of your scene at the beginning of the game. This option is here for convenience, but we do not recommend to use it in big-scale game but rather to manually register only objects of interest.
- To create them from colliders of a scene newly loaded, you can call the `GazableObject.CreateFromSceneColliders` function.
- To create them from colliders of a newly instantiated prefab or existing object hierarchy, you can call the `GazableObject.CreateFromColliders` function.

#### Manual gaze ray casting

If you don't want to use the Gaze Object Detection feature and prefer to perform collider gaze ray casting manually, it is also possible.
There are a number of convenience methods built out for you which determine if the user is looking at a given collider, or a collection of colliders. These methods are designed to be similar to many of the `Raycast` methods provided by Unity, just based on the user's gaze. As such, they're called `Gazecast`. You can find them on the `FoveInterface` class, check them out. They're pretty efficient and easy to use.
The `GazeCastPolicy` property of the Fove settings allows you to choose to ignore or not gaze cast collisions when the user is closing one or two eyes.

If you want to have even more control on what you are doing, you can also grab the combined gaze ray from the method `GetCombinedGazeRay()` of your `FoveInterface` instance and perform ray-casting/gazed object detection by yourself.

---

### Move the camera around in the world

The FOVE tracks your position and orientation in the real world and the `FoveInterface` script automatically synchronizes its game object pose in accordance. What this means for you, dear dev, is that your `GameObject` is going to be spending a lot of time near (but generally not quite at) its origin.

If you don't want your view to always snap back to nearly-world-origin when you hit "Play", then you have to put it underneath (as a child of) another object and modify the position of that one. (I like to call this something like "FOVE Body" or "FOVE Root" in my projects.) You can now place and rotate (and even scale, if that's your "thing") this root object and the FOVE interface's GameObject will always move relative to its parent object. If you have created the `FoveInterface` object of your scene by dragging in the `Fove Rig` the you already have this root object. Just move and rotate the `Fove Rig` root object instead of the `Fove Interface` object.

Another options is to disable the `Fetch and sync: Orientation + Position` options from the `Fove Interface`. In this case the headset pose won't be replicated at all on your `Fove Interface` and you can move it freely as you want. This could be an easier way to do for example if you are developing a 2D application.

Just keep in mind that (in case you're going to make an FPS and move the root transform around) stairs are considered bad in VR. Use elevators, or very gradual ramps if you must.

---

### Access the Fove Interface from another script

Your means to interact with eye tracking is through the member methods on `FoveInterface`. So if you have an object which responds to eye gaze, you need to have access to a reference of `FoveInterface` to get that. We highly recommend using Unity's functionality of exposing fields via the Inspector and requiring a `FoveInterface` that way -- but for runtime spawning of prefabs, it's perfectly acceptable to grab a reference using a method similar to the one shown below. (Note, the example below uses a static reference to the `FoveInterface` because `FindObjectOfType` is pretty expensive. You should determine if a static singleton pattern works best for your situation. Please program responsibly.)

```csharp
using UnityEngine;

public RespondToEyeTracking : MonoBehaviour {
  public GazeReactiveBehaviour thePrefab;

  private static FoveInterface _theFoveInterface;

  void Awake() {
    if (_theFoveInterface == null) {
      _theFoveInterface = FindObjectOfType(typeof(FoveInterface)) as FoveInterface;
      if (_theFoveInterface == null) {
        Debug.LogError("No FoveInterfaceBase object found in the scene! :,(");
        return;
      }
    }

    //... do other setup stuff here
  }

  void SpawnThePrefab() {
    var theInstance = Instantiate(thePrefab) as GazeReactiveBehaviour; // pick whichever version of Instantiate works here
    theInstance.SetFoveInterface(_theInterface);
  }
}
```

Just keep in mind that searching the whole scene graph can be expensive and if you have prefabs that are spawning very frequently, it could slow down your scene significantly to have each of them search for the `FoveInterface` in use whenever they're created.

---

### Add UI screens

To add over-layed UI in your game the easiest way to do is now to:
- Add an additional `Fove Interface` prefab to your scene
- Set the camera clear flags to depth only (only if you want to clear the depth)
- Increase the camera depth to be sure it renders after your scene
- Adjust the camera culling masks to draw only scene objects in one and UI objects in the other
- Add your UI objects under the UI camera or disable automatic camera pose adjustment on the UI FoveInterface.

Lastly, if you disable orientation and position on the FoveInterface for your UI, you should definitely check "Disable Timewarp" in the Compositor options foldout, otherwise you will almost certainly get uncomfortable judder as your stationary layer gets timewarped unnecessarily.

---

### Add post-effects

We support using full screen image effects in all supported versions of Unity. Under most use cases, everything should appear in each eye exactly how it appears in the Unity Editor.
To add a image effect, just proceed as usual by adding a component overriding the `OnRenderImage` method on the camera game object.

The Unity post-processing stacks on the other side are not working out of the box. Some shader of the 2.0 resets the camera projection matrix during the `OnPreCull` method every frame, which removes the custom projection we set via the FOVE plugin. It seems that this reset is only necessary if you use their TAA effect. We've been able to use the source version of the plugin and comment out the line that resets the camera's projection matrix without any issues. The line is in the file `PostProcessLayer.cs`. We wish you luck, and hope that this requirement got fixed by the time you are reading this.

---

### Get various headset and eye information

All headset information (such as the HMD pose, projection matrix, eye offset, etc) and camera independent eye tracking information (such as eye closed status, calibration status, etc.) can be accessed from the `FoveManager`.
Most of the Fove C API functions have their equivalent in this class. For a full list of the function and property available check the class source code directly.

---

## Advanced topics

### Use OpenVR and SteamVR compositor to render your game instead of default Fove services

From version 3.1.2, it is possible to use OpenVR to render your game from the Unity plugin.

To do so, proceed as following:
1. Install SteamVr and Fove SteamVR plugin
2. Disable the auto-start of the Fove compositor from the fove tray 
3. Enable VR support in the Unity player settings of your project. Add the `OpenVR` sdk in the list of supported VR sdks.
4. Add a `Fove Interface` prefab to your game (if not already done)
5. Start your game normally

For more information about (1) and (2), check detailed instructions [here](https://support.getfove.com/hc/en-us/articles/115001954713-Getting-started-with-Steam-FOVE)

Note that the `Fove Interface` prefab is still needed to update your camera pose and get eye gaze data information as OpenVR does not provide any eye gaze API.

Also note that the following properties have no effect when using the OpenVR api:
- FoveSettings:
  - World Scale
  - Render Scale
  - Custom Desktop View
- FoveInterface:
  - Eye Targets
  - Per-Eye Culling Masks
  - Compositor options

---

### Display different content to each eye

Use can use the Unity object layer system and `FoveInterface`'s per eye culling mask to easily render different content to each eye:
* From the Unity project settings/Tags & layer page inspector, create two new layers: 1 specific for right eye objects (e.g. `RightEyeObjects`) and 1 specific for left eye objects (e.g. `LeftEyeObjects`)
* Go to your fove interface(s) inspector. Under `Per-eye culling mask`, select `LeftEyeObjects` for the right eye and `RightEyeObjects` for the left eye
* Go to all the objects that should be rendered only on one eye and set the object layer accordingly (settable at top of the inspector)
* Let all objects that should be rendered on both eyes in the default layer (or any other not eye specific layer)

---

### Display a different view on the PC display

By default the Fove plug-in optimizes the rendering and copies the headset view as-is onto the desktop display. If you want to display a different view (such a config menu, etc.) on your PC monitor, you can disable this optimization from the Fove settings. Open the Resources/FOVE Settings file in Unity editor and check the `Custom Desktop View` option. After enabling this option, every enabled cameras will render to your desktop view while only the cameras (enabled or not) with a `FoveInterface` component will render to the headset. So: 
- To render objects only on the headset, disable the camera component rendering these objects. 
- To render objects only on the Desktop display, add the object layers to the `Per-Eye Culling Masks` of the `FoveInterface` or disable/remove the `FoveInterface` component of the camera.
- To display some objects on both and some only on one, split your objects into several layers and adjust the `FoveInterface` and camera Culling Masks accordingly.

Note that enabling this option forces the Fove plugin to perform an extra rendering of your scene. This lowers performance and should be enabled only when needed.

---

### Oversampling Ratio

Oversampling ratio determines the texel-to-pixel ratio when rendering to FOVE. This is important because the various shaders involved in adapting images to the headset have a tendency to expand pixels near the center of the image, make a 1-to-1 ratio looking kind of blocky and pixellated. We've picked a reasonable tradeoff between getting good performance and looking nice. But if your game requires more performance you can potentially decrease the oversampling ratio and get back a bit of perf at the cost of visual quality. We currently cap this between 0.01 (which looks just so awful) and 2.5 (which is probably overkill). If you need something else, let us know -- I'm very curious to hear your use case!

Note: that the FoveCompositor as its own oversampling factor (default value: 1.4f) that is system wide and applies on top of this one.

---

### HDR

The FOVE native client does not support HDR images. If you use HDR on your Unity cameras without adding a tonemapping effect, you may get a dull, grayed-out looking scene in your headset.

---

### "Asynchronous" data access

The plugin automatically handles acquiring pose and eye tracking data for you in sync with the rendering pipeline, however Unity's FixedUpdate function can be called more frequently than and semi-out-of-sync with the standard Update function. (Default looks like 50 times per second for physics -- you can change this from `Edit > Project Settings > Time`.) For more information on execution of Unity events, see this document: https://docs.unity3d.com/Manual/ExecutionOrder.html (`FixedUpdate` is not ACTUALLY executed out of sync, so if that's what you need, you should probably come up with something else using threads and mutexes.)

If you want to get the immediate, most up-to-date, current information from the headset, you can call the `FoveManager.UpdateHeadsetData()` function at anytime. It will refetch and update all data from the headset. Note that if you need this out-of-sync data only in one specific thread and don't want to mess with the in-sync data that the FoveManager provides, you can also create your own instance of the lower level `FoveHeadset` class, manually register capabilities, fetch eye-tracking/position data, and get the required data.

---

### Eyes or Position images

The eyes image is the image of the user eyes as recorded by headset IR camera. It is used by the FOVE system for eye tracking but can also be queried using the FOVE `FoveManager.GetEyesImage()` API for any other purposes.

The position image is the IR image used by the FOVE system to perform position tracking. It can also be retrieved from the FOVE system using the `FoveManager.GetPositionImage()` API.

### Add-ins

The `FoveManager` system has the ability to register add-ins to get called back just after Headset data updates. This is intended for systems that need to access lower-level functionality of the headset directly and that always kept in sync with the `FoveManager` headset data. To use this just your own add-in action to be called back from the `FoveManager.AddInUpdate` property.

---

## Known Limitations

There are some limitations with the current system which we may be fixing in the near future. (If you have any requests, please contact us!)

* The lowest Unity Editor version supported is 2017.4+.
* Camera orthogonal projections are currently not supported. Perspective projection is always used at runtime to match headset view parameters. Keep this in mind when you build your game UI.
* Using "skybox" camera clear mode and set the scene skybox to null (scene lighting settings) may result in a white screen inside the headset.
* Unity Editor update overhead seems to have increased in recent versions of Unity (2017+). This sometimes result in missed frames and can cause the scenes to stutter when you are playing the scene in-editor. However, these display issues should vanish when you build and launch the real project player.
* Gaze vector can be null (0,0,0) if the calibration has been not properly run.
* SteamVR maintains its own HMD offset values which get applied to the camera in your scene. These are almost certainly different from the position as reported by FOVE. If you want your project to be compatible with both, you can achieve this by using "Standing" mode in the `FoveInterface` inspector.
* In addition, because SteamVR/OpenVR handle the position of the interface object on their own, the world scale values won't work when running in SteamVR mode. We recommend keeping your world scale at 1 for highest compatibility with other VR systems.
* Unity post process stack is not supported as it internally modifies the camera projection matrix.
