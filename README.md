# HonestFrogAcross

A 2D Unity game (Unity 6000.5.10f1, Universal Render Pipeline). Just initialized — gameplay, features, and docs will grow from here.

## Build

Open the project in Unity Hub with editor **6000.5.10f1** (Android module required). One-command AAB:

```sh
UNITY="/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -projectPath . -buildTarget Android -executeMethod FrogAcross.Editor.Build.BuildScript.BuildAndroidAab -logFile build.log
```

Output: `Builds/frogacross.aab` (unsigned without env). Signed builds read `FROG_KEYSTORE_PATH`, `FROG_KEYSTORE_PASS`, `FROG_KEY_ALIAS`, `FROG_KEY_PASS`; `FROG_RELEASE=1` refuses to build unsigned. In-editor: **FrogAcross → Build → Android AAB**.

## Test

Unity Test Framework, EditMode + PlayMode. In-editor: **Window → General → Test Runner**. Headless:

```sh
"$UNITY" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults editmode-results.xml -logFile editmode.log
"$UNITY" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults playmode-results.xml -logFile playmode.log
```

The suite includes the project's invariant guards (no-network/no-ads package and settings checks) and pinned platform settings — a red guard test means a project invariant is being breached, not a flaky test.

## Project management

This project uses the n8SDLC workflow — GitHub Issues are the plan. See `.n8/` and `CLAUDE.md`.
