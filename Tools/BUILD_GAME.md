# Download the local model and build the complete game

**Double-click `BuildGame.cmd` in the Unity project root.** No command-line input is needed. It downloads dependencies, builds the game and creates the complete ZIP, then opens the output folder. The progress window remains open so success or failure can be read. Close Unity before starting.

For terminal users, the equivalent command is:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\BuildCompleteGame.ps1
```

`ExecutionPolicy Bypass` applies only to this command's PowerShell process; it does not change the machine's policy. The script also works in PowerShell 7. The build script does not require Python, Node or an API key. The source repository uses Git LFS for binary assets: install Git LFS and run `git lfs pull` in your checkout before building. The script does not download missing LFS assets for you.

## Build-machine prerequisites

- Unity matching `ProjectSettings/ProjectVersion.txt` (currently 6000.3.21f1), activated through Unity Hub.
- Windows Build Support for that editor. Close this project in the Unity Editor before building.
- The x64 Microsoft Visual C++ redistributable CRT files supplied with Visual Studio's C++ build tools. These DLLs are copied beside the native model runtime, so players need no separate installation.
- Internet access for the first model/runtime download, plus several GB of free disk space for the model, build, ZIP and Unity caches.

If Unity or the redistributable CRT is installed in a custom location:

```powershell
.\Tools\BuildCompleteGame.ps1 `
  -UnityPath 'D:\unity\6000.3.21f1\Editor\Unity.exe' `
  -CrtPath 'C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Redist\MSVC\14.44.35112\x64\Microsoft.VC143.CRT'
```

Both arguments are optional. `UNITY_EDITOR_PATH` is also supported. There are no user-specific absolute paths in the script.

## Automatic steps

1. Check the editor version and required build dependencies.
2. Run `PrepareLocalAI.ps1`: download the pinned llama.cpp runtime and Qwen3 1.7B quantized model when absent; verify SHA-256 before using either; unpack the runtime, collect license notices and copy the CRT DLLs. Existing valid downloads are reused. A failed download or checksum aborts the build.
3. Run Unity's `Sleepet.Editor.SleepetWindowsBuild.Build` in batch mode, wait for completion, and require both a successful exit code and the build success marker.
4. Verify the executable, Unity data, native libraries, licenses and model checksum in the generated game folder.
5. Create a ZIP, verify the model checksum again through the ZIP stream, and write the ZIP's SHA-256 sidecar.

Output is a unique `Builds/Sleepet-Windows-<timestamp>-<id>/` directory, a matching `.zip`, and `.zip.sha256`. Build logs are under `Logs/`. Existing builds are not deleted or overwritten. Failed attempts keep their logs and intermediate files for diagnosis.

Players extract the entire ZIP and double-click `Sleepet.exe`. The complete package includes the model and native inference runtime. Local chat is available without Internet, a terminal, Node or an API key. The optional online provider still needs its separately configured backend.

## Git

Commit `BuildGame.cmd`, all PowerShell scripts in `Tools`, this guide, `Assets/Sleepet/Editor/SleepetWindowsBuild.cs` and its `.meta`, all other Unity source/assets, and the `LocalAI` manifest/license documentation. The normal project commit should include these files.

`LocalAI/models`, `LocalAI/runtime`, `LocalAI/downloads`, `Builds`, and `Logs` remain ignored. Cloning the source and running this script recreates those dependencies and the complete distributable. Do not force-add model binaries or generated ZIPs to ordinary Git.
