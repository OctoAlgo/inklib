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

## M0. Logic tick rate

- **What:** How many logic steps per second.
- **How:** A: count position changes per second of video or log.
- **Pins:** `Tuning.TickRate`.
- **Status:** open (template placeholder).

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
