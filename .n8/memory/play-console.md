---
name: play-console
description: Google Play Console account/app identifiers, constraints, and service-account records
metadata:
  type: project
---

# Google Play Console

- **Developer account:** personal type, owned by `support@honestarcade.app` (verified 2026-08-28; $25 fee paid 2026-08-27). Developer name shown publicly: Honest Arcade.
- **App entry:** "Frog Across" (spaced — the player-visible name everywhere, per the 2026-08-28 ad-hoc ledger entry) · Game · Free (permanent) · package `com.honestarcade.frogacross` (immutable).
- **Play App Signing:** enrollment completes automatically at the first AAB upload (M1 pipeline, #25) using the upload key from [[android-signing]].
- **Personal-account constraint (affects M7):** production access requires a closed test with ≥12 testers continuously opted-in for 14 days. Recorded in the M7 milestone description; recruit testers at M7 start.
- **Service account for CI (#24):** not yet created — record its identity here when it exists.
- Never store credentials in this file — identifiers and constraints only.
