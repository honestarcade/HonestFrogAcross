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

## /n8-plan M0 — 2026-08-27

- **Decision:** Package ID `com.honestarcade.frogacross`, minSdk 26 (Android 8.0), launcher name "FrogAcross", mint-frog-on-navy adaptive icon.
  **Why:** Owner chose each when asked; launcher name matches the design logo and the owner's own naming.
  **Issue:** #14, #18
- **Decision:** Brand icon SVGs committed into ArtSource/brand/ during planning, with the frog+brackets app icon reconstructed from the brand sheet's inline APP ICON SVG.
  **Why:** The design project's uploaded android-foreground files are the studio bracket mark without the frog — using them verbatim would ship the wrong icon; committing sources also removes execution's dependency on design-project access.
  **Issue:** #18
- **Decision:** Play Console story (#20) filed as needs-owner-action and started immediately after planning; owner has no developer account yet.
  **Why:** Identity verification takes days and gates M1's upload pipeline; account type (personal vs organization) affects Play's closed-testing requirements and is recorded in the story.
  **Issue:** #20
- **Decision:** No subtasks and no spikes in M0.
  **Why:** Each story's how is fully specified in its body or is self-evident; no unknown needs a prototype.

## /n8-plan M1 — 2026-08-27

- **Decision:** Unity Personal license → ULF activation path for CI (UNITY_LICENSE secret; repeatable activation workflow committed).
  **Why:** Owner confirmed Personal when asked; determines the whole activation story shape.
  **Issue:** #22
- **Decision:** One reusable workflow_call gate consumed by both PRs and the tag pipeline; PR AABs kept as short-retention artifacts for device testing.
  **Why:** Single definition of "green" per the CI conventions; artifact sideloading costs nothing and helps manual testing.
  **Issue:** #23, #25
- **Decision:** Play service account gets testing-track-only permission; production-release permission deliberately withheld until M7.
  **Why:** Least privilege — CI cannot accidentally ship to production before the closed-testing gate clears.
  **Issue:** #24
- **Decision:** Keystore secrets are set by the agent from the local material #19 generates (documented in signing.md); if absent at exec time, block rather than regenerate.
  **Why:** Regenerating an upload keystore mid-stream would fork the signing identity.
  **Issue:** #25
- **Decision:** GameCI actions pinned at v4 in story bodies with a standing instruction to verify current versions via context7/docs before writing workflows.
  **Why:** context7 unavailable this session (loads next session); stale-doc CI configs are a known failure mode.
  **Issue:** #22, #23, #25
