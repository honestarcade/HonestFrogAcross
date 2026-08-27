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

## /n8-exec M0 — 2026-08-27

- **Decision:** Executed #15 before #14 (both unblocked) so build/settings code lands inside proper assemblies.
  **Why:** #14's editor code and tests need the asmdef structure; reverse order would compile into Assembly-CSharp then move.
  **Issue:** #15, #14
- **Decision:** Analyzer severity escalated via `Assets/Default.ruleset` with `IncludeAll Action="Error"` (all analyzer AND compiler diagnostics are errors for our code), rather than csc.rsp `-warnaserror`.
  **Why:** Ruleset is the Unity-documented project-wide mechanism scoped to Assets assemblies (packages unaffected); IncludeAll also delivers warnings-as-errors, which the invariant philosophy calls for. Discretion granted in #16.
  **Issue:** #16
- **Decision:** Analyzer DLL's .meta handcrafted (all platforms disabled, validateReferences off, RoslynAnalyzer label) instead of letting Unity generate-then-configure.
  **Why:** First-import as a normal managed plugin would produce reference-validation noise against Microsoft.CodeAnalysis; the pre-made meta makes the first import correct.
  **Issue:** #16
- **Decision:** `/usr/bin/keytool` is macOS's no-Java stub; used Unity's bundled OpenJDK keytool (AndroidPlayer/OpenJDK/bin) for keystore generation and cert export.
  **Why:** No system JDK installed; Unity's is version-matched to its Android toolchain. (Blocker-class tool gap resolved inline — Rule 3.)
  **Issue:** #19
- **Decision:** Keystore + generated passwords stored at ~/HonestArcadeApps/secrets/ (chmod 700/600) with an explicit owner instruction to move passwords to a password manager and delete the file.
  **Why:** Agent cannot write to the owner's password manager; this is the most secure locally-achievable handoff and is documented in .n8/memory/signing.md.
  **Issue:** #19
- **Decision:** Icon rasterizations: adaptive layers 432px, legacy/round 192px, via rsvg-convert (present via Homebrew).
  **Why:** 432px is the adaptive-icon layer size (108dp @ xxxhdpi); 192px covers xxxhdpi legacy. Discretion granted in #18.
  **Issue:** #18
- **Decision:** Dropped Round/Legacy launcher-icon kinds; adaptive-only.
  **Why:** Unity 6000.5 deprecates AndroidPlatformIconKind.Round/Legacy (CS0618, an error under our ruleset — the gate caught it on first compile), and minSdk 26 makes adaptive icons universal. Story #18's legacy AC line is satisfied vacuously; noted in its completion comment.
  **Issue:** #18
- **Decision:** Analyzer wiring is `Assets/csc.rsp` (`-analyzer:` + `-ruleset:`), not the RoslynAnalyzer label alone; ruleset explicitly pins all 66 UNT/USP rules to Error (IDs extracted from the DLL), because `IncludeAll` does not lift default-Info diagnostics.
  **Why:** Empirical: on Unity 6000.5, the labeled DLL (Assets root or subfolder) never reached the Csc invocation for asmdef assemblies, and with the analyzer attached UNT0001 stayed silent until explicitly pinned — verified end-to-end by the synthesized empty-Update violation failing the build (error UNT0001, exit 1). Label kept for tooling compatibility.
  **Issue:** #16
- **Decision:** First settings pass crashed (SIGSEGV in Burst compiler thread during the initial Android platform-switch import); simply re-ran after import completed rather than changing configuration.
  **Why:** One-off native crash during cold import; subsequent passes clean. Watch for recurrence in CI (M1) — if it recurs there, GameCI's Library cache mitigates the same cold-import path.
  **Issue:** #14
- **Decision (Rule 2 — invariant enforcement inside story scope):** Removed the five unused `com.unity.modules.unitywebrequest*` built-in modules from Packages/manifest.json after ManifestGuard failed the first real release build (android.permission.INTERNET in the merged manifest, introduced by the template's UnityWebRequest modules under Internet Access=Auto).
  **Why:** The guard exists to force exactly this cleanup; the game uses no networking by invariant 1. If a future package re-requires these modules as dependencies, the guard will resurface the conversation.
  **Issue:** #17, #14
- **Decision (Rule 2, continued):** Also removed `com.unity.modules.unityanalytics` and `com.unity.modules.video` (the packages keeping unitywebrequest resolved; nothing else depends on either), and strengthened the guard blocklist fragment from "com.unity.analytics" to "analytics" — the original fragment did not match the built-in `unityanalytics` module, a real blocklist gap the build exposed.
  **Why:** The analytics engine module is itself an invariant-1 violation; video is unused in a 2D sprite game (re-adding it later is a normal package operation that will re-trigger this conversation via the guard if it drags network modules back in).
  **Issue:** #17
- **Decision (guard redesign, within #17's discretion):** Unity 6's engine template injects android.permission.INTERNET into the unityLibrary manifest unconditionally (persisted even with every network-capable module removed), so presence there is noise, not signal. Enforcement moved to a custom launcher manifest (Assets/Plugins/Android/LauncherManifest.xml) with tools:node="remove" for INTERNET and ACCESS_NETWORK_STATE — OS-level denial, the strongest form of the invariant — and ManifestGuard now verifies (a) the directives reach the generated launcher module and (b) the FINAL AAB's merged manifest carries no network permission strings.
  **Why:** The shipped artifact is the honest verification point; the intermediate library manifest is not. Side effect: Wi-Fi profiler connections won't work on device builds (documented in the manifest comment); profile in-editor instead.
  **Issue:** #17
- **Decision:** FROG_KEY_PASS set equal to FROG_KEYSTORE_PASS in the credentials file (with an explanatory note), keystore kept as PKCS12.
  **Why:** keytool's default PKCS12 format uses a single password for store and key — the separately-generated key password was silently ignored at creation and Gradle signing failed with "final block not properly padded". PKCS12 is the modern format; JKS (which supports split passwords) is deprecated.
  **Issue:** #19
