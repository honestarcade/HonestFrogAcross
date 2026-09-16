# Audio provenance

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

## Music (0 of 2)

`music-menu` and `music-gameplay` are not yet supplied. Since #103 an absent
music file is real silence rather than a placeholder tone, so the slots stay
quiet until tracks land. See #105.

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
