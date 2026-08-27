# HonestFrogAcross

2D Unity game (Unity 6000.5.10f1, Universal Render Pipeline 2D, Input System). Binary assets go through Git LFS (`.gitattributes`); serialization is Force Text — keep it that way so diffs work.

## Project invariants

Load-bearing constraints no story may breach without an explicit conversation with the owner. Changing one is plan drift by definition: log it as an ad-hoc ledger entry in `.n8/decisions.md` and suggest `/n8-replan`.

1. **No ads, no tracking, no analytics, no network calls** — all player data stays on-device. *(test-enforced: guard test fails the build if network permissions or ads/analytics packages appear — guard: #17, planned; marking becomes true when the guard code merges)*
2. **Levels are pure data (JSON)** — adding a level requires zero code changes. *(test-enforced: schema validation; levels load with no code registration)*
3. **Every game piece is a data-defined object type** (characters, lane types, lane objects, obstructions) — new pieces slot in without rewriting systems. *(honor-system, checked by audits)*
4. **Deterministic levels** — a level plays identically every run (seeded, fixed traffic patterns). *(test-enforced: replay/determinism guard test)*

## n8SDLC project

This project is managed by the n8SDLC workflow (GitHub Issues = the plan; `/n8-stat` shows where things stand). If a change made in this session deviates from what planned issues assume — different library, provider, architecture, dropped/added scope, or amending a declared invariant below — do two things before finishing:
1. Append an `## Ad-hoc` entry to `.n8/decisions.md` (format documented in that file's header) naming the change, the why, and the milestones/issues likely affected.
2. Tell the user which future milestones may now have stale plans and suggest running `/n8-replan`.
