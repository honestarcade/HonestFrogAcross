#!/bin/sh
# Local device preview: renders every screen (1920x1080 reference AND the
# S26-Ultra-class 3120x1440 panel) plus the in-game board, without touching a
# phone. Close the Unity editor first (batchmode needs the project lock).
set -e
cd "$(dirname "$0")/.."
U="/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity"
"$U" -batchmode -projectPath . -buildTarget Android -runTests -testPlatform PlayMode \
  -testFilter "FrogAcross.Tests.PlayMode.UiCaptureTests" \
  -testResults /tmp/frog-preview-results.xml -logFile /tmp/frog-preview.log
echo "Renders in Builds/ui (reference) and Builds/ui/device (S26 Ultra panel):"
ls Builds/ui/device
open Builds/ui/device 2>/dev/null || true
