# Fove Unity plugin upgrade guide

This document explains how to smoothly upgrade the plugin from a previous version in a Unity project.

## Upgrading to 4.2.0

A public function has been renamed so you may have to adjust your code (see the [changelog](./Changelog.md) for more details).

## Upgrading to 3.2.0

All Fove APIs return types changed from `Type` to `Result<Type>`. The new result type contains both the previously returned value plus an error code. 
When you upgrade your code, we recommend you to properly handle error codes where it be critical for your application. 
In all other places you can simply have your old code running by adding `.value` at the end of Fove function calls (e.g. `GetGazeRays().value` where it was `GetGazeRays()`).
Note that we also added implicit conversion from `Result<Type>` into `Type`, so some calls may already compile out of the box.

`GazeCastPolicy` settings moved from the `FoveInterface` to the Fove settings. It is now used both by `FoveInterface.GazeCast` functions and well as the new gaze-based object detection APIs. The setting can be changed at runtime from the `FoveManager` instance.

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

However, because we removed the `FoveInterface2` and `FoveRig2`, if you used either of these you will want to still remove the `Behaviours` directory, but leave your prefabs and just import the new plugin on top of your existing directory. This will break your prefabs, which you should select and update them to the FoveInterface behaviour by-hand. We also recommend removing the FoveCamera and FoveInterfaceBase behaviours.

Finally, if any of your other behaviours store a reference to one of the removed behaviours (e.g., `FoveInterfaceBase` or `FoveInterface2`), you will have to update those scripts (along with some other changes -- see below) to require a `FoveInterface`, and you will need to update the missing connections in any scenes as well.

### Obsolete Static `FoveInterface` Methods

Until now, we maintained a static reference of the first `FoveInterface` object registered in each scene, and that object was used for all the static method calls (which have been marked obsolete since version 1.2.0). If you've been using these static methods you will need to acquire your own reference now. We recommend either:

1. Create a public `FoveInterface` member on your custom `MonoBehaviour` class and assign the correct object to it using Unity's built-in inspector bindings.
2. On your `MonoBehaviour`'s `Start` event, find the `FoveInterface` you want using Unity's built-in search for objects (either by type or name, depending on your needs), and then cache that reference as a local member variable so you don't have to search for it every frame. You definitely won't want to be searching your scene hierarchy every frame each time for the `FoveInterface` as such searches are fairly costly for performance. You shouldn't worry about caching the value unless the cache would outlive the `FoveInterface` (e.g. on scene change).

### Draw Layer and UI

In this new version we remove to possibility to choose the draw layer on which the camera renders. All the rendering is now always performed on the base layer.
For see how to deal UI now, check [Add UI to your game](PluginGuide.md#add-ui-to-your-game)

### Camera Effects and Camera Prefabs

The new `FoveInterface` uses the camera object it is attached to directly rather than messing around with separate camera prefabs or anything. This means that if you had special prefabs for your eye cameras previously, you will have to make sure that all the relevant screen effects are attached to the actual FoveInterface. The easiest way to do this may be to copy the behaviours for each effect from the prefab to the `FoveInterface` camera, but ultimately it's up to you to decide the best way in your situation. Just know that `FoveInterface` no longer relies on separate camera prefabs, and each eye should generally match what shows up in the editor view.

If you were using different prefabs or preexisting in-world cameras for your eye overrides, you can achieve similar results by having multiple `FoveInterface` cameras and setting each one to either "Left" or "Right" as needed in the `FoveInterface` inspector, which will then only render that camera to the specified eye and ignore the other eye.

(You are, of course, more than welcome to create your own static reference to a `FoveInterface*` behaviour instance if you know you will only have one, we just no longer enforce that patter ourselves.)

In pre-3.0.0 versions of the plugin, the FoveInterface behaviour needed to be last in the hierarchy in order to capture all of the image effects. This is no longer necessary, so you can relax about that.

### Camera position offset compared to previous version

If you are experiencing a camera position vertical offset after the upgrade to version 3.0, this may be due to the new pose type (standing or sitting) that was introduced in this version. Try to adjust the pose type in the Fove Interface script settings to see if it fixes the problem. Old behaviour should use the "Sitting" mode.

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