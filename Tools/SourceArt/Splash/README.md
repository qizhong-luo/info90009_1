# Splash wordmark source

These seven SVGs are unchanged copies of the original Sleepet frontend's
`assets/onboarding/splash/letter-*.svg` files. Keep their viewports and paths
intact; the splash animation positions each letter separately.

From the Unity project directory, run `node Tools/RasterizeSplashLetters.cjs`
to regenerate `Assets/Sleepet/Art/Flow/letter-*.png` at 512 pixels on the
longest edge. This rasterizes the vector paths directly, rather than enlarging
the previous tiny PNGs. The full `ImportFlowAssets.cjs` import uses the same
renderer for splash letters. Existing Unity `.meta` GUIDs must be retained
so scene sprite references survive regeneration.
