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

## /n8-roadmap — 2026-08-27

- **Decision:** v1 targets Android only; iOS/App Store deferred to the post-v1 backlog epic (#1).
  **Why:** Design specifies landscape Android; owner chose Android-first when asked.
  **Issue:** #1
- **Decision:** Fully automated store upload — tag → signed AAB → Google Play internal track via CI.
  **Why:** Owner chose it over release-artifact-only; requires UNITY_LICENSE, keystore, and Play service-account secrets (owner actions in M0/M1).
  **Issue:** #3
- **Decision:** v1 content is ship-critical only: death/retry and level-complete/medal overlay get designed-as-built; runway/helipad lanes and "still to catalogue" pieces go to the backlog epic.
  **Why:** The design flags these as unbuilt; owner confirmed the split.
  **Issue:** #1, #7
- **Decision:** 100 levels come from a generator + hand-tuning, with schema/solvability validation in CI.
  **Why:** Hand-authoring 100 boards is prohibitive; owner confirmed.
  **Issue:** #10
- **Decision:** Audio assets are owner-supplied; the audio system is built against placeholders so only the final swap blocks.
  **Why:** No audio exists in the design project; owner chose to supply their own.
  **Issue:** #11
- **Decision:** Four project invariants recorded in CLAUDE.md (no-network/no-ads, data-driven levels, piece object model, determinism), with executable guards planned into M0/M2/CI.
  **Why:** Derived from the design's own statements (no ads/tracking, on-device only) and the owner's stated extensibility requirements; owner confirmed all four.
