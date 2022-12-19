@echo off

set UNITY_CI_SERIAL=%1
set UNITY_CI_USERNAME=%2
set UNITY_CI_PASSWORD=%3

Unity.exe -quit -batchmode -serial %UNITY_CI_SERIAL% -username %UNITY_CI_USERNAME% -password %UNITY_CI_PASSWORD% -projectPath "%cd%" -exportPackage "Assets/FoveUnityPlugin" "Fove_Unity_Plugin.unitypackage"

if errorlevel 1 (
   echo Failed to build the plugin package
   echo Detailed errors:
   type %LOCALAPPDATA%\Unity\Editor\Editor.log
   exit /b %errorlevel%
)