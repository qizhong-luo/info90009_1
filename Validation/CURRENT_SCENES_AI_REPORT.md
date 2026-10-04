# AI verification against the updated scenes

Date: 2026-10-04. Unity: 6000.3.21f1, Windows x64.

## Outcome

The current project, including all 20 enabled scenes, built successfully. The portable ZIP passed executable/runtime presence checks and SHA-256 verification of the bundled model. No scene or application source changes were needed for this verification. Existing user changes were preserved.

- Build: `Builds/Sleepet-Windows-20261004-223249-341bd3/Sleepet.exe`
- Complete package: `Builds/Sleepet-Windows-20261004-223249-341bd3.zip`
- ZIP SHA-256: `a5bff7672be8c9c567f8100d4317e973dece68cc01869bb16225f4fb0f86b823`
- Evidence: `Validation/LocalAI/current-scenes/`

## Real local-model conversations

The packaged game used its bundled Qwen3-1.7B model and native runtime, with an isolated synthetic save directory. No API key or external model service was used. The machine's network adapter was not disabled; this verifies local inference, rather than a physical network-disconnection test.

All eight scripted scenarios met their routing and data assertions:

| Scenario | Observed response / behavior | Reply time |
| --- | --- | --- |
| Greeting | Introduced itself as Mocha | 7.11 s |
| Preferences | Reported saved wake time 07:35 | 1.67 s |
| Record review | Reported 1 real app session, 10 minutes; excluded the sample | 5.53 s |
| Tomorrow preparation | Listed all five saved activities, including Visit the library | 6.10 s |
| Expired tomorrow plan | Reported no valid saved plan; did not reuse the library activity | 2.21 s |
| Companionship | Produced a supportive response; see quality issue below | 2.09 s |
| Rename through settings UI | Introduced itself as Miia in the answer body | 5.69 s |
| Wake-time update through settings UI | Reported the updated 08:23 | 2.29 s |

The name and time updates worked without restarting the model process. Cancellation stopped the request and retained retry availability. Full verbatim answers are in `LocalAI/current-scenes/results.txt`.

## Display and interaction

Inspected rendered screenshots at the 804 x 1748 reference-phone resolution. The title, pet, model switch, transcript, message input, send button and close button are visible without overlapping. Longer transcripts scroll within their viewport. The separate buttons read “Meet the Sleepet in AR” and “Chat with the Sleepet”. Placement did not open chat; opening chat closed the camera. Saved data was enabled, with no manual Skill or saved-data controls displayed.

Key images: `00-independent-actions.png`, `01-conversation.png`, `04-tomorrow.png`, `07-renamed-pet.png`, `08-updated-wake-time.png` in the evidence directory. These are in-game Canvas renders, not operating-system desktop captures. Camera placement used the test background; physical camera tracking and mobile keyboard/safe-area behavior were not tested.

## Regression tests

`LocalAI/current-scenes/tests.xml`: **19 passed, 0 failed, 1 skipped**. Coverage includes AI data scopes, automatic Skill routing (English and Chinese), follow-up routing, history invalidation after settings changes, cancellation/retry and late responses, AR navigation and bottom-navigation layout consistency. The optional online backend integration test was skipped because no test endpoint was configured. Live online-model generation was not verified.

## Quality observations

- One companionship answer addressed the user as “Mocha”, confusing the pet's name with the user's name. This is a real response-quality issue despite the routing/data checks passing.
- Many replies repeated “How can I assist you today?” or a similar generic offer. The data answers were correct in this run, but conversational variety remains limited.
- The player logged a Direct3D disk-cache recreation message; rendering and the entire validation run completed. There was no gameplay exception in the inspected player log.

This was a verification pass, not a model-quality rewrite. The observed wording issues remain available for a focused follow-up fix.
