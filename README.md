# HonestFrogAcross

A 2D Unity game (Unity 6000.5.10f1, Universal Render Pipeline). Just initialized — gameplay, features, and docs will grow from here.

## Build

Open the project in Unity Hub with editor **6000.5.10f1**. Builds are made through the Unity editor (File → Build Profiles) until CI is set up.

## Test

Uses the Unity Test Framework. Run tests in the editor via **Window → General → Test Runner**, or headless:

```sh
Unity -batchmode -runTests -projectPath . -testPlatform EditMode -testResults results.xml
```

## Project management

This project uses the n8SDLC workflow — GitHub Issues are the plan. See `.n8/` and `CLAUDE.md`.
