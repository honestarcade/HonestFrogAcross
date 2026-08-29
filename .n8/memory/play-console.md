---
name: play-console
description: Google Play Console account/app identifiers, constraints, and service-account records
metadata:
  type: project
---

# Google Play Console

- **Developer account:** personal type, **signed in as `ntpond@gmail.com`** (the Google account that owns the Console and is on the owner's phone). `support@honestarcade.app` is only the publisher/contact email entered on the account — do NOT tell the owner to switch accounts to it; the Console lives under ntpond@gmail.com. (Corrected 2026-08-28 during M4–M7 verify; verified account 2026-08-28; $25 fee paid 2026-08-27.) Developer name shown publicly: Honest Arcade.
- **App entry:** "Frog Across" (spaced — the player-visible name everywhere, per the 2026-08-28 ad-hoc ledger entry) · Game · Free (permanent) · package `com.honestarcade.frogacross` (immutable).
- **Play App Signing:** enrollment completes automatically at the first AAB upload (M1 pipeline, #25) using the upload key from [[android-signing]].
- **Personal-account constraint (affects M7):** production access requires a closed test with ≥12 testers continuously opted-in for 14 days. Recorded in the M7 milestone description; recruit testers at M7 start.
- **Service account for CI (#24):** not yet created — record its identity here when it exists.
- Never store credentials in this file — identifiers and constraints only.

## Data-safety form — prepared answers (#68, drafted 2026-08-28)

Ground truth: the AAB contains no INTERNET permission (enforced by
NoNetworkGuardTests + the final-AAB byte scan on every build). The app cannot
transmit anything. Answer the form exactly like this:

1. "Does your app collect or share any of the required user data types?" → **No**
2. "Is all of the user data collected by your app encrypted in transit?" → question does not appear once "No" is selected
3. "Do you provide a way for users to request that their data is deleted?" → question does not appear once "No" is selected
4. Preview shows: "No data collected · No data shared" → **Submit**

Privacy policy URL (required even for no-data apps):
**https://honestarcade.github.io/HonestFrogAcross/privacy**
(GitHub Pages from main/docs — enabled 2026-08-28; content: no data collected,
no network access, contact support@honestarcade.app)

## Content rating (IARC) — prepared answers (#68)

- Category: **Game**
- Email: support@honestarcade.app
- Violence: cartoon character can be "defeated" by hazards (vehicle contact,
  water) with a brief cartoon splat/sink — answer **Yes** to "mild cartoon or
  fantasy violence", **No** to realistic violence, gore, or violence toward
  defenseless characters.
- Sexuality / nudity: **No**
- Language / profanity: **No**
- Controlled substances (drugs/alcohol/tobacco): **No**
- Gambling (real or simulated): **No**
- User interaction / online features: **No** (no chat, no UGC, no multiplayer)
- Shares location: **No**
- Digital purchases: **No**
- Expected outcome: **Everyone / PEGI 3** (mild cartoon peril)
- Record the issued rating certificate id here when it arrives: ☐

## Closed test (#69) — machinery + monitoring plan

- Track: Play Console "Closed testing" (API track `alpha`), build promoted
  from internal — same AAB lineage, no side-channel builds.
- Owner recruits ≥12 testers (friends/family, Android), enters their emails
  in the track's email list, distributes the opt-in link (Console shows it
  under Closed testing → Testers → "Copy link").
- Gate math: production access needs **≥12 testers opted in continuously for
  14 days**. A dip below 12 RESTARTS the day it dips. Aim for 15+ so one
  dropout doesn't reset the clock.
- Monitoring cadence: owner checks the Console tester count **daily**
  (calendar reminder recommended); log here:
  - Start date (first day ≥12 opted in): ☐
  - Daily count log: ☐

## Closed-track status (2026-08-28)

- v0.2.0 (versionCode 102) promoted internal → alpha (closed testing) via
  play-promote.yml run 33219293891 — as a **draft** release: Play refuses
  completed releases outside internal while the app is a draft app (listing
  unpublished). After #67/#68 publish the listing, re-run the workflow (or
  roll out in Console) to make it a live closed-test release.
- Promote workflow learnings: google-github-actions/auth needs the IAM
  Credentials API (SA gets 403) — mint tokens with gcloud locally instead;
  the API track id for Closed testing is `alpha`.
