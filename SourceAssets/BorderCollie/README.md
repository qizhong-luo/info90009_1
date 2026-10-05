# Border Collie thoughts animations

Reference: [Capstone high-fi, Slide 16:9 - 1](https://www.figma.com/design/wL0dEDwxixjr25vdROZULV/Capstone-high-fi?node-id=1-98).
The overlapping pet pose reference is node `13:106`. The Figma motion API returned no keyframe tracks;
these are newly generated motion sequences matching the illustrated black/white Border Collie and blue crescent bandana, not exported original Figma animations.

Generated with the built-in ImageGen tool, using the existing `Art/Figma/home-mocha-transparent.png` identity reference.
The prompt set and repair instruction are recorded in `generation-prompts.json`.

- `Gifs/`: six looping transparent GIFs (256 × 256, six frames at 5 fps).
- `*-strip.png`: source images, retained for reproducible packaging.
- `preview-sheet.jpg`: all six actions and their keyframes.
- [Animated overview](preview.gif)
- Unity uses matching PNG frames under `Assets/Sleepet/Resources/BorderCollieThoughts/`, avoiding a GIF decoder at runtime.

Run `Tools/PrepareBorderCollieThoughts.py` with Python, Pillow and NumPy to reproduce packaged assets.
It extracts the six complete alpha-connected characters before alignment, preserving tails that cross nominal slot boundaries.
No Golden Retriever frames are used.

## Motion and thought contract

| Motion | Tap | Ambient | Thought theme |
| --- | --- | --- | --- |
| [bark](Gifs/bark.gif) | yes | no | Greeting or one verified tomorrow-plan reminder |
| [licking1](Gifs/licking1.gif) | yes | no | Affection and appreciation (nose lick) |
| [licking2](Gifs/licking2.gif) | yes | no | Gentle companionship (paw grooming) |
| [itching](Gifs/itching.gif) | yes | yes | Playful whimsical daydream |
| [stretching](Gifs/stretching.gif) | yes | yes | Stretching or optional short break |
| [sitting](Gifs/sitting.gif) | yes | yes | Quiet companionship and optional focus |

Only the original black/white Border Collie appearance animates. Other appearances retain their selected static sprite and still receive the same thought themes.
Thoughts use a separate skill and no chat history. Local and online providers share the same preset.
Non-bark thoughts cannot receive saved plan activities. Generation failures or invalid text are hidden rather than replaced with canned AI claims.
AR foreground idle delays are independently sampled between 120 and 600 seconds. Taps reset the schedule. Leaving AR, losing focus, opening chat, dragging or switching providers cancels pending thoughts.
