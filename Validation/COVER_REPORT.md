# Cover and exit validation

- Added an authored CoverPanel to SleepetApp.prefab, using the existing palette and Mocha prefab.
- Get started dismisses the cover and opens Home. Exit is available on the cover and main header.
- Quit preparation ends and saves an active session once, stops audio, and closes the camera panel. Editor Exit stops Play Mode; standalone Exit calls Application.Quit.
- Unity 6000.3.21f1 successfully imported and saved the prefab.
- PlayMode: 15 passed, 0 failed, 0 skipped (`cover-results.xml`). Setup verifies the initial cover and enters via UI raycasting; regression tests cover navigation and session handling. The new quit preparation test checks persistence and repeated cleanup.
- Inspected `Editable/00-cover.png`: title, pet, description, and both buttons are visible within the phone layout.
- Automated tests exercise quit cleanup without terminating the test runner; the OS window close itself was not manually tested.

## Minimal cover revision
- Removed the pet, decoration, subtitle, and footer. The cover now contains only Sleepet, Start, and Exit.
- Inspected the refreshed Editable/00-cover.png. The cover entry and page navigation PlayMode check passed (1/1; minimal-cover-results.xml).
