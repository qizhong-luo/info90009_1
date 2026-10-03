# Independent AR and automatic-skill chat

Verified on Windows x64 on 3 October 2026, using Unity 6000.3.21f1 and the bundled Qwen3 1.7B model.

## Delivered behavior

- **Meet the Sleepet in AR** places the pet without opening chat.
- **Chat with the Sleepet** opens chat directly, without requiring a placed pet or a working camera. The camera closes while chatting.
- Chat reuses the application's navy, pale-blue and rounded-panel visual assets. Manual skill tabs, the saved-data toggle and technical source labels are removed from the visible interface.
- Saved-data access defaults to enabled. Each skill still receives only its scoped, read-only snapshot. The local/online model button is the only visible model/data setting. Online mode displays a notice that chat and saved information are sent online.
- English and Chinese intent rules automatically route messages to daily companionship, record review or tomorrow preparation. Short follow-up questions retain the previous skill; explicit topic changes select a new one. This is deterministic application routing, not an additional cloud classifier.
- The visible conversation survives topic changes. When stored facts change, outdated model context is cleared while the visible transcript remains intact.

## Verification

Windows build succeeded: `../Logs/Chat-refinement-release.log`.

Regression tests: **17 passed, 0 failed, 1 optional OpenAI fixture test skipped**. Evidence: `LocalAI/chat-release-tests.xml`. Coverage includes bilingual routing, follow-up intent, model switching, default data access, independent placement/chat, navigation, changed-data context invalidation, permissions, retries and cancellation.

Real generated answers in the packaged player passed six sequential scenarios:

1. Ordinary conversation.
2. Automatically read the saved wake time: 07:35.
3. Automatically review records: one real session, ten minutes; samples excluded.
4. Automatically list all five saved activities, including the library visit.
5. After the saved plan became outdated, reply that no valid plan exists rather than repeat old activities.
6. Switch back to daily companionship when the user says they feel tired.

Cancellation also passed. All saved verification data was synthetic and isolated. Evidence: `LocalAI/chat-release/results.txt`. The application and its model child process exited after verification.

Screenshots: `LocalAI/chat-release/00-independent-actions.png`, `04-tomorrow.png`, and `05-stale-plan.png`. The entry buttons, chat layout, long conversation scrolling and current-plan response were visually reviewed.

## Distribution and limits

Extract the complete `Builds/Sleepet-Chat-Updated-Windows.zip` and double-click `Sleepet.exe`. Offline chat needs no Node, terminal, Internet or API key. The optional online provider still requires a configured OpenAI backend; live OpenAI inference was not tested in this revision.

Routing is rule-based and can misclassify unusual wording or mixed-topic requests. The small local model may produce repetitive phrasing. This verification used the development Windows machine and a test background; it does not establish compatibility with every Windows device or physical spatial AR tracking.
