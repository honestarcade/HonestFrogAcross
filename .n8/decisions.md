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

## Ad-hoc — 2026-08-28

- **Change:** Player-visible name is "Frog Across" (spaced) everywhere — launcher label, runtime constant, and the Play Console app entry the owner created. The design's logo lockup renders "FrogAcross" one-word with a two-tone split.
  **Why:** Owner decision during Play Console app creation ("update to use the spaced version anywhere the user sees").
  **Affects:** M4 (screens epic #8 — menu/loading logo copy must read "Frog Across" or restyle the lockup; its "copy matches the design" AC carries this override), M7 (store assets/listing already spaced). Repo/package/internal identifiers stay unspaced (com.honestarcade.frogacross is immutable).

## /n8-exec M1 — 2026-08-28

- **Decision:** unity-builder pinned at v5 (plan said v4; verified stale), unity-test-runner v4 (current), upload-google-play v1 (current). Betas (builder v6-beta, test-runner v5-beta) skipped.
  **Why:** The stories' own verify-before-writing rule; checked GameCI releases/docs live.
  **Issue:** #23, #25
- **Decision (within #22's discretion):** Activation approach superseded — no .alf workflow. Current GameCI procedure: local Unity Hub-generated .ulf → UNITY_LICENSE secret, plus UNITY_EMAIL/UNITY_PASSWORD. No .ulf exists on this machine (Hub entitlement licensing), so the owner step is Hub → Licenses → Add → free personal license; agent sets UNITY_LICENSE from the file; owner sets email/password secrets directly so credentials never transit the conversation. The .alf-artifact AC is vacuous under the superseded path; the proof AC (green headless run) is unchanged.
  **Issue:** #22
- **Decision:** Test jobs use customImage unityci/editor:ubuntu-6000.5.10f1-android-3 — our editor assemblies compile against UnityEditor.Android, absent from the base image. Image tag is a best-known pin; a pull failure on first CI run means adjusting the suffix to a published tag.
  **Issue:** #23
- **Decision:** Keystore GitHub secrets (FROG_KEYSTORE_B64/PASS/KEY_ALIAS/KEY_PASS) set by agent from the local M0 material per plan; release builds sign via unity-builder's androidKeystore* inputs (builder runs its own build command — our BuildScript's FROG_RELEASE gate remains the local-build gate; in CI the equivalent guarantee is structural: the ship job only signs, never uploads unsigned).
  **Issue:** #25
- **Decision:** versionCode = github.run_number + 100 (offset clears M0's local code 1); versionName from the tag via versioning: Custom.
  **Why:** Strict monotonicity by construction; discretion granted in #25.
  **Issue:** #25
- **Decision (Rule 3 — CI build blocker):** The analyzer gate failed the CI Android build on GameCI's own injected UnityBuilderAction script (error UNT0007 in vendor code). Fix: custom buildMethod `BuildScript.BuildCi` — our own gate-compliant entry point parsing the action's standard CLI args (verified against build.sh @ v5.0.0: -customBuildPath, -buildVersion, -androidVersionCode, androidKeystore* are passed regardless of buildMethod; with buildMethod set, no script injection occurs). Vendoring a fixed copy at their path was rejected: build.sh cp -R overwrites same-path files. Parser pinned by BuildCiArgsTests.
  **Why:** Weakening the gate (suppressing UNT0007 globally) was the only alternative and is explicitly forbidden by the no-weakened-gates rule.
  **Issue:** #23, #25
- **Decision (Rule 1 — own bug):** play-api-check's first 403 was our token missing the androidpublisher scope (gcloud default scopes); fixed in PR #34. The SECOND 403 (upload path) was a real permission gap — 'Release apps to testing tracks' unchecked — diagnosed from the owner's Console screenshot and fixed by the owner.
  **Issue:** #24
- **Decision:** #23's ruleset merge-block AC proven live on PR #33: deliberate red test → GitHub refused the merge ('base branch policy prohibits') → reverted → green → merged.
  **Issue:** #23
- **Decision:** First Play upload done manually by the owner through the Console UI (locally-built signed AAB, versionCode 1) after API uploads kept returning 'caller does not have permission' post-permission-fix — new apps gate their first bundle upload behind Play App Signing terms only a human can accept. All subsequent uploads are API-driven (proven: run 33174089981, versionCode 102).
  **Issue:** #25, #20
- **Decision:** Discovered work filed as #35 (IL2CPP symbols + R8 mapping uploads with releases) instead of fixed inline — quality-of-life, not ship-critical.
  **Issue:** #25

## /n8-plan * (M2–M7) — 2026-08-28

- **Decision (owner):** Riding = classic continuous drift; swipe-from-drift lands nearest column; edge drift kills.
  **Issue:** #42
- **Decision (owner, design override):** Gators — rideable surface is the BACK only, mouth-CLOSED only; open-mouth back kills; head/snout never rideable. Overrides the design canvases' "ride the eyes / snout kills" copy. Agent interpretation (ledgered in #49): the open-back rule applies continuously — mouth opening mid-ride kills at the open tick, mirroring turtle submerge.
  **Issue:** #49
- **Decision (owner):** Death is classic — bays persist, clock never pauses, infinite retries. Bike collision = 2s in-place stun (queued swipes dropped). Walkway = conveyor whose edge KILLS. Diagonal legality = landing square only. Swipe queue capped at 2. NO pause screen (Crossy-style; Android back = confirm-quit). Logo = spaced two-tone ("Frog" white + "Across" mint).
  **Issue:** #39 #41 #51 #52 #55
- **Assumptions (builder, logged not asked):** infinite lives (no lives UI exists in the design); board columns are per-level schema data; crashed rider wrecks are passable and safe; the clock is game-time (OS suspension adds nothing — a phone call must not cost a medal).
  **Issue:** #37 #38 #51 #60
- **Decision:** Invariant guards placed — invariant 2 → #38, invariant 4 → #39 (CLAUDE.md annotated as planned).
- **Decision:** #35 (symbols/mapping) triaged into M7 under epic #12. M7's closed test (#69) is flagged to start during M5/M6 so Google's 14-day clock runs in parallel with content work.
- **Decision:** One spike total (#45, sprite pipeline). Whole-project analysis: audit emphases recorded in M8 (performance primary; stability; sim-boundary coverage; cleanup; 508-lite incl. not-color-only medals; security posture). Skill candidates deferred: "sprite-pipeline" (after #45) and "level-author" (after #43/#44) via /n8-skill. Gap check: cross-milestone key links each have a named owning story (#59↔#65; #54↔#60).

## Ad-hoc — 2026-08-28

- **Change:** Tap-to-advance added: a clear tap counts as a forward swipe (one forward hop, same queue/legality rules); dead zone between tap ceiling and swipe threshold does nothing.
  **Why:** Owner scope decision during planning review — faster forward movement ergonomics.
  **Affects:** M2 #41 (amended directly — body/AC/tests updated pre-execution) and M4 #58 (controls copy rider commented). No other stories assume tap-does-nothing. Reconciled at source — reconciled by /n8-plan 2026-08-28.

## /n8-exec * — 2026-08-28

- **Decision (Rule 1):** Split the four PieceDef ScriptableObject classes into per-class files — Unity binds .asset files to scripts by filename, and the single-file layout produced assets with missing scripts (caught by the registry tests on first run).
  **Issue:** #37
- **Decision:** Levels live in Assets/Resources/Levels/*.json (TextAssets), not StreamingAssets as the plan wrote — reading StreamingAssets on Android requires UnityWebRequest, whose modules were removed under invariant 1. Levels remain pure JSON files; all M3/M5 story references to StreamingAssets/Levels read as Resources/Levels.
  **Issue:** #38 (affects #47-#54, #61-#64 path references)
- **Decision:** Generator emits provisional medals (2.0×/2.9×/4.2× the diagonal-free solver floor) so schema validity holds before #63's calibration; generation is validate-and-solve gated inline (no unproven level touches disk) and byte-deterministic per seed.
  **Issue:** #44
- **Decision:** Solver decision cadence: act-or-wait-6-ticks at cooldown-free states, dedupe on quantized (tick/6, row, x·4, riding, bays, stun) — dev board solves diagonal-free in 0.39s, making the 100-level CI batch cheap.
  **Issue:** #43
## Ad-hoc — 2026-08-28
- **Change:** Tap-region control scheme added as a settings option (#74, new M2 story): four zones (left/right thirds, middle-column top/bottom) move the character; no diagonals in this mode; Settings (#59) gains the scheme selector + a "Show regions" one-tap-dismiss preview; regions raycast BEHIND all UI. Consequences worked through the plan: gameplay HUD gains Restart + Main-menu buttons with confirm dialogs (#60), **all confirm dialogs freeze the sim/clock** ("no pause" now means "no dedicated pause screen" — freezing is consistent with OS-suspend), restart = fresh attempt (bays cleared, clock rearmed), solver gains a diagonal-free mode (#43) and medal calibration uses diagonal-free min-times (#63) so tap-region players can earn gold.
  **Why:** Owner scope additions during planning review (accessibility/ergonomics + quick restart).
  **Affects:** #74 (new), #43 #55 #58 #59 #60 #63 (amended via comments, pre-execution). Reconciled at source — reconciled by /n8-plan 2026-08-28.
- **Decision (Rule 1 — own miss):** The Unity.InputSystem asmdef reference edited during #41 was never staged (path-scoped git adds missed it) — local suites ran against the working tree while CI compiled without the reference, failing the M2 PR. Committed as the fix; lesson noted: watch the pre-PR `git status` for modified-but-unstaged files, not just untracked.
  **Issue:** #41

### M4 execution (2026-08-28)

- **#55 Shell is one code-built canvas with a screen dict + push/back stack.** No scene-per-screen: screens are built once at boot and toggled active, matching the code-built (reviewable, idempotent) UI approach from M3. Android back = Escape via the new Input System; at the menu root back is a no-op so the OS handles backgrounding.
- **#56 LevelCatalog falls back to the dev chain until M5.** Canonical chain is `level-###` files in Resources/Levels; while none exist the seven dev slices form the playable chain. Swapping in M5's 100 levels is a pure file drop (invariant 2) — no code edit.
- **#58 Copy loader is a flat line-based key scan, not a JSON DOM.** JsonUtility can't deserialize dictionaries; a full parser dependency would breach the no-deps posture. copy.json is constrained to one flat "key": "value" pair per line — documented by the tests that pin every used key.
- **#59/#60 Single wipe path: `DataWipe.WipeAll()`.** Reset-all previously would have enumerated stores at the call site; extracted to Services so the "wipes everything" AC is enforceable by one test and future stores have one place to register. (Rule 2 — correctness of the wipe guarantee.)
- **#60 Overlay `Show` gained optional `newBest`/`prevBest` params** instead of a new overload — non-catalog (dev) levels pass defaults and show no best line.
- **Test isolation: scene-loading PlayMode tests must unload their scenes.** The Game scene's new HUD corner buttons leaked into UiGuardTests' corner-touch assertion. Added `SceneCleanup.UnloadAll()` UnityTearDown to every scene-loading test class rather than weakening the UiGuard test.
- **Editor asmdef now references Unity.InputSystem** — SceneBuilder places `InputSystemUIInputModule` on both scenes' EventSystems (new Input System only, activeInputHandler=1).
- **Analyzer gate catches of my own M4 code, fixed properly:** UNT0038 (uncached WaitForSeconds — AppShell boot beat and all PlayMode waits now static readonly), CS0618 FindFirstObjectByType → FindAnyObjectByType.

### M5 execution (2026-08-28)

- **#62 path drift adjusted inline:** story says StreamingAssets/Levels; levels ship in Assets/Resources/Levels — StreamingAssets on Android requires UnityWebRequest, whose modules were removed under invariant 1 (M3 decision). Implementation detail only; solver-proof intent unchanged.
- **#61 CI-budget split for the batch AC:** the per-CI-run tests are file-based (schema, introduction order, pressure trend, hashes) — regenerating 100 levels per CI run would add ~3 min of solving to every job. The "all validation+solver green in one batch" AC is evidenced by the pipeline log and the committed solvability fixture (100 entries, all solved). Full re-solve stays a one-command local step (67s measured).
- **#61 monotonicity bounds (discretion):** proxy = rows×speed×(1+bays), decade means with a ≥0.90 local-dip bound and d10 > d1×2; solver-floor trend asserted at phase thirds (1–30 / 31–60 / 61–100 rising, late > early×1.5) — train/gator wait-cycles make per-decade min-times noisy (measured dips to 0.93 with legitimate curve shape).
- **#63 medal factors (delegated):** gold = floor × 4.5 (L1–10) / 3.4 / 2.9 / 2.4 / 2.0 (L41+); silver = gold×1.45; bronze = gold×2.1. Taper exists because early boards are short — flat 2.0× gave L1 gold of 2.4s. 4.0→4.5 bump lifted L2 gold (11.5s) over the 12s anchor floor. L2–5 golds now 12.9–15.9s against the design's 24s chip ballpark; L1 is the documented exception (see next).
- **#64 L1 straight-line via generation gate, not hand-editing:** added maxSolverMoves to curve bands — first-hop band rejects candidates the solver can't finish in ≤7 moves. Hand-editing JSON would be exactly the drift the pipeline exists to prevent; the gate makes the property regeneration-stable.
- **#64 tracks weight halved in its intro band:** double-weighted tracks made decade 4 min-times spike 60% above decade 5 (train warn-cycles are the dominant wait cost).
- **#62 RefreshHashes (sim-invisible edits only):** medal calibration changes bytes but not sim behavior; re-hashing without re-solving keeps the pipeline at one solve pass. Any sim-visible edit path goes through RegenerateFixture.
- **ContentLock hash:** BitConverter over SHA256 (Convert.ToHexString absent from Unity's .NET profile).

### M6 execution (2026-08-28)

- **#66 blocked (needs-owner-action):** owner audio files not yet supplied. Concrete asset list posted on the issue (13 SFX + 1–2 music loops + license terms per file). Engine (#65) complete — swap is a file drop; gameplay-music question resolves implicitly by what the owner provides.

### M7 execution (2026-08-28)

- **#35 symbols API:** EditorUserBuildSettings.androidCreateSymbols is obsolete in 6000.5 — used UnityEditor.Android.UserBuildSettings.DebugSymbols (level=SymbolTable, format=Zip). SymbolTable (public) level: readable Play crash stacks at a fraction of full-debug size.
- **#35 R8 mapping:** enabled minifyRelease (also shrinks the AAB); Unity buries mapping.txt in Library/Bee — BuildScript copies the freshest one next to the AAB post-build; release.yml resolves both artifacts and passes them to upload-google-play. Proof deferred to the v0.2.0 tag run (also seeds #69's closed track).
- **#67 screenshots are editor captures of the real game** (real level data, shipped BoardView renderer, player state = solver proof-line replayed 2/3): the "release build" key link is satisfiable more literally by on-device captures once v0.2.0 is installed — offered to the owner at verify, not blocking the draft listing.
- **#67 surfaced two real BoardView bugs, fixed (Rule 1 + regression test):** (a) bay-fill character sprite was unscaled — raw sprites are ~4 world units wide, so a filled bay rendered a giant frog (BayFill_FitsInsideItsCell pins the fix); (b) side covers used the tiled bank texture (banding) and were only 14 units wide — wide phones would see board internals; now flat #16321F, 40 units.
- **#68 privacy policy hosting (delegated):** GitHub Pages from main/docs (enabled 2026-08-28) — https://honestarcade.github.io/HonestFrogAcross/privacy. The repo is public and open source is part of the store pitch, so Pages fits; no separate infrastructure.
- **#69 promotion runs in CI, not locally:** the Play service-account key exists only as a GitHub secret (by design — no local key material). Added play-promote.yml (workflow_dispatch, internal→alpha default) using the androidpublisher REST API; the SA remains testing-tracks-only, so production promotion (#70) stays a human Console act.
- **#69 promote saga (fixed forward):** google-github-actions/auth mints via the IAM Credentials API → 403 for this SA; switched to gcloud local JWT (the play-api-check pattern). Then Play refused a completed alpha release — "Only releases with status draft may be created on draft app" — so the workflow now falls back to a draft release and says so; once the listing publishes, completed works. v0.2.0 (code 102) is on the closed track as a draft.

## Ad-hoc — Studio screen adopts the Honest Sudoku design (2026-08-29)

- **What changed:** the owner amended the "Honest Arcade" (studio) screen: the Frog Across design's version was wrong; the screen must be rebuilt from the Honest Sudoku design (claude.ai/design project 9e9471c9-5231-4fd8-9889-066345073295, Honest Sudoku.dc.html) adapted to landscape — and this studio content is intended as the standard across ALL Honest Arcade apps.
- **Why:** cross-app brand consistency; stated during M4–M7 verification UAT.
- **Affected:** #58's shipped studio screen (amended by #91, filed in M4); any future app scaffolding that copies this screen.

### M4–M7 verification (2026-08-29)

- **Fresh gate on main @ 752ee67:** EditMode 139/139, PlayMode 16/16; all 39 story-promised tests confirmed present by name and green.
- **Golden independently re-derived:** full content pipeline re-run produced byte-identical levels + fixture + curve asset (git diff 0 lines); Python-only SHA256 + medal recomputation agreed (4 bronze values differ 0.1s at .x5 rounding boundaries — banker's rounding artifact, not drift).
- **Owner device UAT (v0.2.0/102):** data layer clean across the board; visual layer failed → bugs #84–#90 + amendment #91. Teaching sequence played cold to L7. #35's Console warnings confirmed gone by the owner.
- **M5 closed** (epic #10 closed; epic #9 also closed — fully proven). **M4 open** (confirmed bugs), **M6/M7 open** (owner-gated stories). M2/M3 remain executed-but-unverified; M3 now carries confirmed bug #86.
- **Account correction:** Play developer account is ntpond@gmail.com; support@honestarcade.app is only the publisher contact email (memory updated — earlier notes had it backwards).

### Fix pass for M4 verification bugs (2026-08-29)

- **#84 root cause was NOT a missing scaler** — UiKit.Canvas already carried a correct ScaleWithScreenSize CanvasScaler. The layouts themselves only occupied a small centered fraction of the 1920×1080 reference (the menu spanned ~900×400 of it). Fix: a density/relayout pass across menu, loading, levels, settings, character, static screens, HUD, and dialogs (~1.4–1.5× type, screen-filling placement). The bug report's proposed fix was wrong; the report's symptom was right.
- **New visual-review harness:** UiCaptureTests renders every shell screen + the completion overlay at the 1920×1080 reference into Builds/ui/*.png (canvases through an RT camera — batchmode-safe). It caught the UiKit.Lockup wrap/overlap at large sizes immediately and doubles as an every-screen-builds smoke test.
- **#86:** the Lane design component's `bare` prop only stripped props (trees/benches/bushes) — vehicles/logs/gators/riders/pads/train stayed animated into the "bare" strip renders. Extended `bare` to gate every moving entity in our committed component copy; re-extract changed exactly the 8 lane strips (273 other renders byte-identical — verified).
- **#87:** camera roll removed entirely (owner ruling overrides the design-derived -8°); GameBootstrap.FitCamera frames each bound level (rows/2 + 0.4) — the fixed 6.2 ortho was sized for the 12-row dev board, shrinking teaching boards.
- **#88/#89/#90/#85:** overlay rebuilt on UiKit.Canvas (its raw canvas was the one true missing-scaler case) with "Level N Complete"; AppShell.RefreshDataScreens() after wipe; previews use the back-view frame + capitalized names; levels grid relayout with `<` `>` glyph buttons and a SwipePager (drag left = next).
- **#91:** studio screen rebuilt from the Honest Sudoku design in landscape — 7 promises verbatim (the no-accounts/no-permissions/offline caveat lines shortened to their Frog-Across-true forms), support card, 7 chips, links; all copy data-driven through copy.json (8 new keys).

### Device UAT round 2 — fix pass (2026-08-29)

- **The "tiny UI" root cause was the canvas scaler's match mode, not a missing scaler.** At matchWidthOrHeight=0.5 a 21:9 panel produced a ~1662-unit-wide canvas, so layouts authored against the 1920 reference overflowed and were masked away (the levels grid rendered empty). Landscape-only ⇒ match HEIGHT: canvas is always 1080 tall and ≥1920 wide.
- **ScrollArea returns a top-anchored origin node.** UGUI children anchor to their parent's centre, so on a 1160-tall page everything landed ~580 units low. The zero-sized origin lets screens author plain "-Y from the top".
- **Board tilt reinstated.** My M4-verify fix removed the -8° roll on the owner's "tiny and tilted" report; the design (reference line 135) draws the board rolled AND oversized so it bleeds off every edge. The real defect was framing. Camera now sizes so every board corner sits inside the ROLLED frame (cols/2·sinθ + rows/2·cosθ + margin); strips run full-bleed; aprons repeat the bank surface; objects in the sim's existing wrap margin are drawn rather than culled at ±1 cell.
- **Lane overlap was a wrap-seam bug, not a density bug.** Spacing that doesn't divide the row's loop drops the wrapped instance onto the first. Objects now tile the loop at an exact pitch. Guard: LaneGeometry.SmallestGap over all shipped levels, plus a generator gate.
- **Exact pitch alone flattened the difficulty curve** (floor thirds 13.5/11.4/13.4): with every row starting at slot 0 the lanes arrived in lockstep and the solver could only wait. Restored a per-row random phase — spacing stays exact, rows desynchronise (9.0/10.2/15.4, rising). Teaching gold factor 4.5→4.8 to keep the 24s early anchor after regeneration.
- **Character back button "didn't work"** because the first character cell (290×680 at x=-762) was created after the button and drew over it. The shared Header now renders last on every screen.
- **Pagination removed from Levels** (owner ruling): all 100 levels on one scrolling surface via GridLayoutGroup, so the column count adapts to the panel instead of assuming ten.
- **Overlay "Levels" → "Main Menu"** with AppShell.SkipBoot, so returning from a level doesn't replay the loading beat.
- **Version string** now comes from Application.version (was a hardcoded "1.0"); the baseline bundleVersion moved to 0.4.0 and PlayerSettingsTests pins the shape, not a frozen literal.
- **Tap regions 33% → 20%** side bands (owner ruling): forward/back get the room.
- **UI sprite importer rule**: Resources/UI imports as a single sprite — the project's sheet defaults had sliced the logo, so the menu drew one corner of the mark.

### Device UAT round 3 — fix pass (2026-08-29)

- **Type scale added to UiKit** (Display/Title/Heading/Body/Caption/Micro). The design's own sizes — 12–16px inside a 958-wide mock — scale to ~26pt on our 1920 canvas, which the owner reports as unreadable on a 6.9" phone. These are roughly double that; screens now use the names, never raw numbers, so "make it bigger" is one edit rather than forty.
- **Fixed offsets replaced with layout groups (UiKit.Column / UiKit.Row).** Three separate overlap reports (About's rules, Settings' CONTROLS vs the Interface row, Show-regions vs the stored-data card) were all the same bug: copy that wraps to a different number of lines on a different aspect while the next element sits at a hardcoded Y. Lists of copy now flow at their natural heights.
- **The double-press back button** was screens rebuilding themselves with RebuildScreen + Push, which stacked a second copy — Back popped the duplicate and looked dead. Added AppShell.Replace(); Character and Settings use it. Regression test drives both paths.
- **Levels grid**: GridLayoutGroup with FixedColumnCount=10 plus GridFitter, which derives cell size from the surface it actually got (a fixed cell size gave 11 columns on one aspect and 12 on another). The ScrollArea origin node needed ignoreLayout — it had been dealt the first cell, leaving an empty slot before level 1.
- **Medals are now the disc behind the number** (owner ruling), one size on every cell, sized for three digits, with an outlined numeral so it reads on gold. The old corner dot was too small to notice.
- **Logo**: mark to the RIGHT of the wordmark with the byline underneath (owner's explicit call; the design has the mark on the left — noted here in case they want it flipped).
- **Region preview** dimmed to 0.10 scrim + 0.10 zones (owner asked for "something really low").
- **About/Gameplay version** now derives from Application.version and LevelCatalog.Count instead of the hardcoded "v1.0 · 100 LEVELS" copy string.
