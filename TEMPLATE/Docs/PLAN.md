# Plan

libtpl grows in stages. Only one stage is built at a time. A stage is done
when its tests pass and every number in it is VERIFIED or listed in
[MEASURE.md](MEASURE.md).

## Ground rules for every stage

- The inklib standard (SPEC.md in the inklib repo), API version in `tpl.h`.
- Core is netstandard2.1, C# 9, no engine, fixed tick, deterministic.
- Simulation in native units; shared units only at the API boundary.
- Collision only through the host callbacks.
- Every gameplay number is VERIFIED (with source) or UNVERIFIED.

## v1: basic movement (now)

| Feature | What it means here |
|---|---|
| Idle / walk | Stick moves the character camera-relative. |
| Jump / fall | Jump button on the ground; gravity in the air. |
| Steps | Small rises are walked up. |
| Camera | The game's camera, with its own collision. |

Deliverables: Core, C API, C test, example host, replays, RESEARCH.md,
MEASURE.md.

## v2 (later)

Write the next stage here when v1 is done.

## Out of scope

- Rendering, animation playback, sound playback (events tell the host).
- Shipping any game data.
- Optional modules (SPEC.md section 11) until the core is done.
