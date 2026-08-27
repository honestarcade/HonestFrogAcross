---
name: android-signing
description: Where the Android upload keystore lives, how builds consume it, rotation procedure
metadata:
  type: project
---

# Android upload signing

- **Keystore:** `~/HonestArcadeApps/secrets/frogacross-upload.keystore` (outside the repo; `*.keystore` is gitignored). Alias `upload`, RSA 2048, validity 10000 days.
- **Passwords:** generated at creation and written to `~/HonestArcadeApps/secrets/frogacross-signing-credentials.txt` (chmod 600). **Owner action: move both passwords into your password manager and delete that file.** The file doubles as an env template.
- **Public upload certificate:** `ArtSource/signing/upload_certificate.pem` (committed — public by nature; used for Play App Signing enrollment, #20).
- **Build consumption:** `BuildScript.BuildAndroidAab` reads `FROG_KEYSTORE_PATH`, `FROG_KEYSTORE_PASS`, `FROG_KEY_ALIAS`, `FROG_KEY_PASS` at build time only and clears them from editor state afterwards; `FROG_RELEASE=1` makes missing signing a hard build failure. Values are never persisted to ProjectSettings (SigningPersistenceTests guards this).
- **CI (M1, #25):** set the same values as GitHub secrets (`FROG_KEYSTORE_B64` = `base64 < the keystore`, plus the three strings). If this machine's material is lost before that happens, DO NOT regenerate silently — Play App Signing ties the app to this upload cert once enrolled; use Play Console's upload-key reset flow instead.
- **Rotation:** Play Console → Setup → App integrity → request upload key reset; generate a new keystore per above; re-export the cert.
