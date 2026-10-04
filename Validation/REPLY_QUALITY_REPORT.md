# Pet identity and repetitive-response fix

Date: 2026-10-04.

The local prompt now explicitly separates the assistant's current pet name from the user's identity. It reads the name from the current request context; Mocha remains only the fallback when a usable name is unavailable. The prompt forbids addressing the user by the pet name and limits self-introductions to relevant requests. Short factual answers no longer have a multi-sentence minimum. Generic service endings are explicitly discouraged even when present in conversation history. The shared companionship Skill and online backend instructions use the same identity and brevity rules.

No Scene or UI changes were made for this fix. No generated answer text is replaced with canned replies.

## Packaged model validation

Build: `Builds/Sleepet-Windows-20261004-224117-10235b` (complete ZIP with the same base name).

Ten real local-model scenarios passed, using isolated test data. Evidence and screenshots: `Validation/LocalAI/reply-quality/`. The validator now rejects common generic service endings and pet names appearing in the tested companionship replies. It checks default-name companionship, renaming through the settings UI, English companionship after renaming, and Chinese companionship. Existing preference, record, tomorrow-plan, stale-data and cancellation checks also passed.

Examples from `results.txt`:

- Wake time: “Your saved wake time is 07:35.”
- Updated wake time: “Your current saved wake time is 08:23.”
- Renamed introduction begins: “I am Miia.”
- Companionship after renaming begins: “You are not alone, my dear.” It does not address the user as Miia or Mocha.

All ten answers passed the generic-ending checks. Reply times were 0.76–6.48 seconds in this run. The model was not restarted for settings changes.

Backend regression: 9 tests passed, using a mocked provider and local HTTP fixture. Live online generation was not tested.

Unity AI regression: 17 passed, 0 failed, 1 optional online integration test skipped (`reply-quality/tests.xml`). Name coverage includes Miia, Luna and 小月 across all three Skills, plus the blank-name fallback. The complete ZIP passed packaged-model checksum verification.

This fixes the observed behavior through model instructions and verifies the reported cases; it is not a guarantee for every stochastic reply. The small model still sometimes produces awkward phrasing, including mixed-language wording in the Chinese sample. Full raw answers are preserved rather than edited for presentation.
