# HonestFrogAcross

2D Unity game (Unity 6000.5.10f1, Universal Render Pipeline 2D, Input System). Binary assets go through Git LFS (`.gitattributes`); serialization is Force Text — keep it that way so diffs work.

## n8SDLC project

This project is managed by the n8SDLC workflow (GitHub Issues = the plan; `/n8-stat` shows where things stand). If a change made in this session deviates from what planned issues assume — different library, provider, architecture, dropped/added scope, or amending a declared invariant below — do two things before finishing:
1. Append an `## Ad-hoc` entry to `.n8/decisions.md` (format documented in that file's header) naming the change, the why, and the milestones/issues likely affected.
2. Tell the user which future milestones may now have stale plans and suggest running `/n8-replan`.
