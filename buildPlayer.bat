@echo off

set UNITY_CI_SERIAL=%1
set UNITY_CI_USERNAME=%2
set UNITY_CI_PASSWORD=%3
set DEPLOY_PATH=%4

set PATH=%PATH%;C:\Program Files\Unity\Editor

Unity.exe -quit -batchmode -serial %UNITY_CI_SERIAL% -username %UNITY_CI_USERNAME% -password %UNITY_CI_PASSWORD% -projectPath "%cd%" -buildWindows64Player "%DEPLOY_PATH%"

if errorlevel 1 (
   echo Failed to build the plugin test player
   echo Detailed errors:
   type %LOCALAPPDATA%\Unity\Editor\Editor.log
   exit /b %errorlevel%
)