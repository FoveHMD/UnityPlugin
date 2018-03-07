# Fove Unity Plugin

This plugin automates setting up a rendering interface to the Fove Compositor from within Unity via one simple prefab. It also exposes eye-tracking information in a Unity-native way via a static interface.

## Getting Started
1. Install the Fove runtime and make sure it's running.
2. Import the package into Unity.
3. Drag either the _Fove Rig_ or _Fove Rig 2_ prefab from the `FoveUnityPlugin/Prefabs/` directory into your scene.
4. Press Play.

That's all you need to do for a simple setup. Of course, this only gets you the most basic setup, and there are many customizations you may want. We think we've figured out the majority of use cases, so check the rest of this guide before deciding you need to do something too extreme.

## What You Get
The Unity package adds a folder called _FoveUnityPlugin_ to your project. Within this folder, there are several items. We will go over them here.

* *Prefabs*: This folder contains a few prefabs for you to use to get started.
  * *Fove Interface*: This is the classic interface prefab. It's the most reliable, though perhaps less performant (depending on you scenes and what you're trying to do), prefab to use at the moment. [NOTE: This prefab automatically positions itself at runtime based on the position and orientation of the FOVE headset!]
  * *Fove Interface 2*: This is a sample prefab and the minimum requirement for running powering your Unity VR project with FOVE. This prefab can safely be dragged into a scene and it should "just work" for you. [NOTE: This prefab automatically positions itself at runtime based on the position and orientation of the FOVE headset!]
  * *Fove Rig*: This is just such a hierarchy, but it's only here to prevent breaking users' projects from past versions. Please use _Fove Rig 2_ instead.
  * *Fove Rig 2* (experimental): This is simply an empty _GameObject_ with a _Fove Interface 2_ setup as a child. Plop one of these dudes in your scene and go from there. If you want to change the origin point of the VR experience, you would move the top-level container object. (Requires VR to be enabled for your project from the `Edit->Project Settings->Player` inspector panel.)
  * *FoveCameraExample*: This is a prefab used in the legacy setup. Feel free to ignore it. It's really just a regular camera prefab. If you add image effects to this prefab, then the default `FoveInterface` prefabs will get those image effects at runtime.
* *Resources*: This folder contains some shaders that were/are used with the legacy setup for displaying the rendered imaged to your screen. It's perfectly safe to ignore this folder. In fact, I won't even go into the two shaders that it contains -- that's how much you shouldn't worry about this guy. Go ahead and look, but it's pretty uninteresting.
* *x86_64*: This folder contains required native libraries for FOVE to run. It's a standard name to indicate that these libraries are for 64-bit x86 machines.
  * *FoveClient.dll*: The magic library. Well, it's the FOVE library, anyway. This is just the publicly released client library that you can get off our website. We upgrade this with every release of the Unity plugin, as there may be new features which are specific to new client versions, so it's best to leave this guy alone. If you keep up-to-date with the Unity plugin, you will also stay caught up with the FoveClient library.
* *FoveClient_CSharp.dll*: This is the official managed C# bindings DLL. It essentially builds up a couple of C# classes which call into the C API bindings we expose in *FoveClient.dll*, and presents you the data and structs as close to raw as we can while still keeping it looking and feeling like healthy C#. If you are so inclined, feel free to use this directly, but you'll probably end up rewriting a lot of rather tedious code that we already wrote for you if you use the `FoveInterface2` behaviour.
* *Source*: We are releasing the source for our plugin so that you can see what's going on, make changes if you don't like something, and suggest improvements if you feel so inclined. All our source for the plugin is contained beneath this folder.
  * *Behaviours*:
    * *FoveEyeCamera*: This behaviour is automatically added to generated camera objects when using the legacy interface. You shouldn't have been using it before and you DEFINITELY shouldn't be using it now. Please ignore. In fact, just forget this exists.
    * *FoveInterface*: This is the traditional behaviour. It creates two child cameras at runtime (optionally instantiated from a prefab you can specify) which it spaces out to perform stereoscopic rendering. This does not take advantage of any of Unity's built-in stereo-rendering optimizations, however it is stable and reliable, and some of us still use it in-house due to the extra control we can have over the left and right cameras.
    * *FoveInterface2* (experimental): This class utilizes Unity's built-in stereo rendering optimizations where possible to simplify your scene and improve performance. Unity will split this camera into two under the hood, and it therefor can optimize the rendering as well. Because what we are doing is a little nonstandard from Unity's perspective, there may be some unforeseen performance implication, so while we use this in-house ourselves, we do recommend evaluating the performance on your system as well. This requires that VR be enabled for your scene.
    * *FoveInterfaceBase*: The base class for both `FoveInterface` and `FoveInterface2`. This class cannot be instantiated directly, and in the future we plan to move more shared functionality into this class wherever possible to keep the differences between the two to a minimum.
  * *Editor*: This directory stores editor scripts used to show custom inspectors and the FOVE Settings window.
    * *FoveInterfaceBaseEditor*: The Inspector script for all `FoveInterfaceBase` subclasses. It contains the majority of customizations shared between all implementations.
    * *FoveInterfaceEditor*: There are some important editable properties for `FoveInterface` (principally, setting the camera prefab) which this script adds to the base editor when a `FoveInterface` is selected.
    * *FoveSettingsEditor*: Contains all the core code for the "FOVE Settings" editor window.
    * *ProjectChecks*: A container for all of the performance checks and project recommendations done for the "FOVE Settings" window. We will continue to improve these checks, adding new ones and adjusting them as necessary with each plugin release so that you can be confident your projects will perform optimally.
  * *ScriptableObjects*: Contains code for any `ScriptableObject` classes used by our plugin.
    * *FoveSettings*: The container class for our per-project settings. Right now it does very little, but the plan is to expand it to make your life easier. We automatically create one instance of this which goes into the Resources folder and is the file referenced by the rest of the plugin.

---

## Upgrading from 2.0.0 (or before)
The source of the plugin is now available and the relevant behaviours are accessed from their scripts rather than a precompiled binary. Unfortunately, there is no way for us to inform Unity that the script files should be used in place of the behaviours from the old DLL file. The upgrade isn't too difficult, however.

We have already updated the included prefab objects with the source-based behaviour links, but if you constructed your own custom prefabs, or if you are using non-prefab-based objects directly in scenes, you will have to reattach the correct interface behaviour before they will work correctly. (You can tell that it is necessary because the Inspector window will show a warning, "The associated script can not be loaded.") In this case you should be able to drag the correct interface script file (either `FoveInterface` or `FoveInterface2`) onto the "Script" field in the inspector when you select the affected prefab/object.

Any of your customizations should still be preserved in this case, so once you've done this for every object/prefab you created, you should be good to go.

Here are the simple steps to take:

1. Back up your project.
2. Take note of any places you have custom prefabs or scene objects that use `FoveInterface` or `FoveInterface2`. Any custom `FoveInterface` or `FoveInterface2` prefabs or scene objects not attached to prefabs will have to be updated manually after the upgrade.
3. Open your project (if it's already open, you may need to close Unity and reopen it to make sure the native DLLs aren't loaded).
4. Remove the whole FoveUnityPlugin folder from your Assets. (If you have custom prefabs, you should put them somewhere safe first.)
5. Double-click the new _Fove_Unity_Plugin.unitypackage_ file and import the new plugin. Unity should automatically reattach references to any of our prefabs in your scenes.
6. Reattach any missing `FoveInterface` or `FoveInterface2` behaviours in your prefabs and scenes. As stated above, any custom prefabs will need to have their behaviours reattached manually.
7. Select the _FOVE->Edit Settings_ menu item. Near the top of the window, select which `FoveInterface*` you intend to use to get recommendations for that version. The area below will display a list of any recommended project changes to make the most of your selected interface. We recommend accepting all changes listed.

If you didn't skip any objects or prefabs, at this point you should be good to go. It's worth taking a quick check over all your behaviours to make sure that no connections were lost in the process. In our internal tests, nothing outside of the steps above was required.

## Upgrading from 1.3.1 (or before)
First of all, we took a lot of care to keep FoveInterface backwards compatible with previous versions. So if you are happy to keep things as-is, you should be able to just keep using your setup as before. Keep in mind, however, that new features may or may not be released for the legacy interface in the future. So if you are worried about having all the latest features, it might be worth upgrading your project.

0. This one won't likely affect many people, but because we created a new C# library for proper bindings, the old `Fove` namespace has moved to `Fove.Managed`, and we have adopted the same names in there that are used in the C++ SDK. Primarily this means that `Fove.FoveHeadset` becomes `Fove.Managed.FVR_Headset`; and `Fove.FoveCompositor` becomes `Fove.Managed.FVR_Compositor`. It likely that you weren't messing around with these guys, but if you were you'll have to make the changes accordingly. Most of the functionality within those classes has remained the same, though a few methods that *were* returning Unity-native objects now return FOVE-native objects, so keep an eye out.
1. In your own scripts, `FoveInterfaceBase` is almost certainly the class you'll want to reference for accessing information about gaze. You can technically use either `FoveInterface` or `FoveInterface2` as well, but all the functions should be the same, and it makes your custom behaviours more flexible.
2. Make sure no scripts use static methods. `FoveInterfaceBase` removes all obsolete static methods from `FoveInterface`. If you're still using some, you will need to add a reference to a `FoveInterfaceBase` to your behaviour and then either link it in the inspector or find a `FoveInterfaceBase` at runtime. (You are, of course, more than welcome to create your own static reference to a `FoveInterface*` behaviour instance if you know you will only have one, we just no longer enforce that patter ourselves.)

And with that you should be good to go! If between that and reading this document you still have questions, hit us up on the forum: https://support.getfove.com/hc/en-us/community/topics

From here on, the guide assumes you are not using any legacy/obsolete methods or properties.

---
## `FoveInterface` or `FoveInterface2`?!
We get this question a lot. Here is a rundown of the pros and cons of each interface. We highly recomend that you pick one

### `FoveInterface`
The original `FoveInterface` was designed to work before Unity had any VR stereoscopic support, but it continues to be worthwhile today. It creates two cameras positions roughly where the user's eyes are in virtual space in order to submit stereo images to the FOVE runtime. These cameras are created at runtime based on a prefab Unity `Camera` object, so if you want to add image effects, you can put them on the prefab and they will be used for the eye cameras.

Pros:
* **Reliable** -- It worked before Unity had VR support, and it continues to work very well.
* **Customisable** -- There are advanced options to replace each eye individually with separate `Camera` objects in the scene -- a technique we've had to do in-house, but extremely rarely.
* **Works Without VR** -- Because it implements a "poor man's VR" itself, this interface can work without VR enabled in your project (which it isn't by default). So it's slightly easier to hit the ground running.

Cons:
* **Slower** -- Each camera is treated as a separate camera in-engine, and so all shadows, frustum culling, and other effects will be run once for each camera; objects will be rendered once for each camera; and so forth. There are no possible optimisations for this path.
* **More Objects** -- More complexity is required to do advanced things with this setup. For example, toggling full screen image effects on these objects requires you find these camera objects at runtime and enable/disable effects once for each camera when desired.
* **Doesn't Play Well With SteamVR** -- SteamVR uses Unity's built-in VR capabilities, and as such this behaviour may conflict if you try to play with SteamVR as an option to choose from.

### `FoveInterface2`
A newer, more modern interface, `FoveInterface2` was created to take some advantage of Unity's built-in stereoscopic rendering optimizations to improve performance and simplify your scenes and scripts. We manipulate the stereo position and projection matrices for the internal camera split, which is a nonconventional use of the data. This has sometimes resulted in some odd errors appearing in cases where dynamic shadows are cast, however, and so we are still calling this interface "experimental" while we try to work out how to make things more stable.

Pros:
* **Potentially Faster** -- For scenes with lots of dynamic shadows/lights, Unity will render them once for both eyes. You can even enable Unity's experimental single-pass stereo if you're feeling extra spicy. The amount that this speed improvement will be noticeable depends on your scene.
* **Simpler Interaction** -- The object which contains the `FoveInterface2` is also the camera object. So if you want to add full-screen effects, or toggle them on/off at runtime, you can do so more easily by connection the toggler to the interface via the Inspector, rather than searching out objects at runtime. There is also only one place to go to toggle effects (as opposed to the two cameras for the non-experimental interface).
* **Improved SteamVR Compatibility** -- In the presence of SteamVR, this behaviour will shut down and let SteamVR take over, which makes it play a little better if you want to allow either system to exist. We have further improvements planned for this in future plugin updates.

Cons:
* **Less Customization** -- Because Unity splits the camera into left/rigth itself, it is more difficult to have effects which apply to only one eye or the other. (Internally, we find that this is almost never required.)
* **Possible Internal Exceptions** -- We've observed that in some circumstances (possibly related to having dynamic shadows generated) and in some versions, Unity will emit exceptions internally as a result of this interface setting the stereo camera properties. You should evaluate whether you see this behaviour in your own project before committing to this interface. If these exceptions are emitted frequently, it can affect your performance in the editor. It is likely that performance in a precompiled game will be better, however.
* **Requires VR Enabled** -- The _FOVE Settings_ window can automatically enable this and add the correct VR device to your project. There is no "FOVE" VR device built into Unity, which can make the requirements a bit confusing. If you fix all recommendations, however, you should be good to go with little trouble.

---
## Image Effects
We fully support using fullscreen image effects in all supported versions of Unity. It is important to note, however, that you have to make sure that the FOVE-supplied behaviours are last in the list on your cameras.

For `FoveInterface`, this means simply that your camera prefab doesn't contain a `FoveEyeCamera` behaviour. The interface will automatically append this behaviour at the end of the behaviour list, effectively making it run last and capturing all of your image effects.

For `FoveInterface2`, this means that all of your image effects need to be above the `FoveInterface2` behaviour.

### Adding/Removing Effects Live
Any effects appended to your game objects at runtime would be added to the list after the respective FOVE behaviours, and so would not appear in your HMD. If you want to add or remove effects, we recommend attaching the effects to the prefabs or objects in your scene (at the correct position in the hierarchy) and then toggling the relevant behaviours on and off as needed rather than adding and removing behaviours live.

(You could theoretically also adjust your script execution order using the _Edit->Project Settings->Script Execution Order_ panel, however this is likely overkill for most projects, and it's harder to quickly check and debug when an effect isn't appearing.)

### Going for HDR?
The FOVE native client does not accept HDR images. If you try to use HDR on your cameras without adding a tonemapping effect, you will likely get a dull, grayed-out looking scene in your headset. 

If using `FoveInterface`, you must add a tonemapping effect to your camera prefab. Make sure that you don't have `FoveEyeCamera` attached, either. This will force the `FoveInterface` to append it last, ensuring that all your filters and tone mapping are applied before the frames are sent to the HMD.

In the case of `FoveInterface2`, you need to add the tonemapping effect directly to the _GameObject_ which holds the `FoveInterface2` behaviour. Further, you must make sure that `FoveInterface2` is the last behaviour on the stack.

(Again, you could technically adjust your behaviour execution order for the project, as mentioned above, but for most use cases it's still likely easier to put the FOVE behaviours at the bottom of the list.)

---
## The `FoveInterface` Inspector Panel
We tried to come up with helpful tooltips for each of the Inspector panel options, but they are admittedly a bit clunky, and you probably don't want multiple paragraphs of tooltip appearing for each item -- and who knows, maybe you are actually reading the docs to learn? (most people don't) -- so here's a bit more detail about each item in the Inspector for our interface component.

### World Scale
This number represents how many **units** there are in one meter in your game. By default, one "Unity unit" is one meter. That is, if you have a unity cube that's 1 unit long on each side, that cube is also 1m^3 (one cubic meter). Not every game uses this system, however. Have some examples:

* If you want each Unity unity to be 1cm, then you would set *World Scale* to 100 (meaning that there are 100cm in 1m).
* If you want each Unity unity to be 1ft, then you would set *World Scale* to 3.28084, which is the number of feet in a meter.
* If you want each Unity unit to be decameters (an uncommon but actually real measurement where each decameter is 10 meters), you would set *World Scale* to 0.1, indicating that one meter is 1/10th of a Unity unit in your world system.

We do this because eye separation is a large part of how we sense scale, and if you use units other than meters, we have to know what that scaling factor is so we can scale eye separation and head movement to still feel properly like you are human-size in the game world according to your intended scale.

### Client Uses...
The next three checkboxes are essentially hint flags which indicate what pieces you intend to use. Typically you would use all three (gaze, orientation, and position), and so the default is for all three to be selected. Technically this has the potential to not wind up certain systems if you aren't using them, reducing CPU load, but there is no guarantee and you probably should actually just forget I said that.

As an example for a case where you may not want to have all the attributes checked, if you have a constant in-place HUD overlay that you don't want to move as the player moves their head around, you may want to un-check all three. Or if you want to render a sky box, you wouldn't want to have position for sure, and you likely wouldn't want gaze on that interface, but I could see some fun effects having a skybox that responds to gaze...

### Skip Auto Calibration Check
This toggle tells the `FoveInterface2` component that you intend to handle checking for calibration yourself. See below for more details, but essentially you should want this to be checked in a finished product. However for some research projects or trade show demonstration versions you may prefer to check for calibration on ever instance of the program, and `FoveInterface2` will do so for you if you uncheck this toggle.

### Oversampling Ratio
Oversampling ratio determines the texel-to-pixel ratio when rendering to FOVE. This is important because the various shaders involved in transfering the images to the headset and making it look correct have a tendency to expand pixels near the center, make a 1-to-1 ratio looking kind of blocky and pixellated. We've picked a reasonable tradeoff between getting good performance and looking nice. But if your game requires more performance you can potentially decrease the oversampling ratio and get back a bit of perf at the cost of visual quality. We currently cap this between 0.01 (which looks just so awful) and 2.5 (which is probably overkill). If you need something else, let us know -- I'm very curious to hear your use case!

### Headset Overrides
This section is closed off by default because you typically wouldn't mess around with it. We use it promarily for prototyping and debugging. But since it's there, we decided to make it usable. So here goes.

#### Use Custom Position Scaling
This toggle indicates that you want to use the specified values below instead of the defaults.

#### Position Scales
Position scaling refers to the X/Y/Z values reported by the SDK, and how those map to the Unity engine. This is a direct multiplicative relationship between the values received by the SDK and the values set on the `FoveInterface2` game object's transform. The default is a direct pass through, but if (for whatever reason -- likely a desire to make your users sick) you want to change these values, you can adjust each axis individually to make, e.g., side-to-side movement exaggerated while front-to-back is scaled down or something.

#### Use Custom Eye Placement
Toggle this if you want the FOVE eyes to be somewhere other than where we think they should be, according to the three values below. If you don't check this box, none of the values below will be used.

#### Inter Ocular Distance
This is the distance between your eyes. Many people call this "interpupilary distance", which is a misnomer as the distance between your pupils actually changes quite frequently as you look at objects closer and farther away -- and most other headsets can't even SEE your eyes, so there's no way they could even know your IPD. And anyway, IPD doesn't need to be used to get pretty good results compared to IOD.

So if you want to feel like your eyes are unusually close together or far apart (or if they are in real life) you can change this value. Otherwise it will pull the value specified in the SDK.

#### Eye Height
The position of the eyes isn't necessarily the same as the position origin of the HMD. This value is how high the eyes are from the HMD's position origin. If you wanted to see how dizzy/sick you would feel if your eyes were suddenly above your head or below, you could change this value. It's trippy and uncomfortable and not recommended for the faint of heart.

#### Eye Forward
This value represents the forward-facing offset from the HMD origin to where your eyes sit. If you want to feel what it's like having your eyes either in front of or behind your head (and I can't recommend it) you could hard-code this value yourself. But you probably shouldn't inflict that on others.

### Compositor Options
There are a few possible options which may be useful to you once you start getting more involved in developing your game with regard to how each camera communicates with the compositor.

#### Layer Type
The FOVE compositor supplies you with one of three layer types you can render to: "Base", "Overlay", and "Diagnostic":

* Base: This layer is considered fully opaque, and should represent the core of your game experience. There is only allowed to be a single base layer, and if you request more than one, you'll get an error when running. But you should probably *have* one for most games.
* Overlay: You can have multiple of these (keep in mind that each layer incurs a performance toll on the overall system, so we highly recommend keeping the number of layers low). They are considered to have an alpha channel and will show the base layer through any transparent areas. These are good for HUDs and special scene effects, for example.
* Diagnostic: These layers are treated the same as Overlay layers in terms of translucency, but they are always drawn over/in front of the other layers. They can be useful if you want to display some debug information that will never be blocked by the gameplay.

Having layers can be pretty powerful, but also can cause problems, so please be responsible with how you use them.

#### Disable Timewarp
A little background:In an ideal, real-time, deterministic operating system, one could theoretically guarantee that all frames reached the HMD SDK before the HMD screen needed to refresh. These systems are very uncommon (and not generally considered useful for broad user consumption these days), and so even if your game runs quickly there's a good chance you'll end up with frame skipping every now and then. Time warp exists to catch those little skips and prevent them from becoming apparent to your users.

But if you are rendering an overlay that's attached to the camera, time warp during those little skips can actually make your supposedly-steady HUD appear to jump around rather a lot! For these instances, we have the *Disable Timewarp* toggle. If you're making a layer that's intended to stay in one place relative to the user's vision, definitely turn this on.

---

## FOVE Interface Customization
Without a `FoveInterface*` component, you can't take use any of our stuff, so it's kind of important.

You should only instantiate `FoveInterface*` by attaching it to a prefab or Unity GameObject in your scene. (Adding any `FoveInterface*` behaviour via `AddComponent` is unsupported and untested.) We have a bunch of options on these behaviours (and we'd like to think sensible defaults), but most/all of them are only modifiable via the inspector. So even if you wanted to change the default options in code, you couldn't (and shouldn't -- that's why the inspector exists, and you should use it).

Your means to interact with eye tracking is through the methods on `FoveInterfaceBase`. So if you have an object which responds to eye gaze or blinking or whatever, you need to have a reference to a `FoveInterfaceBase` to get that. We highly recommend using Unity's functionality of exposing fields via the Inspector and requiring a `FoveInterfaceBase` that way -- but for runtime spawning of prefabs, it's perfectly acceptable to grab a reference using a method similar to the one shown below. (Note, the example below uses a static reference to the `FoveInterfaceBase` because `FindObjectOfType` is pretty expensive. You should determine if a static singleton pattern works best for your situation. Please program responsibly.)
```
using UnityEngine;

public RespondToEyeTracking : MonoBehaviour {
  public GazeReactiveBehaviour thePrefab;

  private static FoveInterfaceBase _theFoveInterface;
  
  void Awake() {
    if (_theFoveInterface == null) {
      _theFoveInterface = FindObjectOfType(typeof(FoveInterfaceBase)) as FoveInterfaceBase;
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

You could also find the instance as part of your custom behaviour's `Awake` or `Start` method, or even assign the instance as a static property on your behaviour's class so all instances but the first will already have access to it. Just keep in mind that searching the whole scene graph can be expensive and if you have prefabs that are spawning very frequently, it could slow down your scene significantly to have each of them search for the `FoveInterface*` in use whenever they're created.

### Calibration Checks
We assume that you want to make sure that the user has a valid calibration when using your program, so we have provided a function to make sure that they have a valid calibration. The function you're looking for is `EnsureEyeTrackingCalibration`. You should take the player somewhere nice that they don't need to be interacting with or seeing anything specific (we recommend a separate scene specifically for this purpose), call `EnsureEyeTrackingCalibration` on a `FoveInterfaceBase` instance, and then keep checking `IsEyeTrackingCalibrating()`. When it returns false, you should be safe to resume the rest of your game.

If you do *not* want to deal with calling this yourself, you can use the *Skip Auto Calibration Check* toggle in the Inspector and make sure it's unchecked, which will force a calibration check to begin as soon as the first `FoveInterface*` object wakes up in your game. It works, but it's a bit clunky. (The automatic check is disabled by default in our prefabs, as it gets a bit tedious going through the check all the time while developing a game.) Also, sometimes you have publisher/tech videos and copyright notices that you want your users to see and which shouldn't be skippable (for licensing reasons); and having calibration running atop all that kind of defeats the purpose.

Note that calibration checks will probably take over the HMD screen, so you can't see anything _other_ than the calibration check while it's going on; and eye gaze is undefined in this state. Hence why you should take your user somewhere safe where there won't be accidentally interacting with things during calibration check..

### You move in local coordinates
The FOVE tracks your position and orientation and feeds that back into Unity. Your `FoveInterface*` behaviour will automatically position itself in accordance with what the FOVE runtime reports for your headset. What this means is for you, dear dev, is that your `GameObject` is going to be spending a lot of time near (but generally not quite at) its origin.

If you don't want your view to always snap back to nearly-world-origin when you hit "Play", then you have to put it underneath (as a child of) another object. (I like to call this something like "FOVE Body" or "FOVE Root" in my projects.) You can now place and rotate (and even scale, if that's your "thing") this root object and the FOVE interface's GameObject will always move relative to its parent object.

Just keep in mind that, if you're going to make an FPS and move the root around, stairs are considered bad in VR.

### Is a user looking at this collider?
There are a number of convenience methods built out for you which determine if the user is looking at a given collider, or a collection of colliders. These methods are designed to be similar to many of the `Raycast` methods provided by Unity, just based around the user's gaze. As such, they're called `Gazecast`. Check them out. They're pretty efficient and easy to use.

If you simply must do things by hand, you can also grab the gaze rays as UnityEngine `Ray` objects from the method `GetGazeRays()` on your `FoveInterfaceBase` instance.

### VR Overlay Via Multiple Interfaces
You can now have multiple `FoveInterface2` instances, which allows for a variety of effects -- most notably, you can have HMD-locked interface overlays. There should be an example of this in the Unity sample project; however the general idea is that you have two `FoveInterface2`s, one for the main scene rendering and the other for only the overlay.

On the overlay interface, you would set the camera it uses (ideally a prefab) to only render your overlay layer (you may or may not want to prevent the main camera from rendering the overlay layer depending on your goals). It's likely that you may also want to prevent the overlay's orientation and position from changing as the user moves their headset, which can be done by unchecking the "Position" and "Orientation" checkboxes in the `FoveInterface2` inspector panel. Of course, if you're going to have an overlay, you should open the Compositor options" foldout on the inspector and change "Layer Type" to "Overlay", so the compositor knows to preserve the transparency.

Lastly, if you disable orientation and position on the FoveInterface, you should definitely check "Disable Timewarp" in the Compositor options foldout, otherwise you will almost certainly get uncomfortable judder as your stationary layer gets timewarped unnecessarily.

### "Asynchronous" data access
The plugin automatically handles acquiring pose and gaze data for you in sync with the rendering pipeline, however Unity's FixedUpdate function can be called more frequently than and semi-out-of-sync with the standard Update function. (Default looks like 50 times per second for physics -- you can chage this from `Edit > Project Settings > Time`.) For more information on execution of Unity events, see this document: https://docs.unity3d.com/Manual/ExecutionOrder.html (`FixedUpdate` is not ACTUALLY out of sync, so if that's what you need, you should probably come up with something else using threads and mutexes.)

If you want to get the immediate, most up-to-date, current information from the SDK, there are methods available to do so:

* `GetHMDRotation_Immediate`
* `GetHMDPosition_Immediate`
* `GetLeftEyeVector_Immediate`
* `GetRightEyeVector_Immediate`

---

## Known Limitations
There are some limitations with the current system which we may be fixing in the near future. (If you have any requests, please contact us!)

* Calling `EnsureEyeTrackingCalibration` will force the calibration to reset, even if you have an active calibrated profile. This will be fixed in a future update to the SDK and runtime.
* SteamVR maintains its own HMD offset values which get applied to the camera in your scene. These are almost certainly different from the position as reported by FOVE. Because those offsets are handled by SteamVR, there will be a notable offset between your scenes when running in SteamVR mode versus FOVE native mode. It may be possible for you to detect which mode and adjust the VR camera's parent object's placement, but you would have to make that decision for yourself. In future versions of the FOVE runtime, we will add a similar offset/calibration feature which should help ensure that both systems are configured similarly. For now, they are likely pretty far off.
* In addition, because SteamVR/OpenVR handle the position of the interface object on their own, the world scale values won't work when running in SteamVR mode. We recommend keeping your world scale at 1 for highest compatibility with other VR systems.
* With the original interface, you cannot use the game object itself as an eye camera prototype. In fact, you cannot use any  Game Object which has a FoveInterface component as the prototype, as this would cause a horrible cascade of instantiating more and more objects until you run out of memory and crash. If you try to use a FoveInterface as the prototype, it will emit a few errors on launch and disable the base FoveInterface. (The rest of the Game Object's components should still function as normal.)

