## FOVE Unity Plugin Changelog

### 2.1.2
* Fixed GetGazeConvergence methods
* Now uses gaze convergence for all Gazecast queries because convergence is better.
* Fixed a bug where Gazecast might return `false` when, in fact, something WAS hit.

### 2.1.1
* Updated to 0.13.2 FoveClient.dll, which fixes a possible Unity crash.
* Fixed gaze ray origin position on FoveInterface2.
* Fixed order of pose update so submitted frames have their correct pose and timewarp can work properly.
* Added more project suggestions.
* Minor code cleanup and function naming.
* Fixed Unity error when no FOVE Settings file exists
* Added "Refresh" button to FOVE Settings window and made the window less excited about checking for updates on its own

### 2.1.0
* Updated frame submission to use the new client layer acquisition and submission process.
* Plugged up some places we were accidentally calling C API bindings unnecessarily frequently.
* More reliably report eye gaze in unusual cases like when eye tracking is disabled. (Defaults to the user's "forward" from each eye.)
* All fix suggestions are handled by a "FOVE Settings" window, accessible from the "FOVE" menu we put at the top of the Unity Editor. This is the beginning of what we hope to be a simpler system for selecting how you want to use FOVE inside Unity, so keep your eyes here for more information. :)
* `FoveInterface` and `FoveInterface2` have been cleaned up to run more consistently well.
* The plugin is now mostly a bunch of C# files rather than a precompiled binary. So you can explore and hack around and customize to your pleasure.
* `FoveInterface2` has been downgraded to "experimental". It works better now than before, but we have still seen some rare bugs that make us want to pull back a bit on declaring it definitively "the best" interface. We recomment you evaluate the pros/cons of each based on your project.

### 2.0.0

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

### 1.3.1

* Added a check for Unity versions >= 5.6, where they fixed an anti-aliasing screen blit texture inversion; so if you're using the latest version of Unity, you shouldn't get upside-down images in the game view any more.

### 1.3.0
* Added various feedback to let you know if vsync is on when it shouldn't be
  * Red alert text in the `FoveInterface` inspector window tells you if vsync is currently enabled.
  * A popup alert box tells you when you launch the editor if vsync is enabled on any of your quality settings levels, and it will disable vsync for you if you let it.
  * Added a preferences option to toggle the vsync check warning (defaults to enabled, because this is actually important if you want to get decent performance out of Unity with FOVE).
* Made the constructor private so that people who haven't read the docs don't try to instantiate `FoveInterface` and have it compile -- because that definitely won't work without being properly attached to a `GameObject` in the scene.
* Added ability to disable Fove's compositor from the FoveInterface's inspector. This would be useful if you want to use (e.g.) SteamVR but also take advantage of eye tracking.

### 1.2.0
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

### 1.1.1
* Removed the readme from the unitypackage

### 1.1.0
* Exposed a checkbox for telling the compositor not to apply time warp to the submitted frames, in effect keeping them stationary in the HMD viewport
* Cleaned up  the Inspector for FoveInterface to better represent what's going on
* Elements that cannot be expected to work properly when updated live are disabled when in play mode
* Added methods to support SDK 0.11's new EnsureEyeTrackingCalibration functionality
  * public static bool EnsureEyeTrackingCalibration()
  * public static bool IsEyeTrackingCalibrating()
* Updated FoveClient.dll to 0.11.3
* Replaced zip file readme and guide with this document as part of the Unity package

### 1.0.0
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