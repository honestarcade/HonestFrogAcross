# Brand sources

Vector sources for launcher icons, committed so builds never depend on external design-project access.

- `android-foreground-frog-mint.svg` + `android-background-navy.svg` — the Android **adaptive icon** layers (432 viewBox, art inside the safe zone). This is the icon: mint frog + Honest Arcade brackets on navy, per the design brand sheet's APP ICON card.
- `icon-square-frog-navy.svg` / `icon-rounded-frog-navy.svg` — legacy launcher icons, same composition with baked navy background.
- `studio-foreground-mint-brackets.svg` — the Honest Arcade **studio mark** foreground as uploaded to the design project (brackets only, no frog). Kept for provenance; not the app icon.

Provenance: claude.ai/design project "Frog Across Mobile Game" (`f3f74fad-a926-4ae3-986b-fd6531c21085`). The uploaded `android-foreground-*.svg` files there are the studio mark without the frog; the frog+brackets composition was reconstructed from the brand sheet's inline APP ICON SVG in `Frog Across.dc.html`.

Rasterize with e.g. `rsvg-convert -w 432 -h 432 android-foreground-frog-mint.svg > out.png` (see the icon story for target paths).
