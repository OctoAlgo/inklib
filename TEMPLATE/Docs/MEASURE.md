# Measurement wishlist

Numbers to measure in the real game. Each one turns an UNVERIFIED field in
`src/Tpl.Core/Tuning.cs` into a VERIFIED one. `ProvenanceTests` fails if an
UNVERIFIED field is not named here.

Nothing measured goes into the repo except the numbers and a note on how
they were taken: no screenshots, no videos, no game files.

## How to measure

Write down the method for this game. The usual three:

- **A. Memory log.** Run the game, find the character's position in memory,
  log it every frame. Most accurate. A log can go straight into a replay
  file ([REPLAY.md](REPLAY.md)).
- **B. Video.** Record at the game's frame rate, constant frame rate, step
  frame by frame.
- **C. Data.** Read the value from your own copy of the game's data with a
  viewer. Write the number down; do not export or commit the data.

Do each item three times. Write down all three values, the game version,
the place and the method.

## M0. Tick rate, tick step and input delay

- **What:** (a) How many logic steps per real second. (b) How much game time
  one step really simulates: games often step in whole milliseconds or clamp
  and average dt, so the step need not be 1 / rate (Rayman 3 PC: 16 ms at
  60 Hz, so the game runs at 96 % of real time). (c) How many steps pass
  between a button press and the game reacting.
- **How:** A: log position and the game's own timer every frame; the timer
  delta is the step. Compare a stopwatch over a long run with the game's
  timer. For (c), log the pad state next to the position and count frames
  from press to the first change.
- **Pins:** `Tuning.TickRate`, `Tuning.TickSeconds`, `Tuning.InputDelayTicks`.
- **Status:** open (template placeholders).

## M1. Walk speed

- **What:** Speed on flat ground at full stick, and the dead zone.
- **How:** A: positions over one second at full speed. Stick tilts near zero for the dead zone.
- **Pins:** `Tuning.WalkSpeed`, `Tuning.StickDeadzone`.
- **Status:** open (template placeholder).

## M2. Jump and gravity

- **What:** Take-off speed, apex height, time to apex, gravity while falling.
- **How:** A: log the height every frame from take-off to landing.
- **Pins:** `Tuning.JumpSpeed`, `Tuning.Gravity`.
- **Status:** open (template placeholder).

## M3. Body and steps

- **What:** Body radius and height; tallest step walked up without a jump.
- **How:** C: collision shape in the data. A/B: steps of known height.
- **Pins:** `Tuning.BodyRadius`, `Tuning.BodyHeight`, `Tuning.StepHeight`.
- **Status:** open (template placeholder).

## M4. Camera

- **What:** Distance and height behind the character, turn speed at full
  look stick, vertical field of view.
- **How:** A: camera position next to the character's. C: projection
  settings in the data.
- **Pins:** `Tuning.CameraDistance`, `Tuning.CameraHeight`,
  `Tuning.CameraTurnSpeed`, `Tuning.CameraFovY`.
- **Status:** open (template placeholder).
