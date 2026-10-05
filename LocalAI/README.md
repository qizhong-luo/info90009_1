# Offline LLM for Windows

Double-click `Builds/Sleepet-LocalAI-Windows/Sleepet.exe`. Keep its entire folder together. The package includes Qwen3 1.7B Q4_K_M, llama.cpp CPU inference, and the app-local Microsoft Visual C++ runtime. No terminal, Node, model installation, Internet connection, or API key is required for Offline LLM. Windows 10/11 x64 is the target; allow approximately 2 GB of disk space and several GB of available RAM.

Enter the AR screen and choose **Chat with the Sleepet** to chat directly. **Meet the Sleepet in AR** is a separate placement action; Test background is available if there is no camera. Offline LLM is the default; the model loads on the first request. The provider switch selects optional OpenAI, which still requires a configured backend and Internet access. A missing or broken local model produces a retryable error, never a fabricated answer or silent cloud fallback.

## Application skills

- Daily companionship: brief conversation and, when enabled, saved preferences.
- Record review: the last seven calendar days of app sessions, excluding samples from real totals. These records are not measured sleep.
- Tomorrow preparation: the saved plan must be dated for tomorrow; missing or outdated plans are not reused.
- Thoughts: AR tap reactions and random 2–10 minute foreground idle reactions. Tap permits six themes; idle permits only playful daydreams, stretching and quiet companionship. The original black/white Border Collie uses six matching animations; customized pets stay static. Thoughts do not enter chat history.

The chat surface has one local/online model switch and no manual skill selector. The versioned presets live in `Assets/Sleepet/Resources/CompanionSkills`. These are application skills, not Codex plugins. The app executes scoped reads, computes record totals, and passes bounded results to the model. The model cannot write settings or read arbitrary files. Saved data is enabled by default and has no user-facing toggle. The application chooses the relevant skill automatically from English or Chinese message content, using the previous skill for short follow-up questions. Automatic topic changes preserve the conversation. Changing model provider clears conversation context. In Offline LLM the prompt and data remain on the device; in OpenAI mode enabled data is sent to the configured backend.

Windows saves user data under `Application.persistentDataPath/SleepetData` and logs under `Application.persistentDataPath/SessionLogs`, so installation folders can be read-only. Existing editor data remains in the project directory. Earlier standalone saves beside the executable are not migrated automatically.

## Building

For one-command download, build and ZIP packaging, run `Tools/BuildCompleteGame.ps1`; see [the build guide](../Tools/BUILD_GAME.md). Alternatively, run `Tools/PrepareLocalAI.ps1` once. It verifies the pinned model/runtime SHA-256 values in `manifest.json` and copies the Visual Studio redistributable x64 CRT. Unity menu: **Sleepet > Build Windows With Local AI**. Only the developer needs Unity and the preparation script. Model binaries and build outputs are ignored by Git; licenses, manifest and preparation code are tracked.

The app starts a hidden child process listening only on loopback, with a random per-launch access token, `--offline`, no web UI and CPU inference. A Windows Job Object owns the child process and closes it when the application exits. Requests can be cancelled. No prompt or saved-data payload is written to runtime logs.

## Validation

`Sleepet.exe --local-ai-smoke <output-directory>` verifies standalone model loading and real generation. `Sleepet.exe --local-ai-skills <output-directory>` uses isolated synthetic saved data, drives AR chat, checks skill responses and writes screenshots. These flags are developer verification aids; ordinary users do not need them. See `Validation/CHAT_UI_REPORT.md` for the latest interface/routing verification and `Validation/LOCAL_AI_REPORT.md` for the initial runtime validation.

Small local models can still produce inaccurate wording. This version is a companion, not a medical assistant. CPU speed and first-load latency vary by device. This package has been tested on the development Windows machine; a clean Windows VM test is still needed for broader distribution.
