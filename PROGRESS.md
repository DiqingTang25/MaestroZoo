# MaestroZoo Progress

Last updated: 2026-07-09

## Active Split

- Claude Code: Rokid input, gesture calibration, recognition thresholds, core judgement.
- Codex: UI, scene wiring, Build Settings, demo readiness, resource gap checks.

## Codex Status

- Removed keyboard gameplay from the active scene and generated scene path.
- Added `Assets/_Project/Scenes/Main.unity` to Build Settings.
- Wired `RokidDebugPanel` onto `GameDirector` for on-device tracking status.
- Wired `GestureFeedbackDisplay` onto `GameDirector` for visible recognized gesture flashes.
- Reused Claude's Rokid native debug fields instead of duplicating another status HUD.
- Added `Main.unity` to `ProjectSettings/EditorBuildSettings.asset`.
- Added runtime placeholder beat generation in `ChartPlayer` when no BGM clip is assigned.
- Verified with Unity batch compile: no C# errors or warnings.

## Handoff Notes

- Main input path is `RokidNativeGestureInput` through `GesEventInput.OnProcessGesData`.
- Fallback path remains `RokidHandGestureInput` through `XRHandSubsystem`.
- No keyboard gameplay should be reintroduced for the competition build.
- `Main.unity` is currently touched by Codex; coordinate before editing it from another agent.
- `origin/main` currently points at the initial commit; local `master` contains Claude's latest input commits.
