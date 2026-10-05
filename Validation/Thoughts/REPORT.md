# AR Thoughts validation — 2026-10-05

## Delivered

- Tap pool: bark, licking1, licking2, itching, stretching, sitting.
- Foreground AR idle pool: itching, stretching, sitting, with a freshly sampled 120–600 second delay.
- Themes remain fixed per motion in Resources/CompanionSkills/thoughts.json.
- Only the original black/white Border Collie animates. Alternate appearance, species, breed, colour or photo uses the selected static sprite.
- The visible AR implementation does not load or activate the deprecated Golden Retriever animations.
- Six transparent GIFs and 36 corresponding 256-pixel PNG frames. All frames inspected for full-character bounds; tails retained through connected-component extraction.
- Pale blue translucent bubble with plain-text output, fade in/out and heading avoidance. Latest screenshot: ../AI/AR-thought.png.
- Pending thoughts cancel on exit, focus loss, dragging, chat or provider change. Thoughts do not enter the chat transcript.
- Invalid, duplicate, labelled, overlong or mixed-language outputs are not displayed. Local Thoughts also constrain decoder output to printable English-UI characters; ordinary chat remains unconstrained.

## Verification

- Packaged-player follow-up: the affected save contained petOption=0 / petAppearance=0 / Border Collie / Brown. The animation guard correctly rejected it, but the preset sprite renderer still displayed the original Collie. Explicit preset selection now synchronizes breed/colour/photo metadata; name-only or pose-only edits preserve custom metadata. The diagnosed user's save was backed up before correcting Brown to Black & white.
- Preset regression: **4 passed, 0 failed** (../pet-preset-final.xml), covering explicit reselection, preservation of custom settings on ordinary save, persistence, Home/AR frame playback, real UI pointer gestures and static custom pets. A fresh Windows build is supplied in Builds/Sleepet-PetAnimation-Fixed.

- AR tap/drag and Home follow-up: **4 passed, 0 failed** (../pet-input-final.xml). Synthetic mouse state is processed through the scene's actual Input System UI module and raycasts. Batch-only input focus settings are restored by test teardown.
- AR: a 0.45-second press with 8-pixel movement triggers exactly one thought; a 60-pixel drag moves the pet without another thought; the ambient scheduler resumes after drag completion. Focus loss clears a held gesture.
- Home: the existing pet button plays one of the same six Border Collie frame sequences, restores the original sprite, leaves its transform unchanged, and sends zero AI requests. Customized appearances remain static. Repeated taps restart playback; appearance refresh cancels playback before assigning the new sprite.
- The earlier input-only update did not rebuild the Windows executable; the preset follow-up above supplies a new build.

- Unity final run: **23 passed, 0 failed, 1 skipped** (../thoughts-final-tests.xml).
- Skipped test: online client roundtrip requiring SLEEPET_AI_TEST_URL and the integration fixture. This is not a verified live cloud-model run.
- All six themes exercised against bundled Qwen3 1.7B; actual results in local-model-responses.txt.
- Scene test verifies actual AR placement, frame selection, bubble display, independent chat history, ambient scope, cancellation when opening chat, and static customized-pet rendering.
- Backend Node suite: **10 passed, 0 failed**, including thought action/trigger validation, data scoping and HTTP fixture integration.
- GIF packaging verified six distinct clips, six frames each, 5 fps, transparency, matching Unity PNGs.

Source and Unity-imported assets are complete. Older Windows distributions are retained; use Builds/Sleepet-PetAnimation-Fixed for the preset follow-up.

Earlier thoughts-tests.xml/log files record intermediate failures that were corrected; thoughts-final-tests.xml is the final result.
