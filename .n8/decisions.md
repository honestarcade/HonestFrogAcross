# Decision log

Append-only log of decisions made during planning and execution. One `##` section per skill run, entries appended chronologically:

```markdown
## /n8-exec M1 — 2026-08-27

- **Decision:** <what was chosen>
  **Why:** <the reasoning, including alternatives rejected>
  **Issue:** #N
```

Changes made **outside** the n8SDLC commands that deviate from planned issues get an `## Ad-hoc` section — this is the drift ledger `/n8-replan` and `/n8-stat` read:

```markdown
## Ad-hoc — YYYY-MM-DD

- **Change:** <what changed>
  **Why:** <the reason>
  **Affects:** <milestones/issues whose plans may now be stale>
```

When `/n8-replan` processes an ad-hoc entry it appends `— reconciled by /n8-replan <date>`.

---
