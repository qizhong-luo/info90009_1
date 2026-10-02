# Weekly Report style correction — 2026-10-02

Reference: Figma `wL0dEDwxixjr25vdROZULV`, frame `53:378` (Sleepet Weekly Report).

The Weekly scene now uses top-aligned cyan duration columns with a navy final column, single-letter weekday labels, an underlined final day, and a dashed average-bedtime reference. Column height remains driven by each saved record; the prototype sample values are not substituted for user data. The chart follows the prototype's compressed overnight axis.

Updated card typography/colors, rounded period border, companion message, and health detail entry. Watch and chevron assets were exported directly from nodes `90:355` and `90:362`. Existing mascot, rounded panel and navigation sprites are reused. TEST + and sample-data disclosure remain available.

Scene maintenance: `Sleepet/Apply Weekly Figma Layout` updates only `Sleepet_Weekly.unity`; the existing all-scene maintenance entry calls the same styling implementation.

Verification: Unity 6000.3.21f1 compilation and scene save succeeded. The existing `MoonOpensWeeklyAndEachDateOpensItsOwnSavedReport` PlayMode test passed after the final refinements (1 passed, 0 failed). Final results: `weekly-style-final.xml`; rendered evidence, visually inspected: `SceneScreens/WeeklyWithTestData.png`. The project still uses its existing Unity font rather than Figma's Inter font, so text rendering is not pixel-identical.
