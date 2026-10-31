# Replay files

A replay is one character's inputs per tick plus where it should be. Every
`*.replay` in `tests/Tpl.Core.Tests/Replays/` runs as a test
(`ReplayTests.ReplayMatches`). The format is the same in every inklib
(SPEC.md section 5); only the button names, state names and test worlds
are this lib's.

## Example

```
format: inklib-replay 1
lib: tpl 1
name: standing jump
source: VERIFIED: game v1.0, memory log (MEASURE.md A), level 1 start, 2026-11-02
units: native
world: flat
start: 0 0 0 0
origin: 1234 56 -789
tolerance: 0.5

ticks buttons move_x move_y look_x look_y | x y z yaw state
10 0    0 0 0 0 | 1234 56 -789 - Idle
1  jump 0 0 0 0 | - 64 - - Jump
```

The numbers above only show the format; they are not a measurement.

## Header lines

One `key: value` per line, before the table. `#` starts a comment.

| Key | Required | Meaning |
|---|---|---|
| `format` | yes, first | `inklib-replay 1`. |
| `lib` | yes | Prefix and API version the file was written for: `tpl 1`. |
| `source` | yes | `VERIFIED: ...` (where, how, when) or `UNVERIFIED: ...` (why). |
| `name` | no | Shown in failures. Defaults to the file name. |
| `units` | no (`native`) | `native` or `shared` (metres, Z up). All numbers in the file use it. |
| `world` | no (`flat`) | A test world (below). |
| `start` | no (`0 0 0 0`) | Feet `x y z` and `yaw` at tick 0. |
| `origin` | no | The character's position in the game at tick 0. If given, x y z are game coordinates: the runner subtracts `origin` and adds `start`. |
| `tolerance` | no (0.01) | Allowed difference per column, in the file's units. |
| `tuning` | no, repeatable | `tuning: Field value` overrides one `Tuning` field. |
| `hash` | yes in this lib | `hash: 0x` + 16 hex digits: `tpl_state_hash` after the last row. Positions within tolerance but a different hash means the bits differ: a determinism bug, or a deliberate change. |

## Table

The header line is exactly:

```
ticks buttons move_x move_y look_x look_y | x y z yaw state
```

| Column | Meaning |
|---|---|
| `ticks` | How many ticks this input is held (1 or more). |
| `buttons` | `0`, a hex mask `0x..`, or names joined by `+` (`jump`). |
| `move_x move_y look_x look_y` | Sticks, -1..1, as the player holds them. |
| `x y z yaw` | Expected after the last tick of the row. `-` = do not check. |
| `state` | Expected state name (`Idle`, `Walk`, `Jump`, `Fall`), or `-`. |

A game log at the logic rate becomes one row per tick with `ticks` = 1.

## Worlds

In `tests/Tpl.Core.Tests/TestWorlds.cs` (`TestWorld.Make`). Never game geometry.

| Name | Geometry (native units) |
|---|---|
| `flat` | Floor at y = 0. |
| `step` | Floor; a 20 cm step from x = 200; a 100 cm wall from x = 400. |

## Regression replays

`UNVERIFIED` replays recorded from the lib itself catch accidental changes.
After a deliberate change, record them again:

```
INKLIB_RECORD_REPLAYS=$PWD/tests/Tpl.Core.Tests/Replays dotnet test --filter RecordRegressionReplays
```

Recording rewrites the `hash:` lines too. `native/tests/test_tpl.c` repeats
the hash of `walk-and-jump.replay` to check the native library; update it there.

Measured (`VERIFIED`) replays are never re-recorded. If one fails, the lib
is wrong.
