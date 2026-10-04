# Sleepet AI conversation version one

Unity AR chat now supports three versioned application skills, an OpenAI Responses API backend, recent conversation context and read-only local data snapshots. Offline mode is the default. The backend does not persist conversation or user data and never reads arbitrary client file paths.

## Run locally

Requires Node.js 22 or newer. No npm install is needed.

1. In a local terminal, set `OPENAI_API_KEY` to your own API key and `OPENAI_MODEL` to a Responses API model available to your project that supports function calling. Do not put a key into Unity, resource files, screenshots or source control.
2. From `Backend`, run `node server.mjs`.
3. `GET http://127.0.0.1:8787/health` reports `ready: true` when both environment variables are present. This checks configuration, not provider credentials or model availability.
4. Open Unity's `Sleepet_Home` scene and enter Play mode. Enter AR and choose **Chat with the Sleepet**; placement and camera access are not required for chat.
5. Use the model button to switch from Local model to Online model, then send a message. The application automatically selects the relevant skill from message content.
6. Saved data is enabled by default, scoped to the automatically selected skill. With Online model selected, snapshots are sent to the backend and relevant tool results to OpenAI. Camera frames, profile details and raw event logs are never sent.

For PowerShell, environment variables have this shape (replace values privately):

```powershell
$env:OPENAI_API_KEY = '<your key>'
$env:OPENAI_MODEL = '<your function-calling model>'
node server.mjs
```

The default URL and timeout are in `Assets/Sleepet/Resources/CompanionAIConfig.json`. HTTP is allowed only for loopback. A phone's loopback address refers to the phone, so testing on a phone requires a reachable HTTPS backend. Non-loopback server binding requires `SLEEPET_BACKEND_TOKEN`; use a TLS reverse proxy. The Unity desktop provider reads the same token environment variable. A mobile login can inject a short-lived token through the `OpenAICompanion` constructor. The provided shared-token service is a development service, not a multi-user production authentication system.

## Initialized skills

The canonical JSON presets are in `Assets/Sleepet/Resources/CompanionSkills`. Unity loads them for validation and the backend loads their trusted instructions and allowed tools from the same files. Restart the backend after editing them.

| Skill | Tool access | Behavior |
| --- | --- | --- |
| `daily_companionship` | `get_preferences` | Brief, warm conversation, matching the user's language and companion preferences. |
| `record_review` | `get_sleep_history` | Review up to 30 records from the last seven local calendar days. Samples are separate; totals describe app use, not measured sleep. |
| `tomorrow_preparation` | `get_tomorrow_plan`, `get_preferences` | Read a saved plan whose date matches tomorrow. Missing, stale and unreadable data are explicit. |

Skills are routed automatically from English or Chinese message content; short follow-ups retain the previous skill. Topic changes preserve the visible transcript. Settings changes invalidate stale model context while retaining the visible transcript. Provider switching clears conversation context. Conversation history keeps at most six completed turns in memory; visible chat retains up to 24 messages. Failed and cancelled turns are not included in model context. Nothing automatically becomes long-term memory.

## Reliability and boundaries

- Pending requests can be cancelled; leaving AR cancels them and ignores late replies. The draft is retained for retry.
- Online errors do not silently masquerade as successful AI replies. Retry or explicitly switch to Offline. Offline mode uses bundled Qwen3 inference and the same three skill presets. Saved data is read locally only when enabled. See ../LocalAI/README.md.
- The backend validates skill IDs, dates, history roles, data scopes and tool arguments. All tools are read-only and bounded; activity text is untrusted data.
- Source labels are derived from executed tools, not from model-generated citations. Responses API requests use `store: false`; this is not a claim of zero provider data retention.
- The backend accepts at most 64 KiB per request, 20 requests per minute per address and four concurrent requests. Provider work times out after 35 seconds; the default Unity timeout is 45 seconds.
- No automatic retries of paid API calls. No raw chat or keys in application logs. No streaming, voice, persistent long-term chat memory, measured-sleep analysis or model-driven write actions in this version.
- Saved-data access is enabled by default and remains scoped and read-only; there is no visible saved-data toggle. Local inference is the default provider. Switching to online mode sends permitted snapshots to the configured backend; later context changes cannot retract a completed network request.

## Validation

Run backend checks with `node --test` from `Backend`. These tests use a fake provider and a real local HTTP server, covering tool execution, authorization, invalid inputs, missing data and provenance. They do not spend API credits.

Run Unity PlayMode tests in the Test Runner, including `CompanionAITests` and `SceneArchitectureTests`. The editor menu `Sleepet > Apply AI Chat Version One` rebuilds the serialized AR controls if needed. The saved scene already includes them.

For the optional full Unity-to-backend round-trip test, start `node fixtures/unity-integration.mjs` in a separate terminal and set `SLEEPET_AI_TEST_URL=http://127.0.0.1:8788/api/chat` in the environment used to launch Unity. Run `CompanionAITests`. This fixture has a fake upstream provider and checks both a successful tool-backed response and a denied session token. Without this environment variable, only that integration test is skipped. Stop the fixture when testing is finished.

Before release, test with real API credentials and the chosen model, then verify the camera and layout on each target device. A production deployment also needs per-user authentication, quotas, secret management and an explicit retention policy.

API reference: https://developers.openai.com/api/docs/guides/function-calling
