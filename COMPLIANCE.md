# Compliance

Where each existing lib stands against [SPEC.md](SPEC.md) version 1, and what has to change before
it is published under its inklib name. Written 2026-10-31; rollback rows (sections 5, 6, 13) added
the same day.

Legend: ✅ matches, 🟡 partly, ❌ missing or different, ❔ not known.

## inklib-fc (today libfc, Future Cop: L.A.P.D., X1-Alpha mech)

The repo is on a private server and could not be read. This table comes from the description of
C API v7 only. Every ❔ needs a look in the repo.

| Spec | libfc v7 today | Change before publish |
|---|---|---|
| 1 Naming | Repo `libfc`, prefix `fc_`. Character functions use `mech`. | Rename the repo to `inklib-fc`. Add the standard `fc_char_*` names; `fc_mech_*` may stay as aliases. |
| 2 Version, last error | ✅ `fc_api_version`, `fc_last_error(buf, size)`. | Check that it returns the full length and accepts `buf = NULL`. |
| 2 Units query | ❌ | Add `fc_units()` and the `FC_NATIVE_*` macros. |
| 2 Create / destroy | 🟡 `fc_world_create(data_path, level, const fc_collision*)`. | Take `const fc_world_config*` (`size`, `units`, `data_path`, then `level` as a lib field). |
| 2 Add / remove | 🟡 `fc_mech_add(world, config)` with x, y, z, yaw, hover, team, health, max_health, no_camera. Remove ❔. | `fc_char_config`: `size`, `flags` (`NO_CAMERA`), `position`, `yaw`, `variant`, then `hover` as a lib field. Team and health move to the `damage` module. Add `fc_char_remove` if it is missing. |
| 2 Input | 🟡 `fc_mech_input(world, mech, pad, analog, stick_x, stick_y)`. | `fc_input` struct: `size`, `buttons` (PS1 pad bits), `move_x/y`, `look_x/y` as -1..1; lib quantises to PS1 pad bytes. `analog` becomes a lib field. |
| 2 Tick | ✅ `fc_world_tick(world, ticks)`, 30 Hz. | None. |
| 2 State | 🟡 `fc_mech_state`: position, velocity, state, anim, health, plus the camera. | `fc_state`: `size`, `state`, `anim`, `grounded`, `tick`, `position`, `velocity`, `yaw`, `ground_surface`. Health moves to `damage`; camera moves to `fc_char_camera`. |
| 2 Events | ✅ `fc_events(world, out, max)` + `fc_events_dropped`. | Check the event struct (`id`, `tick`, `type`, `char_id`, `a`, `b`, `position`) and renumber: standard types 1..4, libfc's own from 256. |
| 2 Function table | 🟡 `fc_api` exists. | Start it with `size`, `api_version`, `prefix`, then the standard order. Module functions leave the core table (see 12). |
| 2 ABI | ❔ | Check: fixed-size ints only, `size` first in every versioned struct, no callbacks except collision. Reals become `double` (exact for 16.16). This is API v8, a breaking change. |
| 3 Units | 🟡 Native PS1 integers, y up, angles 0..4095, 16.16. Not declared as macros; no shared mode. | Declare them (`FC_NATIVE_RADIANS_PER_UNIT` = 2π / 4096; metres per unit: a MEASURE item; up sign and handedness: check, PS1 games are often y-down). Add `FC_UNITS_SHARED` through a small conversion layer; the simulation keeps its integers and rounds host answers back. |
| 4 Collision | 🟡 `ground(x,y,z -> height + normal)`, `segment(a, b -> hit + normal, skip floors)`, `overlap_actor(box)`. | Same three, standard signatures: `ground(from, max_drop, hit)`, `segment(a, b, flags, hit)`, `overlap(shape, hit)` (box shape; `hit.actor` names the actor). Add `size`, `user`, and surface flags (game materials in bits 16..31). |
| 5 Determinism | ✅ Fixed 30 Hz tick, game-integer maths (by design). Wall clock and random sources ❔. | Check for `DateTime`, `Random`, threads. Reimplement the game RNG if the mech uses one. |
| 5 Same bits on every machine | ✅ Integer PS1 maths (16.16, 4096-step angles), already proven by lockstep online 1v1 with a hash check. | Check that no float or platform maths feeds the simulation (floats only in outputs such as the camera rows or the HUD). Add the scan test and run `dotnet test` on macOS arm64 in CI. |
| 5 Replay format | 🟡 libfc has replays (format ❔). | Convert to `inklib-replay 1` with a `hash:` line per file; the C test repeats one hash. |
| 6 Snapshot (`fc_state_size`, `fc_save_state`, `fc_load_state`) | ❔ Probably missing: lockstep never rolls back, so it does not need one. | Add them: every mech, its camera, timers, the game RNG, the tick and id counters, in the fixed little-endian layout with magic, format and tuning hash. Add the save/step/load/step test. With the arena modules, their state (health, shots, targets) goes into their own module snapshot. |
| 6 State hash | 🟡 A hash exists for the lockstep check (what it covers ❔). | Expose it as `fc_state_hash` = FNV-1a 64 over the snapshot bytes. The hash changes, so old lockstep peers do not match new ones (API v8 anyway). |
| 6 Event ids | ❔ | `id` (same on resimulation) and `tick` on every event. |
| 6 Input quantisation | ✅ The game reads PS1 pad bytes. | Make the stick handling depend only on `round(stick * 127)` (map it onto the pad byte), so inklib-net can send 4 stick bytes. |
| 13 Netcode out of the lib | ❔ The lockstep 1v1 code: inside libfc or in the host? | If it is inside libfc, move it out: it becomes the first piece of inklib-net (lockstep = the zero-rollback case). libfc keeps only snapshot, hash, events and replays. |
| 7 Camera | 🟡 The game's chase camera is in the state: position, pitch/yaw/roll, rotation rows. | `fc_char_camera`: `style` (`CHASE` / `TOP_DOWN`), `position`, `target`, `up`, `fov_y`. Pitch/yaw/roll and rows may stay as fields after the standard ones. Look input through `look_x/y`. |
| 8 Provenance | ❔ | `[Verified]` / `[Unverified]` on every tuning number, the tests, MEASURE.md. |
| 9 Data rules | 🟡 Mesh, atlas and HUD art come from `data_path` (the user's disc). | Check the repo history for game files and copied decomp code; add the CI data check. |
| 10 Packaging | ❔ | NativeAOT win-x64, linux-x64, osx-arm64; Unity UPM package; C example host; CI on Linux and Windows. |
| 11 Docs | ❔ | CLAUDE.md, Docs/PLAN, RESEARCH, MEASURE, REPLAY. |
| 12 Optional modules | ❌ In the core API since v5: mesh/look/atlas, lights, pilot/park, damage, targets, weapons/rearm, shots in flight, HUD as data. | Move each into its own header and table: `fc_look.h` (mesh, look, atlas, lights), and the arena set `fc_damage.h`, `fc_targets.h`, `fc_weapons.h`, `fc_shots.h`, `fc_hud.h`, `fc_vehicle.h`. The core must work with none of them used. These shapes seed the inklib-arena add-on. |
| Licence | ❔ | TODO (octo). |

**Biggest gaps for libfc:** the optional parts live in the core API; no shared-unit mode; camera is
inside the state; create/add/input take loose arguments instead of versioned structs; snapshots
(save/load) for rollback; the online code may have to move out to inklib-net. Together these are one
breaking release (API v8). Cross-machine determinism is already there, thanks to the integer maths.

## inklib-ray3 (today libray3, Rayman 3: Hoodlum Havoc, PC)

Read from OctoAlgo/libray3 at commit `484385e` (2026-10-31).

| Spec | libray3 today | Change before publish |
|---|---|---|
| 1 Naming | Repo `libray3`, prefix `ray3_` ✅, `libray3.dll` ✅, header `native/include/ray3.h` ✅. C# `LibRay3.*`. | Rename the repo to `inklib-ray3`. C# names `Ray3.Core` etc. (optional). |
| 2 Version, last error | ❌ Neither. `ray3_create` returns NULL without a reason. | Add `ray3_api_version`, `ray3_last_error`. |
| 2 Units query | ❌ | Add `ray3_units()` and the `RAY3_NATIVE_*` macros (all 1.0, Z up). |
| 2 Create / destroy | 🟡 `ray3_create(world, config, feet, yaw)` makes one controller. | Split into `ray3_world_create(config, collision)` and `ray3_char_add`. The tuning (`ray3_config`) becomes a lib field in `ray3_char_config` (pointer, NULL = defaults); `ray3_default_config` stays as a lib extra. |
| 2 Add / remove | ❌ One character per create. | `ray3_char_add`, `ray3_char_remove`; several characters per world. |
| 2 Input | 🟡 `ray3_set_input`: world-space stick, `jump` and `target` as int32. | `ray3_input`: `size`, `buttons` (`JUMP`, `TARGET`), `move_x/y`, `look_x/y` as the player holds them. Camera-relative movement comes with the camera. |
| 2 Tick | 🟡 `ray3_step(c)` = one tick. | `ray3_world_tick(world, n)`. |
| 2 State | 🟡 `ray3_get_state`: floats, no `size`, no `anim`. | `ray3_state` per spec: doubles, `size`, `anim` (-1 until animations are known), `ground_surface` ✅, `tick` ✅. |
| 2 Events | ❌ | Add the buffer: `STATE`, `LAND`, `LEAVE_GROUND` at least; `SOUND` when sound ids are known. |
| 2 Function table | ❌ | Add `ray3_api` + `ray3_get_api`. |
| 2 ABI | 🟡 Plain C, fixed-size ints, booleans as int32 ✅. No `size` fields; floats. | `size` first in every versioned struct; reals as double. |
| 3 Units | ✅ Metres, seconds, radians, Z up, right-handed: native already equals shared. | Declare it in the header; accept both `units` values (no conversion needed). |
| 4 Collision | 🟡 `raycast`, `sphere_cast`, `capsule_cast` (origin + unit direction + max distance). | Required `ground`, `segment`, `overlap` (host provides; ray3 may build its ray on `segment`), plus optional `sweep` (sphere/capsule), which ray3 needs and says so in its header. |
| 4 Surface flags | ❌ Game bit positions in bits 0..15 (slide = 0, trampoline = 1, ledge = 2, ...). | Standard bits 0..15 (`SLIDE`, `BOUNCE`, `LEDGE`, `HANG`, `CLIMB`, `HURT`, `DEATH`, `WATER`, `NO_COLLISION`); the game's own bits move to 16..31 (wall, fall trigger, electric, ...). |
| 5 Determinism | ✅ Fixed 60 Hz (`Tuning.TickRate`), no clock, no random, no threads (CLAUDE.md rule 2). | None. |
| 5 Same bits on every machine | ❌ The simulation is `float` (System.Single) throughout: `Vec3`, `Tuning`, all state. It calls `MathF.Sin` (1), `MathF.Cos` (2), `MathF.Atan2` (2), `MathF.Pow` (2) and `MathF.Sqrt` (1), which come from the platform. CI tests Linux and Windows x64 only. | Move the simulation to `double` (the C API already changes to doubles). Replace `MathF.Sin/Cos/Atan2` with `DetMath` (copy it from the template) and `MathF.Sqrt` with `DetMath.Sqrt`. Replace `MathF.Pow(1 - perFrame, dt * 60)`: at a fixed tick the exponent is fixed (exactly 1 at 60 Hz), so use repeated multiplication. Keep `Math.Clamp`, `MathF.Abs/Min/Max` as their `Math` double forms. Add the scan test, `hash:` lines, and `dotnet test` on macOS arm64 in CI. All regression replays are recorded again; VERIFIED replays keep passing within their tolerance. |
| 5 Replay format | 🟡 `libray3-replay 1`, columns `ticks move_x move_y jump target x y z state`, 6 replays. | Convert to `inklib-replay 1` (`lib: ray3 1`, buttons column, look columns, `|`, yaw, `start` with yaw, `hash:`). Mechanical; the runner keeps `origin`, `tolerance`, `tuning`. |
| 6 Snapshot and hash | ❌ None. The controller keeps private state (`_jumpWasHeld`, `_targetWasHeld`, `_lockedYaw`, `_regrabTimer`, `_jumpTick`, `_offGround`, `_offGroundTime`, `_coyote`, `_airTime`, `_hangPos`, `_climbTopPos`) next to the public state. | Add `ray3_state_size/save_state/load_state/state_hash`. Every one of those fields, the public state, the input and (later) the camera goes into the snapshot. Add the save/step/load/step test. |
| 6 Event ids | ❌ No events yet. | Add events with `id` and `tick` from the start. |
| 6 Input quantisation | ❌ Stick floats are used as given. | Quantise to `round(stick * 127) / 127` in `ray3_char_input`. |
| 13 Netcode out of the lib | ✅ None. | None. |
| 7 Camera | ❌ Out of scope in PLAN.md. MEASURE M6 notes that turning is done by the camera's script. | Ship Rayman 3's third-person camera (`THIRD_PERSON`): research, look input, `ray3_char_camera`. This is the largest piece of new work. |
| 8 Provenance | ✅ `[Verified]` / `[Unverified]` on every tuning field, `ProvenanceTests`, header comments checked by `HeaderTests`. | Add the "every UNVERIFIED field is in MEASURE.md" test. Today `CoyoteGroundGravity` and `LedgeGrabAllSides` are not named as `Tuning.X` there. |
| 9 Data rules | ✅ No game files, no copied code (CLAUDE.md rules 3-4), `.gitignore` backstop. | Add the CI data check. |
| 10 Packaging | 🟡 NativeAOT linux-x64 and win-x64 ✅, `.def` ✅, CI on Linux and Windows ✅. No macOS ("No macOS" in CLAUDE.md), no Unity package, C test only. | Add osx-arm64, the UPM package (`unity/`), `native/example/host.c`, release artifacts. |
| 11 Docs | ✅ CLAUDE.md, Docs/PLAN, RESEARCH, MEASURE, REPLAY. | Point CLAUDE.md at the standard. |
| 12 Optional modules | ✅ None in the core. | None. |
| Licence | MIT LICENSE in the repo. | Octo to confirm (the standard leaves it TODO). |

**Biggest gaps for libray3:** floats and platform maths mean replays can differ between machines,
so it cannot do rollback until the simulation moves to doubles and `DetMath`; no snapshot or hash;
the camera does not exist yet; the API is one controller instead of a world with characters; no
version, last error, events or function table; surface bits use the game's positions instead of the
standard ones.
