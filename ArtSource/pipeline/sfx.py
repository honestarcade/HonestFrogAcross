#!/usr/bin/env python3
"""Sound-effect generation (#105/#66): ElevenLabs text-to-sound-effects into
the game's clip names, post-processed to the lengths the engine expects.

Unlike extract.py, this is NOT reproducible — the model returns something new
every call. The committed WAVs are the artifact of record; this script exists
to audition candidates and to regenerate one sound when a better prompt is
found. Owner picks the winners by ear.

Usage:
  source ~/HonestArcadeApps/secrets/elevenlabs.env
  python3 sfx.py <out_dir> [--variants 3] [--only hop,ui-tap]

Writes <out_dir>/<key>-v<N>.wav (44.1kHz, mono, 16-bit) plus prompts.txt.
"""
import argparse
import ssl
import json
import os
import struct
import sys
import urllib.request
import wave
from pathlib import Path

API = "https://api.elevenlabs.io/v1/sound-generation?output_format=pcm_44100"
RATE = 44100
# pcm_44100 comes back as INTERLEAVED STEREO 16-bit. Writing it as mono plays
# every clip at half speed an octave down, holding half its content — verified
# against `afinfo` on an mp3 of the same request (2026-09-16).
CHANNELS = 2


# audioop was removed in Python 3.13 (PEP 594) and this script only ever needed
# two operations from it, so they live here instead (#132).
def _peak(pcm: bytes) -> int:
    """Largest absolute 16-bit sample."""
    hi = 0
    for i in range(0, len(pcm) - 1, 2):
        v = struct.unpack_from("<h", pcm, i)[0]
        if v == -32768:
            return 32768
        hi = max(hi, abs(v))
    return hi


def _scale(pcm: bytes, factor: float) -> bytes:
    """Multiply every sample, clamped to the 16-bit range."""
    out = bytearray(len(pcm))
    for i in range(0, len(pcm) - 1, 2):
        v = int(struct.unpack_from("<h", pcm, i)[0] * factor)
        struct.pack_into("<h", out, i, max(-32768, min(32767, v)))
    return bytes(out)


def _to_mono(pcm: bytes) -> bytes:
    """Average interleaved stereo down to mono."""
    out = bytearray(len(pcm) // 2)
    for i in range(0, len(pcm) - 3, 4):
        left = struct.unpack_from("<h", pcm, i)[0]
        right = struct.unpack_from("<h", pcm, i + 2)[0]
        struct.pack_into("<h", out, i // 2, (left + right) // 2)
    return bytes(out)


def _ssl_context() -> ssl.SSLContext:
    """python.org builds on macOS ship without a CA bundle; certifi has one."""
    try:
        import certifi
        return ssl.create_default_context(cafile=certifi.where())
    except ImportError:
        return ssl.create_default_context()

# key -> (prompt, target_seconds). Targets come from the engine: a hop may not
# outlast the 9-tick hop cooldown or it overlaps itself on a fast run, and a
# death sound has the 30-tick respawn window to play in.
SOUNDS = {
    "hop": ("a single short soft cartoon boing, a small frog hopping, dry and "
            "tight, no reverb, no music", 0.15),
    "death-splat": ("a cartoon splat, small squishy impact against a hard "
                    "surface, dry, comic, single hit", 0.50),
    "death-sink": ("a short water plop and gurgle as something small sinks "
                   "under the surface", 0.50),
    "death-slide": ("a short comic slip and whoosh sliding off an edge, "
                    "descending, dry", 0.45),
    "rider-crash": ("a bicycle falling over, short metallic clatter and "
                    "tumble, cartoon, dry", 0.55),
    "stun": ("a short dizzy cartoon stun, wobbling descending tone", 0.30),
    "bay-fill": ("a bright short positive confirm chime, like landing safely, "
                 "clean and warm", 0.35),
    "medal": ("a warm celebratory sparkle chime, rewarding, short and bright", 1.40),
    "level-complete": ("a short triumphant arcade fanfare, upbeat and bright, "
                       "resolving", 1.80),
    "train-warning": ("a railway level-crossing bell, urgent repeated ding, "
                      "metallic", 0.70),
    "turtle-warning": ("an urgent soft underwater bubbling alert, rising, "
                       "warning that something is about to submerge", 0.60),
    "ui-tap": ("a tiny soft muted interface tap, minimal click, very short", 0.12),
    "ui-navigate": ("a soft short interface whoosh for moving between screens, "
                    "subtle and airy", 0.22),
}


# The two music slots, generated with the sound-effects endpoint's loop flag
# (v2 model) rather than Eleven Music: the SFX terms grant a plain commercial
# licence on any paid plan, while Eleven Music's rights depend on a separate
# table and a "Studio Games" category we cannot read from the public docs
# (2026-09-16). These are ambient beds, not a composed soundtrack.
MUSIC = {
    "music-menu": ("a calm playful looping background bed for a cartoon game "
                   "menu, soft warm marimba and gentle pad, unhurried, light, "
                   "instrumental, no drums", 24.0),
    "music-gameplay": ("a light energetic looping background bed for a cartoon "
                       "arcade game, playful gentle percussion and warm bass, "
                       "steady, instrumental, not intense", 24.0),
}


# Relative level per sound, in dB below full scale, applied when a pick is
# installed. Generation normalises every clip to -1 dBFS, which would put a
# menu tap at the same volume as the win fanfare — these are the levels that
# make them sit together. Category buses (music/effects/ui) sit on top.
MIX_DB = {
    "hop": -9.0,            # heard constantly; must not dominate
    "ui-tap": -13.0,
    "ui-navigate": -12.0,
    "bay-fill": -6.0,
    "stun": -7.0,
    "rider-crash": -6.0,
    "death-splat": -4.0,
    "death-sink": -4.0,
    "death-slide": -5.0,
    "train-warning": -5.0,  # a warning has to cut through traffic
    "turtle-warning": -6.0,
    "medal": -2.5,          # rewards are the loudest thing in the game
    "level-complete": -2.0,
}


# Absolute peak each music bed sits at once installed. Effects take their level
# from MIX_DB applied over a -1 dBFS normalise; a bed is declared ABSOLUTELY so
# re-levelling is idempotent — a relative cut applied twice cuts twice (#120).
MUSIC_DBFS = {
    "music-menu": -15.0,      # owner: "about half the volume" (2026-09-17)
    "music-gameplay": -9.0,   # relevel writes this file too; the LEVEL is unchanged
}


def relevel(names) -> None:
    """Set an already-installed bed to its declared level. Idempotent."""
    import math
    for name in names:
        path = DEST / f"{name}.wav"
        with wave.open(str(path), "rb") as w:
            channels, frames = w.getnchannels(), w.getnframes()
            pcm = w.readframes(frames)
        peak = _peak(pcm) or 1
        before_db = 20 * math.log10(peak / 32767.0)
        target = 32767.0 * (10 ** (MUSIC_DBFS[name] / 20.0))
        write_wav(path, _scale(pcm, target / peak), channels)
        print(f"  {name}.wav  {before_db:.1f} -> {MUSIC_DBFS[name]:.1f} dBFS  "
              f"{frames / RATE:.1f}s {channels}ch")


def generate(prompt: str, seconds: float, key: str) -> bytes:
    """One call to the sound-effects endpoint; returns raw 16-bit mono PCM."""
    body = json.dumps({
        "text": prompt,
        # the API floor is 0.5s; anything shorter gets trimmed here instead
        "duration_seconds": max(0.5, round(seconds + 0.2, 2)),
        "prompt_influence": 0.6,
    }).encode()
    req = urllib.request.Request(API, data=body, method="POST", headers={
        "xi-api-key": key,
        "Content-Type": "application/json",
    })
    with urllib.request.urlopen(req, timeout=120, context=_ssl_context()) as r:
        return r.read()


def polish_loop(pcm: bytes) -> bytes:
    """A loop may not be trimmed or faded — either would break the seam. Set
    the level only, and leave it a few dB down: music sits under the game.
    Music stays STEREO."""
    peak = _peak(pcm) or 1
    return _scale(pcm, min(8.0, (32767 * 0.35) / peak))  # about -9 dBFS


def generate_loop(prompt: str, seconds: float, key: str) -> bytes:
    body = json.dumps({
        "text": prompt,
        "duration_seconds": min(30.0, seconds),
        "prompt_influence": 0.5,
        "loop": True,
    }).encode()
    req = urllib.request.Request(API, data=body, method="POST", headers={
        "xi-api-key": key,
        "Content-Type": "application/json",
    })
    with urllib.request.urlopen(req, timeout=300, context=_ssl_context()) as r:
        return r.read()


def polish(pcm: bytes, seconds: float) -> bytes:
    """Trim the silent head, cut to length, fade out, normalise to -1 dBFS.

    Leading silence is the one defect a player feels rather than hears: on
    `hop` and `ui-tap` it reads as input lag.
    """
    if not pcm:
        return pcm
    pcm = _to_mono(pcm)  # stereo in, mono out: effects are mono
    peak = _peak(pcm) or 1
    gate = max(int(peak * 0.02), 64)

    start = 0
    for i in range(0, len(pcm) - 1, 2):
        if abs(struct.unpack_from("<h", pcm, i)[0]) > gate:
            start = i
            break
    start = max(0, start - int(0.005 * RATE) * 2)  # 5ms of pre-roll
    pcm = pcm[start:]

    want = int(seconds * RATE) * 2
    if len(pcm) > want:
        pcm = pcm[:want]

    # 12ms fade-out so a hard cut never clicks
    fade = min(int(0.012 * RATE), len(pcm) // 4)
    if fade > 0:
        tail = bytearray(pcm[-fade * 2:])
        for n in range(fade):
            off = n * 2
            v = struct.unpack_from("<h", tail, off)[0]
            struct.pack_into("<h", tail, off, int(v * (1 - n / fade)))
        pcm = pcm[:-fade * 2] + bytes(tail)

    peak = _peak(pcm) or 1
    return _scale(pcm, min(8.0, (32767 * 0.89) / peak))  # -1 dBFS


def write_wav(path: Path, pcm: bytes, channels: int = 1) -> None:
    with wave.open(str(path), "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm)


DEST = Path(__file__).resolve().parents[2] / "Assets/Resources/Audio"


def install(src: Path, picks: dict) -> None:
    """Copy the chosen takes into the game at their mixed levels."""
    for name, variant in picks.items():
        with wave.open(str(src / f"{name}-v{variant}.wav"), "rb") as w:
            pcm, channels, frames = w.readframes(w.getnframes()), w.getnchannels(), w.getnframes()
        # a loop is already at its bed level and must not be touched again
        gain = 10 ** (MIX_DB.get(name, 0.0) / 20.0)
        write_wav(DEST / f"{name}.wav", _scale(pcm, gain), channels)
        peak = _peak(_scale(pcm, gain))
        print(f"  {name}.wav  v{variant}  {MIX_DB.get(name, 0.0):+.1f} dB  "
              f"peak {20 * __import__('math').log10(max(peak, 1) / 32767.0):.1f} dBFS  "
              f"{frames / RATE:.2f}s  {channels}ch")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("out_dir")
    ap.add_argument("--variants", type=int, default=3)
    ap.add_argument("--only", default="")
    ap.add_argument("--relevel", action="store_true",
                    help="set installed music beds to their declared MUSIC_DBFS level")
    ap.add_argument("--music", action="store_true",
                    help="generate the two looping music beds instead")
    ap.add_argument("--install", default="",
                    help="pick winners and install them, e.g. hop=2,ui-tap=3")
    args = ap.parse_args()

    if args.install:
        picks = dict(
            (k.strip(), int(v)) for k, v in
            (pair.split("=") for pair in args.install.split(","))
        )
        unknown = [k for k in picks if k not in SOUNDS and k not in MUSIC]
        if unknown:
            print(f"unknown sound(s): {', '.join(unknown)}", file=sys.stderr)
            return 2
        install(Path(args.out_dir), picks)
        print(f"\n{len(picks)} clips installed to {DEST}")
        return 0

    if args.relevel:
        relevel(list(MUSIC))
        print(f"\n{len(MUSIC)} beds re-levelled in {DEST}")
        return 0

    # --relevel makes no network call, so the key is only required past here (#132)
    key = os.environ.get("ELEVENLABS_API_KEY", "")
    if not key:
        print("ELEVENLABS_API_KEY is not set — source the env file first", file=sys.stderr)
        return 2

    if args.music:
        out = Path(args.out_dir)
        out.mkdir(parents=True, exist_ok=True)
        for name, (prompt, seconds) in MUSIC.items():
            for v in range(1, args.variants + 1):
                try:
                    pcm = polish_loop(generate_loop(prompt, seconds, key))
                except Exception as exc:              # noqa: BLE001
                    print(f"  {name} v{v}: FAILED ({exc})")
                    continue
                write_wav(out / f"{name}-v{v}.wav", pcm, CHANNELS)
                print(f"  {name} v{v}: {len(pcm) / (2 * CHANNELS) / RATE:.1f}s stereo  "
                      f"{len(pcm) / 1024 / 1024:.1f} MB")
        return 0

    wanted = [k.strip() for k in args.only.split(",") if k.strip()] or list(SOUNDS)
    unknown = [k for k in wanted if k not in SOUNDS]
    if unknown:
        print(f"unknown sound(s): {', '.join(unknown)}", file=sys.stderr)
        return 2

    out = Path(args.out_dir)
    out.mkdir(parents=True, exist_ok=True)
    notes = []
    for name in wanted:
        prompt, seconds = SOUNDS[name]
        notes.append(f"{name}  ({seconds:.2f}s)\n    {prompt}")
        for v in range(1, args.variants + 1):
            try:
                pcm = polish(generate(prompt, seconds, key), seconds)
            except Exception as exc:                      # noqa: BLE001 - report and continue
                print(f"  {name} v{v}: FAILED ({exc})")
                continue
            path = out / f"{name}-v{v}.wav"
            write_wav(path, pcm)
            print(f"  {name} v{v}: {len(pcm) / 2 / RATE:.2f}s  {path.name}")
    (out / "prompts.txt").write_text("\n".join(notes) + "\n")
    write_audition_page(out, wanted, args.variants)
    print(f"\n{len(wanted)} sounds x {args.variants} variants -> {out}")
    print(f"audition: open {out / 'audition.html'}")
    return 0


def write_audition_page(out: Path, names: list, variants: int) -> None:
    """A local page for picking winners by ear — three players per sound,
    side by side, with the trigger and the length budget in view."""
    rows = []
    for name in list(names) + [m for m in MUSIC if (out / f"{m}-v1.wav").exists()]:
        prompt, seconds = SOUNDS[name] if name in SOUNDS else MUSIC[name]
        players = "".join(
            f'<div class="v"><span>v{v}</span>'
            f'<audio controls preload="none" src="{name}-v{v}.wav"></audio></div>'
            for v in range(1, variants + 1)
            if (out / f"{name}-v{v}.wav").exists()
        )
        rows.append(
            f'<section><h2>{name} <em>{seconds:.2f}s</em></h2>'
            f'<p>{prompt}</p><div class="row">{players}</div></section>'
        )
    (out / "audition.html").write_text(
        "<!doctype html><meta charset=utf-8><title>Frog Across - SFX audition</title>"
        "<style>"
        "body{font:15px/1.5 system-ui,sans-serif;background:#05285F;color:#fff;margin:0;padding:32px}"
        "h1{font-size:26px;margin:0 0 4px}h1+p{color:#9FC3EE;margin:0 0 28px}"
        "section{background:rgba(255,255,255,.06);border-radius:14px;padding:16px 20px;margin:0 0 14px}"
        "h2{font-size:19px;margin:0 0 2px}h2 em{color:#00D6B4;font-style:normal;font-size:14px;margin-left:8px}"
        "section p{color:#9FC3EE;font-size:13px;margin:0 0 12px}"
        ".row{display:flex;flex-wrap:wrap;gap:16px}"
        ".v{display:flex;align-items:center;gap:8px}.v span{color:#6E93C4;font-size:13px;width:20px}"
        "audio{height:34px}"
        "</style>"
        "<h1>Frog Across &mdash; sound audition</h1>"
        "<p>Three takes per sound. Tell Claude the winners, e.g. &ldquo;hop v2, ui-tap v1&rdquo;.</p>"
        + "".join(rows)
    )


if __name__ == "__main__":
    raise SystemExit(main())
