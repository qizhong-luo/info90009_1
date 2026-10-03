# AI conversation version one validation

Date: 2 October 2026

## Implemented

- Three initialized, versioned application skill presets shared by Unity and the backend.
- Serialized AR controls for skill selection, online/offline selection, saved-data access, scrolling conversation, retry and cancellation.
- OpenAI Responses API proxy with read-only function tools, bounded local snapshots, source indicators and separate sample/app-session totals.
- Six completed conversation turns in memory, independent of rendered bubbles. Clear context on skill/provider/permission changes.
- Request cancellation on AR exit, app pause and scene lifecycle; retained drafts and no duplicate user message on retry.
- Specific connection/configuration/rate-limit errors and visible offline replies.
- Empty-state guidance and a small pet response animation.

## Checks

| Check | Result |
| --- | --- |
| Unity scene authoring and script compilation | Passed |
| Full PlayMode regression suite before final UX refinements | 19 passed, 0 failed |
| AI-focused PlayMode suite after refinements | 5 passed, 0 failed, 0 skipped |
| Node backend tests | 9 passed, 0 failed |
| Unity to local backend HTTP integration | Passed with a fake upstream provider, including function output and invalid-token handling |
| AR chat screenshot review | Reviewed at portrait 402 by 874 design size and a 2x capture; no clipping or overlapping chat controls observed |

Test reports and the chat screenshot are generated under ignored `Validation/AI`. The upstream mock is only in the dedicated integration fixture. The normal server always calls OpenAI.

## Remaining external validation

No OPENAI_API_KEY or OPENAI_MODEL was configured in this session, so real OpenAI credentials, model availability, latency and generated-answer quality were not tested. Mobile camera behavior, soft keyboard interaction and production authentication need target-device or deployment testing. This version defaults to offline mode and does not implement voice, persistent memory, streaming or data writes.
