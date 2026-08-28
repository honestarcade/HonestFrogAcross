# Difficulty curve — the shipped 100 levels

The shipped set `level-001` … `level-100` is generated, never hand-typed. The
single source of truth is `Assets/GameData/GeneratorParams/curve.asset`
(a `DifficultyCurve` asset), and every level is reproducible from it:

- Level *n* uses seeds `baseSeed + n·1000 + attempt` (attempt 0, 1, 2 …) and
  keeps the **first** candidate that passes required-kinds + schema validation +
  solver proof. Same asset ⇒ byte-identical set.
- Regenerate everything:
  `Unity -batchmode -quit -executeMethod FrogAcross.Editor.Generator.CurveGenerator.GenerateAll`
  (or menu *FrogAcross ▸ Levels ▸ Generate curve set*).
- Hand-editing a level JSON without regenerating is caught twice: the
  regeneration diff, and #62's solvability-fixture hash guard in CI.

## Teaching order

One new lane kind per decade, so each mechanic gets ten levels of practice
before the next one arrives:

| Levels | Band | New mechanic | Lane pool |
|---|---|---|---|
| 1 | first-hop | one bay, one crossing, slow road | road ×2, grass |
| 2–10 | teaching-road | roads (and light medians) | road ×2, grass |
| 11–20 | river | riding logs/turtles/rafts | road, grass, river ×2 |
| 21–30 | swamp | gator backs, lily pads | road, grass, river, swamp ×2 |
| 31–40 | tracks | train warnings | + tracks, concrete |
| 41–50 | bike | crash traffic, 2s stun | + bike ×2 |
| 51–60 | walkway | conveyor drift, edge kill | + walkway ×2 |
| 61–80 | combination-1 | everything together | full pool |
| 81–100 | combination-2 | depth: rows, speed, density | full pool minus grass |

Bands *force* their new kind: every level in an introduction band contains at
least one row of the kind it teaches (`requiredKinds` — candidates without it
are rejected before validation).

## Pressure schedule

Within each band, values interpolate linearly from the band's start to its end,
so pressure rises inside a band, not just at band boundaries:

- **Rows**: 3 middle rows at L1 → 10 at L100.
- **Bays**: 1 at L1 → 5 by the 90s (each bay is one full crossing).
- **Speeds**: deadly 1.0–1.3 cells/s at L1 → 2.3–3.2 at L100; water, crash and
  conveyor speeds ramp on their own gentler schedules.
- **Spacing slack** (extra gap beyond vehicle size + 1.2 cells): 3.0–4.5 at L1
  shrinking to 1.2–2.2 at L100 — the traffic literally tightens.
- **Obstructions** on safe ground: 0% at L1 → up to 24% of tiles by L100
  (validator guarantees rows are never walled shut).
- **Columns**: 9–11 early, 11–13 late (wider boards = longer exposed runs).

Exact per-band numbers live in `curve.asset` (inspectable in the editor;
serialized in the repo) — this document records the shape and the why, the
asset records the values.

## Medal calibration

Medals derive from the diagonal-free solver floor recorded in the
solvability fixture (#62) — never hand-typed. The gold factor tapers as
boards lengthen: ×4.5 for L1–10, ×3.4 (11–20), ×2.9 (21–30), ×2.4 (31–40),
×2.0 from L41 on. Silver = gold × 1.45, bronze = gold × 2.1. Early boards
are short, so a flat 2.0× would make L1 gold an esport time — the taper
anchors L2–5 gold inside the design chips' 24s ballpark (12–48s), while
L1 stays the deliberate near-zero exception (#64's single-bay straight
line). The diagonal-free floor is deliberate: tap-region players can't
diagonal, and gold must be earnable in every control scheme (owner
fairness rule). Rebuild everything with
`FrogAcross ▸ Levels ▸ Rebuild shipped content` (≈70s of solving locally).

## Tuning log

- 2026-08-28 — initial schedule authored (#61). Bands and interpolation as
  tabled above; variance bounds for the monotonicity test live in CurveTests.
- 2026-08-28 — teaching tune (#64): split L1 into its own `first-hop` band
  (3 rows, 1 bay, slow wide traffic — the solver crosses it in a straight
  line); L2–10 deepened to 5–6 rows and 2–3 bays so early floors support
  meaningful medal times. Tracks weight halved in its intro band — train
  warn-cycles were spiking decade-4 min-times 60% above decade 5.
- 2026-08-28 — medal factors (#63): teaching factor raised 4.0 → 4.5 to lift
  L2 gold over the 12s anchor floor; taper otherwise as documented above.
