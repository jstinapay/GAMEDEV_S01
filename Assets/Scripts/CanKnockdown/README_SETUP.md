# Carnival Can Knockdown — Setup & Test (3 balls / 6 cans, slingshot)

## 1. Open the project
- Unity `6000.6.0f1`, URP. On first open, Unity installs `com.unity.textmeshpro 3.0.9`
  (added to `Packages/manifest.json` for the TMP UI).

## 2. Build the booth (one click)
- Recommended: menu **CanKnockdown / New CanKnockdown Scene (clean, no Laser Corridor)**.
  This creates an empty scene, builds the booth + wooden slingshot, deactivates any
  Laser Corridor leftovers, and saves `Assets/Scenes/CanKnockdown.unity`. Then press Play.
- Or, in your current scene: **CanKnockdown / Build Carnival Booth (3 balls, 6 cans)**.
  At Play, `EnsureLevelInPlace()` auto-deactivates `LaserCorridorLevel`, the corridor
  `GameManager`/`Player`/HUDs, and the FPS `CameraFollow` so they can't fight for
  the camera, cursor, or UI. Your `LaserCorridor.unity` file on disk is untouched
  unless you save over it.
- Blank-scene fallback: `CanKnockdownManager.Awake` self-builds the level at Play,
  so even an empty scene with only `CanKnockdownManager` works.

## 3. How to shoot (slingshot)
- A ball waits in the pouch. Click the ball to grab it, then drag BACK to stretch
  the bands. Release fires the opposite way, toward the cans. The trajectory arc
  + power bar show while dragging and match the release exactly.
- Longer pull = faster shot (9–26 m/s). Short pulls snap back without consuming
  the ball. While a ball is flying (or physics is settling) you cannot grab another.

## 4. Stack stability (cans hold formation)
- Fresh cans spawn parked asleep (`sleepInitially`), so the pyramid can't jitter
  apart — only a real impact wakes them. Wider spacing (0.34 vs 0.3 diameter),
  per-row lift epsilon, grippy low-bounce can material, and asleep-on-reset
  keep the 3-2-1 formation until hit.

## 5. Inspector tuning (defaults)
- `CanKnockdownManager`: `totalThrows 3`, `pointsPerCan 100`, `fullClearBonus 500`,
  `settleTime 2.5`, `maxExtraWait 2`, `sleepThreshold 0.2`.
- `ThrowController`: `maxPull 1.3`, `minPull 0.18`, `minLaunchSpeed 9`,
  `maxLaunchSpeed 26`, `throwCooldown 0.8`, trajectory preview on.
- `CanStackManager`: `canMass 0.4`, `canDrag 0.05`, `canAngularDrag 0.3`,
  continuous collision, interpolate.
- `CanController` (per can): `tiltThreshold 35°`, auto fall threshold.
- `CanKnockdownAudio`: clips optional — synthetic tones play when empty;
  `hitThrottle 0.06` stops pileup spam.

## 6. Play test checklist
1. Press Play → Main Menu shows (`CAN KNOCKDOWN`, PLAY/INSTRUCTIONS/QUIT).
2. PLAY → HUD (`SCORE: 0`, `BALLS: 3/3`), ball loaded in the slingshot pouch,
   bands drawn to the fork tips, no crosshair.
3. Cans stand still in 3-2-1 before the first shot (asleep, no drift).
4. Drag back → arc + power bar appear. Release → ball flies via `AddForce`,
   cans tumble with physics only.
5. Wait ~2.5 s → score notification (`+100`, `+200`…), each can scores once.
6. Use all 3 balls → `ROUND COMPLETE!` with final score, cans hit, accuracy,
   perfect-round message when 6/6. `PLAY AGAIN (R)` / `MAIN MENU` work.
7. PLAY AGAIN → cans snap back asleep, ball reloads, score/throws reset.
8. Check: no tunneling, no double-scoring, no console errors.

## 7. Balloon Pop Rush (second mode)
- Main menu has CAN KNOCKDOWN + BALLOON POP RUSH (60s, 12 balls, moving targets).
- Same slingshot/balls/physics; balloons pop on trigger contact, once each.
- Types: red/yellow/blue standard 100 (moderate), gold 300 (fast, emissive, late-round
  odds up), teal bonus 200 +1 ball (slow, white band). Streak bonus +50 every 3rd
  consecutive hitting ball. Accuracy = hitting balls / fired balls.
- Round ends on timer or ammo-out (live balls finish first). R replays results,
  MODE SELECT returns to menu. Knockdown mode behavior unchanged.
- New scripts: `BalloonType`, `BalloonController`, `BalloonSpawner`, `BalloonRushManager`,
  `BalloonRushUI`. Rebuild the booth menu to get the mode button on old scenes
  (spawner/manager/UI self-create at Play regardless).

## 8. Presentation (visual polish)
- Stylized night-carnival booth: stage discs, striped counter + gold trim, carpet
  runner, bunting rows, balloon clusters, gold star studs, double bulb rows, and a
  warm spotlight pooling on the can stack (the visual target).
- Juice: confetti bursts on knockdowns/wins (`CanKnockdownFX`), ball flight trails,
  score popups, power bar shifting green-gold-red, poster-shadow display text with
  dark readability chips, gold PLAY button. Ambient life (`CanKnockdownAmbience`):
  bobbing balloons, swaying bunting, twinkling bulbs, breathing spotlight — all
  decor-only, never touching physics. Mechanics and scoring untouched.

## 8. Scripts (`Assets/Scripts/CanKnockdown/`)
`CanKnockdownManager` (states/score/throws), `ThrowController` (slingshot/bands/trajectory),
`CanController` (per-can knockdown), `CanStackManager` (3-2-1 build/reset),
`BallController` (lifetime/cleanup), `CanKnockdownUI` (TMP menus/HUD),
`CanKnockdownAudio` (throttled SFX + synthetic fallback), `CanKnockdownBuilder` (booth builder).
