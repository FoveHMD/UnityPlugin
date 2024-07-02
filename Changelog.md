# FOVE Unity Plugin Changelog

## 4.4.1
* Update FOVE SDK from v1.3.0 to v1.3.1

## 4.4.0
* Updated the FOVE SDK from 1.2.0 to 1.3.0
* Removed the GazeCastPolicy settings from the plugin (see the [upgrade guide](./UpgradeGuide.md) for more information on how to update your code)
* GazeRecorder: Rename RecordingRate.70FPS/120FPS into RecordingSync.SyncWithRendering/SyncWithEyeTracking
* Added the `FoveManager.GetGazeScreenPositionCombined` API to get the 2D screen position of the combined gaze.
* Removed deprecated `FoveManger.IsHardwareReady()`, `FoveManager.WaitForHardwareReady`, and `FoveManager.IsHardwareReady()`
* Reworked the `ForceCalibration` FOVE setting:
  * Renamed it into `EnsureCalibration`
  * Add a corresponding `EnsureCalibration` property to the FoveManager. That can be used at runtime to change the behavior.
  * Use the `EnsureCalibration` FOVE setting only for the initialization of the `FoveManager.EnsureCalibration` property value
* Reworked the `CustomDesktopView` Fove setting:
  * Renamed it into `UseVRStereoViewOnPC`.
  * Add a corresponding `UseVRStereoViewOnPC` property to the FoveManager. That can be used at runtime to change the behavior.
  * Use the `UseVRStereoViewOnPC` FOVE setting only for the initialization of the `FoveManager.UseVRStereoViewOnPC` property value
  * Do not automatically disable cameras associated to `Fove Interface`s at runtime. Instead provide editor helpers that assist you in optimizing your project at edition time.
* Improved the display of the Fove settings menu a little bit
* `Fove/Edit settings` menu now redirects directly to the settings tab of the fove settings windows. The fixes tab can be accessed by the newly added `Fove/Game fixes` menu.
* Rename `PlayerPose.Sitting` into `PlayerPose.Standard` and add comments explaining how the position is impacted

## 4.3.1
* Update FOVE SDK from v1.2.0 to v1.2.1
* Fix stack overflow crash happening when moving from scene having a FoveInterface to a scene without any FoveInterface

## 4.3.0
* Update FOVE SDK from v1.1.0 to v1.2.0
* Add the QueryLicenseInfo API to the `FoveManager`

## 4.2.0
* Rename `FoveInterface.GetEyeVector` into `FoveInterface.GetEyeRay`
* Fix `FoveManager.GetCombinedGazeDepth` calculation. It was using render scale instead of world scale.
* Fix rendering issue when disabling `fetch and sync: Orientation` from the `FoveInterface`.
* Update FOVE SDK from v1.0.2 to v1.1.0
* Fix plugin guide documentation

## 4.1.0
* Update FOVE SDK from v1.0.0 to v1.0.2
* Major update to GazeRecorder script
* Fix lockup when FOVE Compositor is not running
* Various bug fixes regarding the mirror texture
* Remove unnecessary [SerializeField] attribute generating warnings in new version of Unity.
* Fix GetGazeScreenPosition
* Add pupil shape API
* Set world and render scales in the awake function rather than in the constructor as Resource.Load can't be called from constructors

## 4.0.0
* `FoveManager`:
  * Add the `HasHmdAdjustmentGuiTimeout` API
  * Add the `IsEyeTrackingCalibratedForGlasses` API

## 3.2.0
* All Fove APIs now return a `Result` structure containing both the return value as well as an error code.
* Introduce and use the `Stereo` type for all function returning left and right eye data.
* Add the `GazableObject` component and other gaze object detection related APIs
* Fove native Warning/Error logs now appear in the Unity console with suffix `[FOVE]`
* Changed the prefabs camera FOV to match the one of the HMD (this has an impact only when using a custom view for the desktop window).
* Add xml documentation file `FoveClient_CLR.xml`. Documentation for Fove types can now be seen directly from Visual Studio assembly explorer.
* `FoveManager` class:
    * Add new calibration APIs: `StartEyeTrackingCalibration`, `StopEyeTrackingCalibration`, `GetEyeTrackingCalibrationState`, `TickEyeTrackingCalibration`
    * Remove `EnsureEyeTrackingCalibration`, use `StartEyeTrackingCalibration` with calibration `lazy` option instead.
    * Add the `GetGazedObject` function. It returns the registered scene object currently gazed.
    * Add `RegisterCapabilities` and `UnregisterCapabilities` functions to manually register HMD capabilities.
    * Add event callbacks for `HardwareConnected`, `HardwareDisconnected`, `HardwareReady`, `CalibrationStarted`, `CalibrationEnded`, `IsGazeFixatedChange` and `EyesClosedChanged` events.
    * Add yield instructions `WaitForHardwareConnected`, `WaitForHardwareDisconnected`, `WaitForHardwareReady`, `WaitForEyeTrackingCalibrationStart`, `WaitForEyeTrackingCalibrationEnd` and `WaitForEyeTrackingCalibrated`
    * Add the Profile APIs to be able to manage user calibration profiles from the app.
    * Add the `IsUserPresent` function allowing to check if the user is wearing the headset or not
    * Add the `IsHmdAdjustmentGuiVisible` function allowing to check if the headset adjustment is being displayed to the user.
    * Make the `RenderScale` and `WorldScale` properties static
    * Merge the `GetLeftEyeVector` and `GetRightEyeVector` functions into a single `GetEyeVectors` function
    * Merge the `GetLeftEyeOffset` and `GetLeftEyeOffset` functions into a single `GetEyeOffsets` function
* `FoveInterface` class:
    * Add the `registerCameraObject` property to enable or disable automatic camera object registration
    * Add the `RegisterCameraObject`, `UpdateCameraObject` and `RemoveCameraObject` functions for manual camera object management.
    * Add the possibility to change `FoveInterface.TimewarpDisabled` at runtime
    * Remove obsolete `FadingDisabled` and `DistortionDisabled` properties.
    * Remove the `GazeCastPolicy` property and move it to the fove Settings.
    * `GetGazeRays` return type changed from `EyeRays` to `Stere<Ray>`
* `FoveResearch` class:
    * Add the `MirrorTexture` property. It returns the same texture as displayed in the mirror client.
    * Add `GetEyeShapes` function. It return current user eye shapes
    * Optimize capability registration and image data update
* Calibration:
    * Add eye-by-eye calibration (possibility to calibrate each eye separately)
    * Add 1-Point calibration method
    * User application can now render the calibration process

## 3.1.2
* Fix support for custom post effects using the `OnRenderImage` method
* Add support for UI canvas using render mode `Screen space - camera`
* Allow to start the Fove HMD services after the game is started
* Allow to render to the Fove HMD through the OpenVR API and SteamVR compositor
* Add property `IsUsingOpenVR` in the `FoveSettings`
* Add support for registration and unregistration of new capabilities at runtime

## 3.1.1
* Fix the layout of values of the ResearchGaze.
* Fix the stereo projection depth of game objects added under the FoveInterface objects.
* Turn most fields and members of FoveInterface to protected so that the class can be inherited

## 3.1.0

* Rename `Fove::Ray` into `Fove::EyeRay`
* Rename `Fove::Quaternion` into `Fove::Quat`
* FoveUnityUtils: use extension functions for type conversion instead of static functions
* Move internal fields and methods of FoveInterface and FoveManager in a separate '.internal' files for readability
* Move Data type structures outside of the FoveManager and FoveInterface classes to increase readability of the public APIs
* Made FoveInterface's fetchGaze, fetchOrientation, fetchPosition, eyeTargets, poseType, cullLeftMask and cullRightMask public so that they can be queried or changed at runtime
* Fix a memory and processing leak happening every time the game was restarted inside Unity
* Allow to disabled and enable FoveInterface at runtime
* Fix the order of the eyes on the desktop view (right and left were swapped)
* Add an option inside the FoveSettings to allow a custom view on the Desktop (different from the HMD)
* Fix issue with scene loading and unloading (HMD screen was turning black or going back to default space skybox)
* Fix random HMD freezing issue happening when Unity was changing the rasterizing state
* Fix the rotation of the `xx BASE` object generated when `FoveInterface` has no parent in the scene. The rotation of the Fove interface was lost in this case.
* Add the `GetResearchGaze` function to the `ResearchFove` get the research gaze with eye radius information
* Rename and move enumeration `FovePoseToUse` into `FoveInterface::PlayerPose`
* Unify FoveManager and FoveInterface get APIs:
    * Merge synched and out-of-sync query functions by adding an optional `immediate` parameter. Got rid of `_Immediate` functions
    * Rename functions returning HMD coordinate space values `GetHMDxx` instead of using sometimes the term `HMD` and sometimes `Local`
* FoveInterface:
    * Make `PositionToEye` function private as it was never intended to be public (and not really usable as a public function anyway)
    * Make `RenderEye` function internal as users should not directly call this function (it is still accessible though)
    * Remove obsolete and bugged function `CreateGazeRaysFromScreenPoints`
    * Remove obsolete function `Make2DFromVector`. This function was using internal types
    * Add `GazeCastPolicy` field to add more control on how to dismiss gaze cast collisions when the user closes his eyes
* FoveManager:
    * Remove the `GetFVRHeadset` function. Please use the already existing `Headset` property instead
    * Add the `ConnectCompositor` and `DisconnectCompositor` functions
    * Fixed `CheckEyesClosed` function that was always returning out-of-sync values
    * Add immediate version for `IsGazeFixated` and `GetPupilDilatation` functions
    * Add the `GetHMDPose` function
    * Fix the headset data values returned before the connection to the HMD is established
    * Move FoveManager instance to the `DontDestroyOnLoad` scene to avoid screen freezing issues sometime happening during scene loadings

## 3.0.0

* Removed log in case of null gaze vectors (killing the framerate).
* Changed the way Matrix44 is marshalled to remove array allocation at each frame.
* Blit eye rendered texture on main screen instead of re-rendering the screen a third time.
* Fixed graphics-driver crashes by implementing a native-level helper library with fancy callbacks for code that needs to run on the Unity graphics thread.
* Created a FoveManager class which gets automatically added to the scene and lives through scene changes. This is in-part to separate the headset-relative and worldspace-relative interfaces better, but it also helps to keep the native plugin objects alive (see next bullet point).
* Fixed a bug where changing scenes would often result in temporary loss of eye tracking and position tracking and then make you sick by jerking you around as position tracking came back on.
* All the methods which were on the `FoveInterface` but returned HMD-relative data were removed and must be accessed via methods on the static property, `FoveManager.Instance`.
* New, shinier versions of many of these methods have been made which get you the same data relative to the attached GameObject's position in the world.
* The "FOVE Settings" window now has a page for project options and configuration (e.g. adjusting the world scale).
* Resolution oversampling was moved from each `FoveInterface` object to the "FOVE Settings" window.
  * You can modify oversampling real-time by changing the `FoveManager.Instance.ResolutionScale` property.
* World scale is now also global in the "FOVE Settings" window.
  * And it can ALSO be changed dynamically via `FoveManager.Instance.WorldScale`.
* There are no more `FoveInterface` versus `FoveInterface2` choices to make. There's only the one interface.`FoveInterface2` was unstable and we decided that it wasn't worth porting it over to the new system.
* Renamed `GetGazeConvergence_Raw` to `GetGazeConvergence_Immediate`.
* Removed a deprecated methods.
* Revamped the `Gazecast` method family so they're now much more flexible and useful, and you generally have parity with the normal Unity `Raycast` methods for whatever you'd like to do.
* Any `FoveInterface` that doesn't have a parent object will now give itself a parent so it doesn't jump unexpectedly close to the world origin. This makes quick prototyping a scene a bit more convenient; however if you try to attach movement scripts to your `FoveInterface` object, it will still fight with the interface itself (and likely not work -- hence you would still need to add a custom parent for larger-scale movement).
* With love and support from the new `FoveManager`, `FoveInterface` now no longer needs to create extra cameras or use camera prefabs to preserve your effects. You now just use the camera and effects attached to the `FoveInterface` object directly.
  * We removed the `FoveCamera` behaviour as part of this. Everything is much cleaner to use now.
* We optimized the rendering pipeline for `FoveInterface`, so it should offer more stable framerates and a bit better performance for you.
* Also we have reduced the number or submit calls required when using multiple `FoveInterface` instances in a scene (layered rendering, etc...), which will reduce the overhead in some cases.
* Some unused/unwise options were removed from `FoveInterface` (like custom X/Y/Z scaling of eye placement) to keep things cleaner. If you really need to be messing with these values and making your users sick, we encourage you to change the plugin source yourself -- that's why it's there! :)
* Added a field "Eye Targets" to `FoveInterface` that will only render to the specified eyes. Please use this responsibly (or mostly just leave it set to "Both" for most uses).
* Added a handful of `UnityEvent`s you can subscribe to for position, gaze, eye offset, etc... updates in case you need to move objects based on where/when the HMD gets updated.
* `FoveInterface` now lets you specify layers to ignore for left/right eye rendering. That is, if you want an object to only be rendered on one eye and not the other, you can set it to a special layer and assign that layer as ignored/skipped by the other eye. Neat!
* Created an add-in registration system that will give you access to the underlying static `FVRHeadset` object if you need to do any really low-level access to our API (but you probably don't). This is mostly for our own internal and future use, but rather than maintaining two versions, it's there for everyone to... see.
* Removed headset overrides section as we no longer needed it internally and nobody seemed to be using it. If you need to hardcode/override any of the offsets, we encourage you to modify to source of the plugin to achieve whatever change you need.
* Added support for selecting sitting/standing mode. Sitting mode is the same as the previous default behaviour. Standing applies the FOVE camera offset (default to 1m up) to the position. (Offset is configurable via each user's Fove.config file and is not configurable in Unity.)

## 2.1.2

* Fixed GetGazeConvergence methods
* Now uses gaze convergence for all Gazecast queries because convergence is better.
* Fixed a bug where Gazecast might return `false` when, in fact, something WAS hit.

## 2.1.1

* Updated to 0.13.2 FoveClient.dll, which fixes a possible Unity crash.
* Fixed gaze ray origin position on FoveInterface2.
* Fixed order of pose update so submitted frames have their correct pose and timewarp can work properly.
* Added more project suggestions.
* Minor code cleanup and function naming.
* Fixed Unity error when no FOVE Settings file exists
* Added "Refresh" button to FOVE Settings window and made the window less excited about checking for updates on its own

## 2.1.0

* Updated frame submission to use the new client layer acquisition and submission process.
* Plugged up some places we were accidentally calling C API bindings unnecessarily frequently.
* More reliably report eye gaze in unusual cases like when eye tracking is disabled. (Defaults to the user's "forward" from each eye.)
* All fix suggestions are handled by a "FOVE Settings" window, accessible from the "FOVE" menu we put at the top of the Unity Editor. This is the beginning of what we hope to be a simpler system for selecting how you want to use FOVE inside Unity, so keep your eyes here for more information. :)
* `FoveInterface` and `FoveInterface2` have been cleaned up to run more consistently well.
* The plugin is now mostly a bunch of C# files rather than a precompiled binary. So you can explore and hack around and customize to your pleasure.
* `FoveInterface2` has been downgraded to "experimental". It works better now than before, but we have still seen some rare bugs that make us want to pull back a bit on declaring it definitively "the best" interface. We recomment you evaluate the pros/cons of each based on your project.

## 2.0.0

* Switched plugin to using the official C# plugin library. Included the official C# library.
* Added a new interface class (`FoveInterface2`) which takes advantage of Unity's built-in stereoscopic rendering optimizations. You should use this moving forward in order to help make cross-API camera systems and to get some performance improvements over the older system. This should also make it a bit more intuitive to use camera effects.
* Added a warning when your build settings aren't for Windows 64-bit. (That's the only build we support for now -- this will update as we add more platforms.) There's a button you can press to automatically switch your build setting to x86_64.
* More settings are modifiable live while playing in-editor so you can get a better sense of what your changes will look like without having to stop and restart all the time.
* Clamped inter-ocular distance and world scale to be at least zero -- no negative scales or swapped eyes, please.
* Limited the overdraw ratio to [0.01 - 2.5]. No more negative texture sizes for you!
* Actually checking the preferences to tell if you want to be nagged about vsync performance problems. Sorry about that.
* Detect when another FoveInterface is attached to the eye camera prototype, emit errors on launch, and disable the interface. This is an error state.
* Updated the user guide
* Fixed deluge of errors when no HMD is attached and/or the runtime is disabled.

## 1.3.1

* Added a check for Unity versions >= 5.6, where they fixed an anti-aliasing screen blit texture inversion; so if you're using the latest version of Unity, you shouldn't get upside-down images in the game view any more.

## 1.3.0

* Added various feedback to let you know if vsync is on when it shouldn't be
  * Red alert text in the `FoveInterface` inspector window tells you if vsync is currently enabled.
  * A popup alert box tells you when you launch the editor if vsync is enabled on any of your quality settings levels, and it will disable vsync for you if you let it.
  * Added a preferences option to toggle the vsync check warning (defaults to enabled, because this is actually important if you want to get decent performance out of Unity with FOVE).
* Made the constructor private so that people who haven't read the docs don't try to instantiate `FoveInterface` and have it compile -- because that definitely won't work without being properly attached to a `GameObject` in the scene.
* Added ability to disable Fove's compositor from the FoveInterface's inspector. This would be useful if you want to use (e.g.) SteamVR but also take advantage of eye tracking.

## 1.2.0

* Removed requirement for only a single FoveInterface per scene. This is to allow overlays and possibly other complex effects. Please use this power responsibly.
* Added static methods for immediate access to data:
  * `GetHMDRotation_Immediate`
  * `GetHMDPosition_Immediate`
  * `Get[Left/Right]EyeVector_Immediate` (provides the HMD-relative eye vector; does not account for HMD orientation)
* Added method family `Gazecast` to FoveInterface instances. You should use this instead of the static `IsLookingAtCollider` method which is now obsolete.
* Moved a number of methods to be instance methods on FoveInterface rather than static. The old static methods have been marked obsolete and you should migrate away from using them as soon as possible as follows (original static on the left, new instance method on right):
  * ~~GetEyeRays~~ -> GetGazeRays
  * ~~Get(Left/Right)EyeCamera()~~ -> GetEyeCamera(EFVR_Eye whichEye)
  * ~~IsLookingAtCollider~~ -> Gazecast [there are a few overloads of this one]
  * ~~GetNormalizedViewportPosition~~ -> GetNormalizedViewportPointForEye
* Fixed an issue where the static method `GetGazeConvergence()` was returning world-space coordinates rather than HMD-relative coordinates. If you want world-space convergence, use the FoveInterface instance method `GetWorldGazeConvergence()`.
* Added `GetWorldGazeConvergence()` to the non-static FoveInterface interface.
* Fixed an issue that was causing awful, awful lag when using MSAA inside the Unity plugin.
* `FoveInterface` now defaults to the Quality Setting state when choosing anti-aliasing for their render textures (unless you override).

## 1.1.1

* Removed the readme from the unitypackage

## 1.1.0

* Exposed a checkbox for telling the compositor not to apply time warp to the submitted frames, in effect keeping them stationary in the HMD viewport
* Cleaned up  the Inspector for FoveInterface to better represent what's going on
* Elements that cannot be expected to work properly when updated live are disabled when in play mode
* Added methods to support SDK 0.11's new EnsureEyeTrackingCalibration functionality
  * public static bool EnsureEyeTrackingCalibration()
  * public static bool IsEyeTrackingCalibrating()
* Updated FoveClient.dll to 0.11.3
* Replaced zip file readme and guide with this document as part of the Unity package

## 1.0.0

* Consolidated Fove Interface prefab
* New Fove Rig prefab
* Added EyeShaderInverted to correct Unity camera preview
* Updated FoveClient.dll (v0.10.0)
* Added: public static bool IsHardwareConnected()
* Added: public static bool IsHardwareReady()
* Added: public static bool IsCalibrated()
* Added: public static bool CheckSoftwareVersions(out string error)
* Added: public static string GetClientVersion()
* Added: public static string GetRuntimeVersion()
