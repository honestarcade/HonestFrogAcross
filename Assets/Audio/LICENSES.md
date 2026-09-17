# Audio provenance

> **Scope note.** The repository's `LICENSE` (MIT) covers the source code and the
> sprite art. The audio files below are **not** covered by it: they are licensed
> to Honest Arcade from ElevenLabs for use in Frog Across, and no licence is
> granted to use them in another project — ElevenLabs' terms specifically
> prohibit licensing collections of generated output to third parties.
>
> They ship here so the game builds and runs as released. To make your own, the
> prompts, length budgets and mix levels are all in `ArtSource/pipeline/sfx.py`;
> a paid ElevenLabs plan regenerates the set in a few minutes.

Every audio file shipped in `Assets/Resources/Audio/` is listed here with its
source and licence. The `TemporaryClips_AreClearlyTagged` guard fails the build
if a file is neither `placeholder-`prefixed nor named in this manifest.

## Sound effects (13)

| File | Source | Licence |
|---|---|---|
| `hop.wav` | ElevenLabs text-to-sound-effects | ElevenLabs Creator plan, commercial licence |
| `death-splat.wav` | ElevenLabs text-to-sound-effects | ElevenLabs Creator plan, commercial licence |
| `death-sink.wav` | ElevenLabs text-to-sound-effects | ElevenLabs Creator plan, commercial licence |
| `death-slide.wav` | ElevenLabs text-to-sound-effects | ElevenLabs Creator plan, commercial licence |
| `rider-crash.wav` | ElevenLabs text-to-sound-effects | ElevenLabs Creator plan, commercial licence |
| `stun.wav` | ElevenLabs text-to-sound-effects | ElevenLabs Creator plan, commercial licence |
| `bay-fill.wav` | ElevenLabs text-to-sound-effects | ElevenLabs Creator plan, commercial licence |
| `medal.wav` | ElevenLabs text-to-sound-effects | ElevenLabs Creator plan, commercial licence |
| `level-complete.wav` | ElevenLabs text-to-sound-effects | ElevenLabs Creator plan, commercial licence |
| `train-warning.wav` | ElevenLabs text-to-sound-effects | ElevenLabs Creator plan, commercial licence |
| `turtle-warning.wav` | ElevenLabs text-to-sound-effects | ElevenLabs Creator plan, commercial licence |
| `ui-tap.wav` | ElevenLabs text-to-sound-effects | ElevenLabs Creator plan, commercial licence |
| `ui-navigate.wav` | ElevenLabs text-to-sound-effects | ElevenLabs Creator plan, commercial licence |

**Generated:** 2026-09-16, on an ElevenLabs **Creator** subscription, model
`eleven_text_to_sound_v2` via `POST /v1/sound-generation`.

**Terms.** Paid ElevenLabs plans grant a commercial licence to generated audio,
and that licence survives the end of the subscription for anything generated
while it was active. The free plan grants no commercial licence and requires
"elevenlabs.io" in the title of published work — none of these files were
generated on it. **No attribution is required in the app.**

Sources: <https://help.elevenlabs.io/hc/en-us/articles/13313564601361-Can-I-publish-the-content-I-generate-on-the-platform>,
<https://elevenlabs.io/terms-of-use>

## Music (2)

| File | Source | Licence |
|---|---|---|
| `music-menu.wav` | ElevenLabs text-to-sound-effects, `loop: true` | ElevenLabs Creator plan, commercial licence |
| `music-gameplay.wav` | ElevenLabs text-to-sound-effects, `loop: true` | ElevenLabs Creator plan, commercial licence |

**Not Eleven Music.** These 24-second beds come from the same
`/v1/sound-generation` endpoint as the effects, using its `loop` flag, and are
covered by the same plain commercial licence. Eleven Music is a separate
product whose rights depend on a "Music Commercial Rights table" that is not in
the public docs, defines "Studio Games" as its own category, and attaches
co-branding obligations to paid tiers — none of which could be verified well
enough to assert a licence here (2026-09-16). If a composed soundtrack is
wanted later, those terms need reading first.

Both loop seamlessly by construction, so they are neither trimmed nor faded —
either would break the seam. They are installed at about -9 dBFS: music sits
under the game.

## How these were made

`ArtSource/pipeline/sfx.py` holds the prompt and length budget for every sound,
and the relative mix level each is installed at. Unlike the sprite pipeline it
is **not reproducible** — the model returns something new on every call — so the
committed WAVs are the artifact of record. The script exists to audition
candidates and to re-roll a single sound when a better prompt is found:

```sh
source ~/HonestArcadeApps/secrets/elevenlabs.env
python3 ArtSource/pipeline/sfx.py /tmp/audition --only hop --variants 3
python3 ArtSource/pipeline/sfx.py /tmp/audition --install hop=2
```

All clips are 44.1kHz mono 16-bit, trimmed of leading silence (which reads as
input lag on `hop` and `ui-tap`), cut to their length budget, faded out over
12ms so the cut never clicks, then set to the level in `MIX_DB`.
