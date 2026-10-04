# Sleepet Unity

Sleepet is a portrait companion app prototype built with Unity **6000.3.21f1**. It combines a customizable pet, bedtime planning, sleep-session tracking, morning routines, reports, camera-based pet placement, and local AI conversation.

This README describes the current source as reviewed on **5 October 2026**. Start from `Assets/Sleepet/Scenes/Sleepet_Home.unity`. The reference portrait layout is 402 x 874; the Windows player also supports a centered portrait presentation.

## Get started

### Build from a Git checkout

1. Install Git LFS before cloning. This repository uses LFS for images, audio, video, fonts, and other binary assets. In an existing checkout, run `git lfs install` and `git lfs pull` to retrieve the actual assets. LFS pointer files alone are not sufficient to build the complete app.
2. Install and activate Unity **6000.3.21f1** through Unity Hub, with **Windows Build Support**.
3. Install the x64 Microsoft Visual C++ redistributable CRT files supplied with Visual Studio C++ Build Tools. The packaging script copies these DLLs into the game; installing only a player-side redistributable does not necessarily provide the directory the script expects.
4. Allow Internet access for the first Unity package/model/runtime downloads and several GB of free space. Close this project in Unity before building.
5. Double-click **`BuildGame.cmd`** in this directory. The script downloads and verifies the AI dependencies, builds the Windows x64 player, packages it, and opens the output directory after success.

From Command Prompt, the same launcher is:

```bat
BuildGame.cmd
```

To check launcher dependencies and PowerShell script syntax without building:

```bat
BuildGame.cmd --check
```

This check does not verify Unity installation, download availability, successful compilation, or gameplay.

For custom Unity/CRT locations, use the PowerShell parameters documented in [the build guide](Tools/BUILD_GAME.md). `UNITY_EDITOR_PATH` is also supported. Python, Node.js, Codex, Codex skills, and an API key are not required for the offline Windows build. Git LFS is needed to materialize this repository's assets, even though the build script itself does not invoke it.

### Run the packaged game

Build output is `Builds/Sleepet-Windows-<timestamp>-<id>/`, with a matching `.zip` and `.zip.sha256`. Logs are written to `Logs/`. Existing packages are not overwritten.

Extract the **entire ZIP** and double-click **`Sleepet.exe`**. Keep the executable, Unity data, and `LocalAI` folder together. Players do not need Unity, Node.js, a terminal, an API key, or Internet access for local conversation. The first reply takes longer while the model loads. The bundled inference runtime targets Windows x64; this is not an iOS package.

Rebuild after pulling source changes. Existing executables and ZIPs do not update when Git source files change.

## Implemented features

### Onboarding and companion customization

- Animated splash, account form, companion selection, personalization, creation transition, and meet-your-pet screens.
- Companion name, species, breed, colour, and supported photo selection/preview, with saved companion preferences shared across the app.
- Readable breed/colour dropdowns, selection indicators, and validation feedback.
- The account form is a local prototype flow, not a production authentication service. It validates input and clears the password field; it does not establish a server-authenticated account.

### Home, settings, and bedtime planning

- Home navigation to sleep, tomorrow's plan, reports, settings, and AR/chat; tapping the visible pet produces scale/sway feedback that returns to its resting state.
- Editable profile/avatar, companion settings, wake/bed times, wind-down duration, sound, playback behaviour, and volume. Settings persist locally and refresh across scenes.
- Time and pet dialogs support validation, preview, saving, and cancellation. Profile names use the existing English-letter and length restrictions.
- Tomorrow's Todo supports routine options, a separate date editor with valid-date checks, multiple custom events, selection, and individual removal. Older single-event saves remain compatible.
- Routine confirmation appears only after the save succeeds. Morning reminders and AI context consume the updated event list.

### Sleep and morning routines

- Start and end an app sleep session, retain it while navigating, adjust the alarm/time controls, and select sleep audio and playback behaviour.
- Audio changes avoid repeatedly resetting the clip; changing from Silence to a sound during an active session resumes playback.
- Wake feedback, a five-level morning rating, water completion/skipping, a timed stretch step with skipping, planned todos, and a morning completion screen.
- Morning steps follow the saved plan. Rating, water/stretch status, the notify-later selection, and completion state are saved with the session.
- The notify-later circle has a working click target and checkmark. Saving this preference does not by itself implement an operating-system notification scheduler.
- Morning completion requires an upward swipe of at least 60 UI units with greater vertical than horizontal travel. Taps, short swipes, and horizontal swipes do not finish the flow.
- Animated/video visuals, transitions, and companion-dependent presentation support the onboarding and morning screens.

### Daily and weekly reports

- Daily report, weekly overview, and a separate historical day-detail page; the bottom moon navigation opens the weekly report.
- Seven date controls open the selected day's saved report or its empty state. Weekly statistics and duration bars are driven by saved records.
- The weekly reference line averages the displayed duration-bar heights for dates with records and is hidden when there is no data.
- `TEST +` adds an explicitly marked, repeatable seven-day sample set without duplicating it or replacing real records. Sample Deep/Light/REM values remain identified as fixed test data.
- Real app sessions are not sensor-measured sleep. Missing sleep-stage data is disclosed instead of being fabricated.
- A health-app entry provides a configured external-launch interface and an explicit unavailable state. HealthKit integration and a working device-specific health-app target are not established by this prototype.

### Independent AR placement and AI chat

- **Meet the Sleepet in AR** opens pet placement; **Chat with the Sleepet** opens conversation directly without requiring placement or a working camera.
- Opening chat closes the camera. Placement uses the saved companion settings, a camera feed or a clearly marked test background, and draggable 2D pet presentation.
- AR currently means a 2D overlay on camera imagery, not ARKit plane detection or spatial anchoring.
- Chat includes a scrolling transcript, a white rounded multiline input, an inset send button, request/error states, cancellation, and retry.
- English and Chinese intent rules automatically select an application skill. Short follow-up questions retain the previous skill, while topic changes preserve the visible conversation.
- Saved-data access is enabled by default and remains scoped and read-only. The visible setting is the local/online model switch; there are no manual skill tabs or saved-data toggle.
- Settings changes invalidate stale model context while retaining the visible transcript. Updated pet names and wake times are available without restarting the model process.
- Updated prompts distinguish the pet's identity from the user's identity and discourage repetitive generic service endings. These changes improve the verified cases but cannot guarantee every generated reply.

## AI skills and model

The three application skill presets are ordinary version-controlled JSON resources in [Assets/Sleepet/Resources/CompanionSkills](Assets/Sleepet/Resources/CompanionSkills):

| Skill | Data scope and behaviour |
| --- | --- |
| `daily_companionship` | Brief companionship using permitted saved preferences. |
| `record_review` | Recent app-session history from the last seven local calendar days; samples are excluded from real totals. |
| `tomorrow_preparation` | Saved preferences and a plan dated for tomorrow; missing or expired plans are not reused. |

The presets, their `.meta` files, routing code, and prompt-building code are tracked by Git. Unity loads the presets from Resources and includes them in the player. The online backend also reads the canonical presets; restart it after changing them.

Offline inference uses **Qwen3 1.7B Q4_K_M** with **llama.cpp b11146 Windows x64 CPU**. [LocalAI/manifest.json](LocalAI/manifest.json) records the expected model/runtime checksums and licensing information. Preparation verifies the downloads, and packaging verifies the model again in the output and ZIP. A missing or invalid local model produces an error rather than silently switching to a cloud service.

The model receives bounded, read-only snapshots. It cannot arbitrarily read files or change settings. In local mode, inference stays on the device. Online mode sends chat and permitted saved information to the configured backend and relevant model service.

For optional online conversation, configure the backend with Node.js 22+, `OPENAI_API_KEY`, and `OPENAI_MODEL`, then select Online model in chat. See [Backend/README.md](Backend/README.md) for setup and the optional integration fixture. Credentials are not supplied by cloning the repository. Live OpenAI generation is not covered by the recorded local-model checks.

## What Git shares and what build consistency means

| Content | Shared through this repository? |
| --- | --- |
| Unity scenes, scripts, resources, `.meta` files, settings, and package lockfile | Yes, after committing and pushing; binary assets require LFS retrieval. |
| The three game AI skills and their routing/prompt code | Yes; they are built into the game. |
| `BuildGame.cmd`, PowerShell build scripts, model manifest, and license documents | Yes. |
| Downloaded model/runtime files | No; `LocalAI/models`, `LocalAI/runtime`, and `LocalAI/downloads` are ignored and recreated by preparation. |
| Generated builds, ZIPs, Unity caches, and build logs | No; these are local build products. |
| Personal saves, session logs, backend `.env` files, and API credentials | Not part of the normal source checkout. |
| Codex skills/plugins installed in a developer's user directory | No; they are development-assistant tools outside this repository and are not needed by the game build. |

Two developers using the **same committed revision**, complete LFS assets, the required Unity version, and verified model/runtime downloads should build the same game features, skill definitions, and model configuration. Local edits must be committed and pushed before another developer can retrieve them.

This is **functional consistency**, not a guarantee of byte-identical builds or identical AI replies. Build timestamps and identifiers vary; locally sourced CRT DLL versions and build environments can differ. Saves, preferences, credentials, hardware performance, and generated conversation can also differ. To distribute the exact same compiled artifact, share one complete verified ZIP and its SHA-256 sidecar.

A successful local build or `--check` run does not prove that a fresh machine is fully configured. A clean Windows-machine/VM build and run remains a separate compatibility check.

## Scene layout and editing

There are currently **20 enabled scenes** in [EditorBuildSettings.asset](ProjectSettings/EditorBuildSettings.asset), including the original eight pages and twelve onboarding/morning pages. All are under `Assets/Sleepet/Scenes/` and use the `Sleepet_` prefix and `.unity` extension.

| Group | Scene names |
| --- | --- |
| Main pages | `Home`, `Sleep`, `Me`, `Routine`, `Daily`, `Weekly`, `AR`, `DayDetail` |
| Onboarding | `OnboardingSplash`, `Account`, `ChooseCompanion`, `PersonalisePet`, `CreatingPet`, `MeetPet` |
| Morning | `WakeFeedback`, `MorningRating`, `MorningDrink`, `MorningStretch`, `MorningTodos`, `MorningEnd` |

Page layouts are serialized uGUI objects that can be edited in the Scene view or Inspector. Runtime code binds data and interaction and may extend repeated content such as todo rows. `SleepetApp.prefab` holds the non-rendering application core; `SleepetSceneSession` preserves shared data and active session state across navigation. `SceneTransitionCount` and `StateRevision` track navigation and committed state changes.

Editor maintenance menus can reapply authored layouts or interaction fixes. They are not required for ordinary Inspector edits and may overwrite manual layout adjustments if deliberately rerun.

Windows player data lives under `Application.persistentDataPath/SleepetData`, with session logs under `Application.persistentDataPath/SessionLogs`. Editor data uses the project-local data mechanism. Earlier standalone saves next to the executable are not automatically migrated.

## Recorded validation and remaining limits

These are existing validation results, not tests rerun for this README update:

| Area | Recorded result | Evidence |
| --- | --- | --- |
| Onboarding/morning visual and interaction regression | 28 passed, 0 failed | [Flow results](Validation/Flow/web-parity-final.xml) |
| Scene interaction fixes | 7 targeted tests passed; preceding regression recorded 32 passed and 1 optional online test skipped | [Final results](Validation/scene-issues-final-results.xml), [earlier results](Validation/scene-issues-results.xml) |
| AI against all 20 scenes | Windows build/package verification; 8 real local-model scenarios; 19 regression tests passed and 1 skipped | [Current-scenes report](Validation/CURRENT_SCENES_AI_REPORT.md) |
| Pet identity and reply quality | 10 real local-model scenarios; 17 Unity tests passed and 1 skipped; 9 mocked backend tests passed | [Reply-quality report](Validation/REPLY_QUALITY_REPORT.md) |
| Live name/wake-time refresh | Settings changed through the UI and reflected in generated answers without restarting the model | [Settings-refresh report](Validation/SETTINGS_REFRESH_REPORT.md) |

Some detailed evidence lives in ignored `Validation/LocalAI` directories and is only available on the development machine. Committed reports describe those runs. Older reports refer to earlier eight-scene layouts or earlier AI behaviour; use the current source and this overview for current capabilities.

The packaged local-model runs used isolated synthetic saves on the development Windows machine. Physical network disconnection, clean-machine compatibility, live online inference, mobile camera permissions, mobile keyboard/safe-area behaviour, HealthKit, native notifications, and real spatial AR require separate validation. The small local model can still produce awkward or inaccurate wording. Sleepet records app activity and is not a medical sleep-measurement system.

## Further documentation

- [Windows build prerequisites and options](Tools/BUILD_GAME.md)
- [Local model packaging and runtime](LocalAI/README.md)
- [Optional online backend](Backend/README.md)
- [Automatic skill routing and independent chat](Validation/CHAT_UI_REPORT.md)
- [Latest scene interaction fixes](Validation/SceneIssues/README.md)
- [Earlier design implementation notes](HIGH_FI_IMPLEMENTATION.md)
