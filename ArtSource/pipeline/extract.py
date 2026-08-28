#!/usr/bin/env python3
"""Batch sprite extraction (#46): drives headless Chrome over the committed
design components through mini-dc.js. Reproducible: same inputs → same PNG set.

Usage: python3 extract.py <out_dir> [--serve-dir <dir>] [--port 8433]
Serves ArtSource/design-components + pipeline, renders every manifest entry.
"""
import json
import subprocess
import sys
import threading
import urllib.parse
from functools import partial
from http.server import HTTPServer, SimpleHTTPRequestHandler
from pathlib import Path

CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
ROOT = Path(__file__).resolve().parent.parent  # ArtSource/

LIVERIES = ["blue", "red", "green", "purple"]
DIRS = ["right", "left"]

def manifest():
    m = []
    # Characters: 6 × 4 facings (animation poses are engine-side squash/step).
    for ch in ["frog", "bunny", "hopper", "roo", "dog", "cat"]:
        for facing in ["up", "down", "left", "right"]:
            m.append((f"char-{ch}-{facing}", "Creature", 120, 120,
                      {"char": ch, "facing": facing}, 0))
    # Road vehicles.
    for kind, w, h, livs in [("truck", 186, 48, LIVERIES), ("car", 88, 38, LIVERIES),
                             ("bus", 150, 46, LIVERIES), ("convertible", 88, 38, LIVERIES + ["black"])]:
        for liv in livs:
            for d in DIRS:
                m.append((f"{kind}-{liv}-{d}", "LaneObject", w, h,
                          {"kind": kind, "dir": d, "livery": liv}, 0))
    # Trains.
    for kind in ["freight", "passenger"]:
        for d in DIRS:
            m.append((f"{kind}-{d}", "LaneObject", 600, 56, {"kind": kind, "dir": d}, 0))
    # Riders: 3 animation frames across their limb cycles.
    for kind, w in [("cyclist", 60), ("skater", 56), ("runner", 52)]:
        for liv in LIVERIES:
            for d in DIRS:
                for i, off in enumerate([0.0, 0.27, 0.55]):
                    m.append((f"{kind}-{liv}-{d}-f{i}", "LaneObject", w, 56,
                              {"kind": kind, "dir": d, "livery": liv}, off))
    # Crash sequences (4 keyframes over the 2.4s storyboard) + crashed rest state.
    for kind in ["crashing", "skater-crashing", "runner-crashing"]:
        for liv in LIVERIES:
            for d in DIRS:
                for i, off in enumerate([0.05, 0.8, 1.55, 2.3]):
                    m.append((f"{kind}-{liv}-{d}-f{i}", "LaneObject", 72, 56,
                              {"kind": kind, "dir": d, "livery": liv}, off))
    for kind in ["crashed", "skater-crashed", "runner-crashed"]:
        for liv in LIVERIES:
            for d in DIRS:
                m.append((f"{kind}-{liv}-{d}", "LaneObject", 72, 56,
                          {"kind": kind, "dir": d, "livery": liv}, 0))
    # River / swamp objects.
    for kind, w in [("log-short", 130), ("log", 200), ("log-long", 270)]:
        for d in DIRS:
            m.append((f"{kind}-{d}", "LaneObject", w, 42, {"kind": kind, "dir": d}, 0))
    for kind in ["gator", "gator-open"]:
        for d in DIRS:
            m.append((f"{kind}-{d}", "LaneObject", 206, 44, {"kind": kind, "dir": d}, 0))
    # Obstructions.
    for kind, w, h in [("tree", 52, 62), ("bush", 46, 44), ("bench", 78, 50), ("lamp", 22, 70)]:
        m.append((f"ob-{kind}", "Obstruction", w, h, {"kind": kind}, 0))
    for plant in ["daisy", "tulip", "fern", "lavender", "succulent"]:
        m.append((f"ob-planter-{plant}", "Obstruction", 64, 52, {"kind": "planter", "plant": plant}, 0))
    # Lane surface tiles (bare, fixed 400px strips for horizontal tiling).
    for kind in ["road", "river", "swamp", "tracks", "bike", "grass", "concrete", "goal"]:
        m.append((f"lane-{kind}", "Lane", 400, 50,
                  {"kind": kind, "dir": "right", "w": 400, "bare": True, "preview": True}, 0))
    return m

class Quiet(SimpleHTTPRequestHandler):
    def log_message(self, *args):
        pass

def main():
    out = Path(sys.argv[1]).resolve()
    out.mkdir(parents=True, exist_ok=True)
    port = 8433

    serve_root = Path("/tmp/frog-extract-www")
    serve_root.mkdir(exist_ok=True)
    for f in (ROOT / "design-components").glob("*.dc.html"):
        (serve_root / f.name).write_text(f.read_text())
    for name in ["mini-dc.js", "harness.html"]:
        (serve_root / name).write_text((ROOT / "pipeline" / name).read_text())

    server = HTTPServer(("localhost", port), partial(Quiet, directory=str(serve_root)))
    threading.Thread(target=server.serve_forever, daemon=True).start()

    entries = manifest()
    failed = []
    for name, comp, w, h, props, off in entries:
        target = out / f"{name}.png"
        url = (f"http://localhost:{port}/harness.html?component={comp}&w={w}&h={h}"
               f"&props={urllib.parse.quote(json.dumps(props))}&animOffset={off}")
        r = subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars",
                            "--default-background-color=00000000",
                            f"--window-size={w},{h}", f"--screenshot={target}",
                            "--virtual-time-budget=4000", url],
                           capture_output=True, text=True, timeout=60)
        if not target.exists() or target.stat().st_size < 200:
            failed.append((name, r.stderr.strip()[-200:]))
    server.shutdown()

    print(f"extracted {len(entries) - len(failed)}/{len(entries)}")
    for name, err in failed:
        print("FAILED:", name, err)
    sys.exit(1 if failed else 0)

if __name__ == "__main__":
    main()
