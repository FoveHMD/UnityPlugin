# Fove Unity Plugin

This plugin automates setting up a rendering interface to the Fove Compositor from within Unity via one simple prefab. It also exposes eye-tracking information in a Unity-native way via the Fove interface component.

## Getting Started

1. Install the Fove runtime and make sure it's running.
2. Import the package into Unity.
3. Drag either the _Fove Rig_ prefab from the `FoveUnityPlugin/Prefabs/` directory into your scene.
4. Press Play.

That's all you need to do for a simple setup. Of course, this only gets you the most basic setup, and there are many customizations you may want. We think we've figured out the majority of use cases, so check the rest of this guide before deciding you need to do something too extreme.

## What You Get

The Unity package adds a folder called _FoveUnityPlugin_ to your project. Within this folder, there are several items. We will go over them here.

* *Prefabs*: This folder contains a few prefabs for you to use to get started.
  * *Fove Interface*: This is the most basic fove interface prefab. You need to have this prefab instanced at least once in your game or attach a Fove Interface script to an existing camera to be able to use the FOVE headset. [NOTE: The Fove Interface changes the position and orientation of the containing game object at runtime to match the pose of FOVE headset in the real world. If you want to modify the position of the camera in your game use a *Fove Rig* instead!]
  * *Fove Rig*: This is a Fove Interface incorporated into an additional root game object. Use this when you want to move the camera around in your game.
* *x86_64*: This folder contains required native libraries for FOVE to run. It's a standard name to indicate that these libraries are for 64-bit x86 machines.
  * *FoveClient.dll*: The magic library. Well, it's the FOVE library, anyway. This is just the publicly released client library that you can get off our website. We upgrade this with every release of the Unity plugin, as there may be new features which are specific to new client versions, so it's best to leave this guy alone. If you keep up-to-date with the Unity plugin, you will also stay caught up with the FoveClient library.
  * *FoveUnityFuncs.dll*: Contains a handful of extra functions that bind to Unity's native plugin interface for synchronizing frame submission and such with Unity's graphics thread.
* *FoveClient_CLR.dll*: This is the official managed C# bindings DLL. It essentially builds up a couple of C# classes which call into the C API bindings we expose in *FoveClient.dll*, and presents you the data and structs as close to raw as we can while still keeping it looking and feeling like healthy C#. If you must, you could use this directly, but you'll probably end up rewriting a lot of rather tedious code that we already wrote for you if you.
* *Source*: We are releasing the source for our plugin so that you can see what's going on, make changes if you don't like something, and suggest improvements if you feel so inclined. All our source for the plugin is contained beneath this folder.
  * *Behaviours*:
    * *FoveInterface*: This what you put onto camera objects in the scene. It has methods for rendering each eye's view correctly.
    * *FoveManager*: This is a singleton behaviour which is automatically created whenever you try to get data from the FOVE SDK. You should access information from this class using the `FoveManager.Instance` object.
  * *Editor*: This directory stores editor scripts used to show custom inspectors and the FOVE Settings window.
    * *FoveInterfaceEditor*: There are some important editable properties for `FoveInterface` (principally, setting the camera prefab) which this script adds to the base editor when a `FoveInterface` is selected.
    * *FoveSettingsEditor*: Contains all the core code for the "FOVE Settings" editor window.
    * *ProjectChecks*: A container for all of the performance checks and project recommendations done for the "FOVE Settings" window. We will continue to improve these checks, adding new ones and adjusting them as necessary with each plugin release so that you can be confident your projects will perform optimally.
  * *ScriptableObjects*: Contains code for any `ScriptableObject` classes used by our plugin.
    * *FoveSettings*: The container class for our per-project settings. Right now it does very little, but the plan is to expand it to make your life easier. We automatically create one instance of this which goes into the Resources folder and is the file referenced by the rest of the plugin.
  * *Utility*: Contains scripts with extra functionality -- mostly conversion functions between FOVE types and Unity types.

---

## Upgrading to 3.1.0

A few public functions have been renamed so you may have to adjust your code (see the [changelog](./Changelog.md) for more details).

Also as the serialized name of the Fove interface's `Client uses: gaze/orientation/position` and `Pose type` properties have changed, you will need to set those settings back from the Unity inspector.

## Upgrading to 3.0.0

Always back up your project before doing a major-version upgrade like this!

A lot has changed in this version, and we decided not to preserve perfect backwards compatibility. All of the core features still exist, in separating HMD-relative methods from Unity-relative methods some things were moved around. 

We also removed:
- the notion of layer (base, overlay, diagnostic) inside the headset. Now all rendering is always performed on the base layer (see below for more details).
- the deprecated static `FoveInterface` methods from this version, so if you were using that, then you may have several places which you will need to fix things up.
- the manual drift correction functions as they are not required anymore with the new version of the runtime.

If you don't have much time and an older interface is working for you, feel free to keep using it, however there are some notable quality of life improvements in this version. What follows are some things to check for when upgrading from an older version of the plugin.

### Importing the New Plugin

It's generally safe to import the new plugin atop the old one, however, because we've simplified a lot of the interface and layout, you will end up with extra (no longer supported) files in your plugin directory, and if you use any of the `Fove*2` behaviours or prefabs, you will need to update those yourself. So you have a few options for the initial import.

If you only used the default `FoveInterface` or `FoveRig` prefabs (not the `Fove*2` variants), then we've had pretty good luck with (after backing up your project). Simply delete the "FoveUnityPlugin" and "Test Scripts" folders and re-import the new plugin from the provided Unity package file. Initially, you will break your prefab connections, but when you re-import the plugin it should re-import all the files with the correct GUIDs and reattach them.

However, because we removed the `FoveInterface2` and `FoveRig2`, if you used either of these you will want to still remove the `Behaviours` directory, but leave your prefabs and just import the new plugin on top of your existing directory. This will break your prefabs, which you should select and update them to the FoveInterface behaviour by-hand. We also recomment removing the FoveCamera and FoveInterfaceBase behaviours.

Finally, if any of your other behaviours store a reference to one of the removed behaviours (e.g., `FoveInterfaceBase` or `FoveInterface2`), you will have to update those scripts (along with some other changes -- see below) to require a `FoveInterface`, and you will need to update the missing connections in any scenes as well.

### Gazecast Methods

The Gazecast family of methods has been formalized as instance methods on actual `FoveInterface` objects and cannot be accessed statically (see below). We do, however, offer more ways to use Gazecast, which generally mirrors 1-to-1 the built-in `Raycast` method family from Unity. So it may be worth looking at your `Gazecast` calls to make sure you're using the one(s) with the best settings for each case.

### Draw Layer and UI

In this new version we remove to possibility to choose the draw layer on which the camera renders. All the rendering is now always performed on the base layer.

To add overlayed UI in your game the easiest way to do is now to:
- Add an additional Fove Interface prefab to your scene
- Set the camera clear flags to depth only (only if you want to clear the depth)
- Increase the camera depth to be sure it renders after your scene
- Adjust the camera culling masks to draw only scene ojects in one and UI objects in the other
- Add your UI objects under the UI camera or disable automatic camera pose adjustment on the UI  FoveInterface.

Lastly, if you disable orientation and position on the FoveInterface for your UI, you should definitely check "Disable Timewarp" in the Compositor options foldout, otherwise you will almost certainly get uncomfortable judder as your stationary layer gets timewarped unnecessarily.

### Display a different view on PC display

By default the Fove plug-in optimizes the rendering and copies the HMD view as-is onto the desktop display. If you want to have some configuration settings or show diffrent view of your scene on your PC monitor, you can disable this optimization from the Fove settings. Open the Resources/FOVE Settings file in Unity editor and check the `Custom Desktop View` option. After enabling this option, enabled cameras will render to your desktop view while only the cameras (enabled or not) with a `FoveInterface` component will render to the HMD. So: 
- To render objects only on the HMD, disable the camera component rendering those objects. 
- To render objects only on the Desktop display, add those object layers to the `Per-Eye Culling Masks` of the fove interface or disabled (or remove) the `FoveInterface` component of the camera.
- To display some objects on both and other only on one, split your objects into several layers and adjust the fove interface and camera Culling Masks.

Note that enabling this option forces the Fove plugin to perform an extra rendering of your scene. This lowers performance and should be enabled only when needed.

### Obsolete Static `FoveInterface` Methods

Until now, we maintained a static reference of the first `FoveInterface` object registered in each scene, and that object was used for all the static method calls (which have been marked obsolete since version 1.2.0). If you've been using these static methods you will need to acquire your own reference now. We recomment either:

1. Create a public `FoveInterface` member on your custom `MonoBehaviour` class and assign the correct object to it using Unity's built-in inspector bindings.
2. On your `MonoBehaviour`'s `Start` event, find the `FoveInterface` you want using Unity's built-in search for objects (either by type or name, depending on your needs), and then cache that reference as a local member variable so you don't have to search for it every frame. You definitely won't want to be searching your scene hierarchy every frame each time for the `FoveInterface` as such searches are fairly costly for performance. You shouldn't worry about caching the value unless the cache would outlive the `FoveInterface` (e.g. on scene change).

### Camera Effects and Camera Prefabs

The new `FoveInterface` uses the camera object it is attached to directly rather than messing around with separate camera prefabs or anything. This means that if you had special prefabs for your eye cameras previously, you will have to make sure that all the relevant screen effects are attached to the actual FoveInterface. The easiest way to do this may be to copy the behaviours for each effect from the prefab to the `FoveInterface` camera, but ultimately it's up to you to decide the best way in your situation. Just know that `FoveInterface` no longer relies on separate camera prefabs, and each eye should generally match what shows up in the editor view.

If you were using different prefabs or preexisting in-world cameras for your eye overrides, you can achieve similar results by having multiple `FoveInterface` cameras and setting each one to either "Left" or "Right" as needed in the `FoveInterface` inspector, which will then only render that camera to the specified eye and ignore the other eye.

(You are, of course, more than welcome to create your own static reference to a `FoveInterface*` behaviour instance if you know you will only have one, we just no longer enforce that patter ourselves.)

In pre-3.0.0 versions of the plugin, the FoveInterface behaviour needed to be last in the hierarchy in order to capture all of the image effects. This is no longer necessary, so you can relax about that.

### Camera position offset compared to previous version

If you are experiencing a camera position vertical offset after the upgrade to version 3.0, this may be due to the new pose type (standing or sitting) that was introduced in this version. Try to ajdust the pose type in the Fove Interface script settings to see if it fixes the problem.

## Upgrading from 2.1.2 (or before)

The source of the plugin is now available and the relevant behaviours are accessed from their scripts rather than a precompiled binary. Unfortunately, there is no way for us to inform Unity that the script files should be used in place of the behaviours from the old DLL file. The upgrade isn't too difficult, however.

We have already updated the included prefab objects with the source-based behaviour links, but if you constructed your own custom prefabs, or if you are using non-prefab-based objects directly in scenes, you will have to reattach the correct interface behaviour before they will work correctly. (You can tell that it is necessary because the Inspector window will show a warning, "The associated script can not be loaded.") In this case you should be able to drag the correct interface script file (`FoveInterface`) onto the "Script" field in the inspector when you select the affected prefab/object.

Your customizations should still be preserved in this case, so once you've done this for every object/prefab you created, you should be good to go.

Here are the simple steps to take:

1. Back up your project.
2. Take note of any places you have custom prefabs or scene objects that use `FoveInterface` or `FoveInterface2`. Any custom `FoveInterface` or `FoveInterface2` prefabs or scene objects not attached to prefabs will have to be updated manually after the upgrade.
3. Open your project (if it's already open, you may need to close Unity and reopen it to make sure the native DLLs aren't loaded).
4. Remove the whole FoveUnityPlugin folder from your Assets. (If you have custom prefabs, you should put them somewhere safe first.)
5. Double-click the new _Fove_Unity_Plugin.unitypackage_ file and import the new plugin. Unity should automatically reattach references to any of our prefabs in your scenes.
6. Reattach any missing `FoveInterface` behaviours in your prefabs and scenes. As stated above, any custom prefabs will need to have their behaviours reattached manually.
7. Select the _FOVE->Edit Settings_ menu item. The main area will display a list of any recommended project changes to make. Obviously, we recommend accepting all changes listed, but it's up to you and your project.

If you didn't skip any objects or prefabs, at this point you should be good to go. It's worth taking a quick check over all your behaviours to make sure that no connections were lost in the process. In our internal tests, nothing outside of the steps above was required.

## Upgrading from 1.3.1 (or before)

There may be more issues involved in this upgrade, but these steps should cover most of the major issues.

1. Because we created a new C# library for proper bindings, the old `Fove` namespace has moved to `Fove.Managed`, and we have adopted the same names in there that are used in the C++ SDK. Primarily this means that `Fove.FoveHeadset` becomes `Fove.Managed.FVR_Headset`; and `Fove.FoveCompositor` becomes `Fove.Managed.FVRCompositor`. It's likely that you weren't messing around with these guys, but if you were you'll have to make the changes accordingly. Most of the functionality within those classes has remained the same, though a few methods that *were* returning Unity-native objects now return FOVE-native objects, so keep an eye out.
2. Make sure no scripts use static methods. `FoveInterface` removes all obsolete static methods. If you're still using some, you will need to follow the instructions above in the "Static `FoveInterface` Methods" section.

And with that you should be good to go! If between that and reading this document you still have questions, hit us up on the forum: https://support.getfove.com/hc/en-us/community/topics

From here on, the guide assumes you are not using any legacy/obsolete methods or properties.

---

## Using Image Effects

We fully support using fullscreen image effects in all supported versions of Unity. Under most use cases, everything should appear in each eye exactly how it appears in the Unity Editor.

**NOTE:** The shader stack 2.0 resets the camera projection matrix during the `OnPreCull` method every frame, which removes the custom projection we set via the FOVE plugin. It seems that this reset is only necessary if you use their TAA effect. We've been able to use the source version of the plugin and comment out the line that resets the camera's projection matrix without any issues. The line is in the file `PostProcessLayer.cs`. We wish you luck, and hope that this requirement gets fixed before the new stack is released.

### HDR

The FOVE native client does not support HDR images. If you use HDR on your Unity cameras without adding a tonemapping effect, you may get a dull, grayed-out looking scene in your headset.

---

## The FOVE Settings Window

You can open the FOVE settings window by selecting the "FOVE" menu and selecting "Edit Settings..." There is a panel on the right which displays details about most options of you put your mouse cursor over them.

The main view displays any project settings suggestions that we've detected may improve your experience when using the FOVE plugin. You can press "Fix" next to any suggestions to automatically apply the change, or you can click "Fix All" to apply all recommended changes at once.

The "Settings" tab (near the top of the window) shows any global settings. At the moment there are only two settings:

* *Force Calibration*: Default off. If you check this, then every time you start play, it will force the FOVE runtime to recalibrate. Typically you would leave this off, however in some cases (such as trade show demos) it may be useful to have this.
* *Custom Desktop View*: Allow you to render a different view on your desktop display. See above [Display a different view on PC display](#display-a-different-view-on-pc-display) section for more details.
* *World Scale*: Default 1. This is the number of Unity units which represent 1 meter in your game/simulation. Unity's physics engine assumes 1 as well, so if your assets are different and you use Unity's physics in any way, make sure you adjust the gravity constant for your scale. Changing this value is important because we have to adjust eye separation and HMD offset so that the world "feels" right inside VR.

### Oversampling Ratio

Oversampling ratio determines the texel-to-pixel ratio when rendering to FOVE. This is important because the various shaders involved in transfering the images to the headset and making it look correct have a tendency to expand pixels near the center, make a 1-to-1 ratio looking kind of blocky and pixellated. We've picked a reasonable tradeoff between getting good performance and looking nice. But if your game requires more performance you can potentially decrease the oversampling ratio and get back a bit of perf at the cost of visual quality. We currently cap this between 0.01 (which looks just so awful) and 2.5 (which is probably overkill). If you need something else, let us know -- I'm very curious to hear your use case!

---

## The `FoveInterface` Inspector Panel

We tried to come up with helpful tooltips for each of the Inspector panel options, but they are admittedly a bit clunky, and you probably don't want multiple paragraphs of tooltip appearing for each item -- and who knows, maybe you are actually reading the docs to learn? (most people don't) -- so here's a bit more detail about each item in the Inspector for our interface component.

### Client Uses...

The next three checkboxes are essentially hint flags which indicate what pieces you intend to use. Typically you would use all three (gaze, orientation, and position), and so the default is for all three to be selected.

As an example for a case where you may not want to have all the attributes checked, if you have a constant in-place HUD overlay that you don't want to move as the player moves their head around, you may want to un-check all three. Or if you want to render a sky box, you wouldn't want to have position for sure, and you likely wouldn't want gaze on that interface, but I could see some fun effects having a skybox that responds to gaze...

### Pose Type

Starting with version 0.15.0 of the Fove SDK, we have added two different pose settings: sitting and standing. Sitting is the same as previous, and will tend to keep the user's view close to where the camera starts out.

Standing is the new mode, and it takes into account an offset of the position-tracking camera from the floor. In theory, using the standing mode, if the user has this value configured well, they should end up in similar places when using either Fove or SteamVR.

### Compositor Options

There are a few possible options which may be useful to you once you start getting more involved in developing your game with regard to how each camera communicates with the compositor.

#### Disable Timewarp

A little background:In an ideal, real-time, deterministic operating system, one could theoretically guarantee that all frames reached the HMD SDK before the HMD screen needed to refresh. These systems are very uncommon (and not generally considered useful for broad user consumption these days), and so even if your game runs quickly there's a good chance you'll end up with frame skipping every now and then. Time warp exists to catch those little skips and prevent them from becoming apparent to your users.

But if you are rendering an overlay that's attached to the camera, time warp during those little skips can actually make your supposedly-steady HUD appear to jump around rather a lot! For these instances, we have the *Disable Timewarp* toggle. If you're making a layer that's intended to stay in one place relative to the user's vision, definitely turn this on.

#### Disalbe Distortion 

Disable the automatic distortion correction applied correct lens distortion. 
Use this only if you are know what you are doing. For example if you want to apply your own distortion for some reasons.

---

## FOVE Interface Customization

Without a `FoveInterface` component, you can't take use any of our stuff, so it's kind of important.

You should only instantiate `FoveInterface` by attaching it to a prefab or Unity GameObject in your scene. (Adding any `FoveInterface` behaviour via `AddComponent` is unsupported and untested.) We have a bunch of options on these behaviours (and we'd like to think sensible defaults), but most/all of them are only modifiable via the inspector. So even if you wanted to change the default options in code, you couldn't (and shouldn't -- that's why the inspector exists, and you should use it).

Your means to interact with eye tracking is through the member methods on `FoveInterface`. So if you have an object which responds to eye gaze or blinking or whatever, you need to have a reference to a `FoveInterface` to get that. We highly recommend using Unity's functionality of exposing fields via the Inspector and requiring a `FoveInterface` that way -- but for runtime spawning of prefabs, it's perfectly acceptable to grab a reference using a method similar to the one shown below. (Note, the example below uses a static reference to the `FoveInterface` because `FindObjectOfType` is pretty expensive. You should determine if a static singleton pattern works best for your situation. Please program responsibly.)

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

### You move in local coordinates

The FOVE tracks your position and orientation and feeds that back into Unity. Your `FoveInterface` behaviour will automatically position itself in accordance with what the FOVE runtime reports for your headset. What this means is for you, dear dev, is that your `GameObject` is going to be spending a lot of time near (but generally not quite at) its origin.

If you don't want your view to always snap back to nearly-world-origin when you hit "Play", then you have to put it underneath (as a child of) another object. (I like to call this something like "FOVE Body" or "FOVE Root" in my projects.) You can now place and rotate (and even scale, if that's your "thing") this root object and the FOVE interface's GameObject will always move relative to its parent object.

Just keep in mind that (in case you're going to make an FPS and move the root transform around) stairs are considered bad in VR. Use elevators, or very gradual ramps if you must.

### Sitting vs. Standing

Since 0.15.0 of the FOVE SDK, we have exposed a "standing" mode which takes a configured camera position into account when reporting the HMD's position. For the old behaviour, set your `FoveInterface` to use "Sitting" mode. "Standing" mode will apply each user's configured camera position to the cameras when rendering. Assuming the setting has been configured properly, "Standing" mode offers a more accurate transfer of the user's view into VR to what their brains expect to see in terms of position off the floor and movement. However for simulations where the user is... well, sitting, we recommend using the "Sitting" mode which will allow you to position them more precisely in the world.

### Is a user looking at this collider?

There are a number of convenience methods built out for you which determine if the user is looking at a given collider, or a collection of colliders. These methods are designed to be similar to many of the `Raycast` methods provided by Unity, just based around the user's gaze. As such, they're called `Gazecast`. Check them out. They're pretty efficient and easy to use.

If you simply must do things by hand, you can also grab the gaze rays as UnityEngine `Ray` objects from the method `GetGazeRays()` on your `FoveInterface` instance.

### "Asynchronous" data access

The plugin automatically handles acquiring pose and gaze data for you in sync with the rendering pipeline, however Unity's FixedUpdate function can be called more frequently than and semi-out-of-sync with the standard Update function. (Default looks like 50 times per second for physics -- you can chage this from `Edit > Project Settings > Time`.) For more information on execution of Unity events, see this document: https://docs.unity3d.com/Manual/ExecutionOrder.html (`FixedUpdate` is not ACTUALLY executed out of sync, so if that's what you need, you should probably come up with something else using threads and mutexes.)

If you want to get the immediate, most up-to-date, current information from the SDK, there are methods available to do so:

* `GetHMDRotation_Immediate`
* `GetHMDPosition_Immediate`
* `GetLeftEyeVector_Immediate`
* `GetRightEyeVector_Immediate`

## Add-ins and Camera Images

The new FoveManager system has the ability to register add-ins to get updates called back every frame in sync with the manager's updates. This is intended for use by systems that need to access lower-level functionality of the headset directly. You can see an example of this functionality in `FoveResearch.cs`, which registers itself as an add-in and then on add-in callbacks updates 2D textures for the eye camera and position camera images.

If you want to use this script file, you can add it into your project anywhere (we use the `Source/Utility/` folder inside out plugin, usually), and then you can query `FoveResearch.EyesTexture` to get the current eyes texture, for instance. The add-in goes so far as to not grab duplicate textures if the camera image hasn't changed, so it should be good for general use. We know a lot of researchers have requested this feature, so we hope that you can put it to good use!

---

## Known Limitations

There are some limitations with the current system which we may be fixing in the near future. (If you have any requests, please contact us!)

* Only Unity Editor version 5.4+ are supported.
* Camera orthogonal projections are currently not supported. Perspective projection is always used at runtime to match heaset view parameters. Keep this in mind when you build your game UI.
* Using "skybox" camera clear mode and set the scene skybox to null (scene ligthing settings) may result in a white screen inside the heaset.
* Unity Editor update overhead seems to have increased in recent versions of Unity (2017+). This sometimes result in missed frames and can cause the scenes to stutter when you are playing the scene in-editor. Howevern, these display issues should vanish when you build and launch the real project player.
* Calling `EnsureEyeTrackingCalibration` will force the calibration to reset, even if you have an active calibrated profile. This will be fixed in a future update to the SDK and runtime.
* Gaze vector can be null (0,0,0) if the calibration has been not properly run.
* SteamVR maintains its own HMD offset values which get applied to the camera in your scene. These are almost certainly different from the position as reported by FOVE. If you want your project to be compatible with both, you can achieve this by using "Standing" mode in the `FoveInterface` inspector.
* In addition, because SteamVR/OpenVR handle the position of the interface object on their own, the world scale values won't work when running in SteamVR mode. We recommend keeping your world scale at 1 for highest compatibility with other VR systems.
* Unity post process stack is not supported as it internally modifies the camera projection matrix.
* Having several fove interfaces with different compositor options (disable timewarp, etc.) at the same time is not supported.