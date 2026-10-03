# Windows offline AI verification

Verified on 3 October 2026 using Unity 6000.3.21f1, Windows x64, AMD Ryzen 7 5800H and approximately 64 GB RAM.

## Deliverable

`Builds/Sleepet-LocalAI-Windows/Sleepet.exe` is the standalone entry point. The complete folder includes the 1,282,439,264-byte Qwen3 1.7B Q4_K_M model, llama.cpp b11146 CPU binaries, app-local Microsoft C++ runtime libraries and license notices. No Node server, Ollama installation, terminal command or user API key is required for Offline LLM. The package is approximately 1.48 GB before compression.

The model SHA-256 was verified as `d2387ca2dbfee2ffabce7120d3770dadca0b293052bc2f0e138fdc940d9bc7b5`. The runtime archive SHA-256 is pinned in `LocalAI/manifest.json`.

## Results

1. First Windows build, before skill integration: real native-model answer, “Hello! I'm here to help.” Loading plus generation took 2.70 seconds. Evidence: `LocalAI/phase1/smoke.txt`.
2. Final Windows build: success. Log: `../Logs/LocalAI-release-build.log`.
3. Final packaged AR chat verification: six real generated responses passed. Evidence: `LocalAI/final/results.txt` and six PNG screenshots.

| Scenario | Result | Seconds |
| --- | --- | ---: |
| Conversation, saved data disabled | Local generated answer | 4.67 |
| Daily skill with preferences enabled | Correct saved wake time, 07:35 | 1.33 |
| Record review | One real app session, ten minutes; 600-minute sample excluded | 2.33 |
| Tomorrow preparation | Included the saved library activity | 2.74 |
| Outdated plan | Did not reuse the library activity | 0.67 |
| Permission revoked | Did not reveal the previously supplied wake time | 1.34 |

4. Cancelled generation preserved the draft for retry. Unity and its llama-server child had exited after verification; no remaining model process was found.
5. AI regression suite: six passed, zero failed, two optional OpenAI fixture tests skipped because their test backend was not started. Covers bounded snapshot permissions, local skill data scopes, sample exclusion, stale plans, context clearing, retry and late-response cancellation. Evidence: `LocalAI/tests.xml`.
6. Screenshots were rendered from the actual standalone AR canvas. Record review, tomorrow plan and permission-off screenshots were visually inspected; controls and generated text are legible without overlap.

## Scope and limits

The native inference process uses `--offline`, binds only to loopback, uses a per-launch token and requires no external inference service. The machine's network adapter was not disabled during testing. A clean Windows VM test and testing on lower-memory hardware have not been performed.

All verification data was synthetic and isolated under `LocalAI/final/isolated-data`; it did not modify the user's saved preferences or plans. The test background was used, so camera permissions and physical AR tracking are outside this verification.

The small model can phrase answers imperfectly, including speaking in first person about an already saved plan. It has no write tool and cannot actually change preferences or plans. This version does not measure sleep or provide medical diagnoses. Typical latency depends on hardware and reply length; the first response includes model loading.
