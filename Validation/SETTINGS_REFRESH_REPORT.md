# Live settings refresh verification — 4 October 2026

The local system prompt incorrectly fixed the companion's identity to Mocha. It now reads the current permitted pet name on every request. Settings changes invalidate model conversation context across all skills while retaining the visible transcript.

Verified in `Builds/Sleepet-Windows-20261004-003658-42cbed/Sleepet.exe` using isolated synthetic data:

- Started a conversation as Mocha, then navigated to the actual settings UI.
- Renamed the pet to Miia and changed the wake time from 07:35 to 08:23 using the settings controls.
- Returned to chat without restarting the application or the model process.
- Actual generated introduction: “Hi, I'm Miia.”
- Actual generated settings answer: “Your current saved wake time is 08:23.”
- All prior skill, expired-plan and cancellation scenarios also passed.

Evidence: `LocalAI/settings-refresh/results.txt`, `07-renamed-pet.png`, and `08-updated-wake-time.png` in that same evidence directory. The renamed-pet screenshot was visually inspected.

Regression suite: 17 passed, 0 failed, 1 optional online fixture test skipped. It includes current-name prompting for all three skills and invalidating old context after a rename followed by a different skill. Results: `LocalAI/settings-refresh-tests.xml`.

The package was produced through Windows PowerShell 5.1 using the automated build script. Its model-in-ZIP checksum passed during packaging; the final ZIP SHA-256 sidecar was independently checked. Existing installed binaries must be replaced with this build, or rebuilt from the updated Unity project.
