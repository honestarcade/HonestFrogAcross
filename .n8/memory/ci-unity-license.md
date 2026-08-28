---
name: ci-unity-license
description: How CI authenticates Unity (Personal license), failure signatures, re-activation runbook
metadata:
  type: project
---

# Unity licensing in CI

- **License:** Unity Personal, owned by the owner's Unity account. Three repo secrets: `UNITY_LICENSE` (full contents of the `.ulf` file), `UNITY_EMAIL`, `UNITY_PASSWORD` (the Unity account credentials — GameCI requires all three for Personal licenses).
- **Where the .ulf comes from:** Unity Hub → Settings → Licenses → Add → "Get a free personal license" writes `/Library/Application Support/Unity/Unity_lic.ulf` on macOS. Hub's newer entitlement licensing does NOT create this file until that step is done explicitly, even when the editor runs fine locally.
- **Procedure history:** the old `.alf` activation-file workflow (game-ci/unity-request-activation-file) is superseded — do not resurrect it; current GameCI docs use the local-.ulf method (checked 2026-08-28).
- **Failure signatures:** log lines like `[Licensing::Module] Error: Access token is unavailable` / `No valid Unity license` in the test/build step → license secret invalid or expired.
- **Re-activation:** repeat the Hub step above (new .ulf), then `gh secret set UNITY_LICENSE < "/Library/Application Support/Unity/Unity_lic.ulf"`. Email/password secrets only change if the Unity account does.
