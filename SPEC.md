# The inklib standard

Version 1 (draft). Status: written 2026-10-31, not yet adopted by any lib.

An inklib is a small library that moves ONE character from ONE game exactly like the original.
The rules come from reverse engineering. Data comes from the player's own disc. Any engine can host it.
[libsm64](https://github.com/libsm64/libsm64) is the model.

Each inklib is useful on its own. A host can drop in one lib and get the character, its controls and
its camera. Nothing here assumes that several libs meet in one game. That is the later "inklib-arena"
add-on (section 11). It must not leak into the core.

Users never depend on an inklib runtime package. There is no shared `inklib.dll` and no shared header.
A new lib copies [TEMPLATE/](TEMPLATE/) and renames it. Everything shared is a convention written here.

The words MUST, MUST NOT and MAY mean what they say. `<p>` is the lib's C prefix (`fc`, `ray3`, `tpl`).
`<P>` is the same in capitals.

## 1. Naming

- Repo: `inklib-<game>`, lower case, short. Examples: `inklib-fc`, `inklib-ray3`, `inklib-pz`.
- C prefix: `<p>_` on every function and type, `<P>_` on every macro and enum value. The prefix is
  short and fixed for the life of the lib.
- Shared library file: `lib<p>.dll` / `lib<p>.so` / `lib<p>.dylib`. Header: `native/include/<p>.h`.
- C# namespace and project names: `<Name>.Core`, `<Name>.Native`, `<Name>.Core.Tests`
  (for example `Ray3.Core`).
- Character functions use the noun `char` (`<p>_char_add`). A lib MAY add aliases with the game's own
  word (`fc_mech_add`), but the standard names MUST exist and MUST be the ones in the function table.

## 2. The C API

The core API is the list below. A lib MUST export all of it, with these names, argument orders and
return rules. [TEMPLATE/native/include/tpl.h](TEMPLATE/native/include/tpl.h) is the reference header;
it documents every field.

| Function | What it does |
|---|---|
| `int32_t <p>_api_version(void)` | Returns `<P>_API_VERSION` of the built lib. |
| `int32_t <p>_last_error(char *buf, int32_t size)` | Copies the last error of this thread as UTF-8 with a NUL, cut to `size`. Returns the full length without the NUL, 0 if there is none. `buf` MAY be NULL to ask for the length. |
| `int32_t <p>_units(<p>_units_info *out)` | Says the native units and axes (section 3). |
| `<p>_world *<p>_world_create(const <p>_world_config *config, const <p>_collision *collision)` | Makes a world. NULL on error. |
| `void <p>_world_destroy(<p>_world *world)` | Frees it. NULL is fine. |
| `int32_t <p>_char_add(<p>_world *world, const <p>_char_config *config)` | Adds the character. Returns its id (1 or more), 0 on error. |
| `int32_t <p>_char_remove(<p>_world *world, int32_t id)` | Removes it. Its id is never reused in this world. |
| `int32_t <p>_char_input(<p>_world *world, int32_t id, const <p>_input *input)` | Sets the pad for the next ticks. Input stays until the next call. |
| `int32_t <p>_world_tick(<p>_world *world, int32_t ticks)` | Steps `ticks` fixed ticks (section 5). |
| `int32_t <p>_char_state(const <p>_world *world, int32_t id, <p>_state *out)` | Position, velocity, facing, state, animation. |
| `int32_t <p>_char_camera(const <p>_world *world, int32_t id, <p>_camera *out)` | The game's own camera (section 6). |
| `int32_t <p>_events(<p>_world *world, <p>_event *out, int32_t max)` | Moves up to `max` events out of the buffer, oldest first. Returns how many. |
| `int64_t <p>_events_dropped(const <p>_world *world)` | Events lost because the buffer was full, since create. |
| `const <p>_api *<p>_get_api(void)` | The function table. Never NULL. |

Return rule: functions that return `int32_t` return 1 (or an id) on success and 0 on error, and set the
last error. A bad pointer or unknown id is an error, never a crash. `<p>_events` returns 0 when the
buffer is empty; that is not an error.

### Structs

- `<p>_world_config`: `size`, `units` (`<P>_UNITS_NATIVE` or `<P>_UNITS_SHARED`), `data_path`
  (UTF-8 path to the user's own game data, MAY be NULL if the lib needs none). Libs add their own
  fields at the end (for example `level`).
- `<p>_char_config`: `size`, `flags` (`<P>_CHAR_NO_CAMERA` = 1 skips the camera), `position`, `yaw`,
  `variant` (lib-defined: skin, suit, character in a cast; 0 = default).
- `<p>_input`: `size`, `buttons` (bitmask, bits defined by the lib as the game's pad), `move_x`,
  `move_y`, `look_x`, `look_y` (floats -1..1, raw sticks as the player holds them, +y = up/forward).
  The lib turns these into movement the way the game does: camera-relative, tank controls,
  whatever the original did. The lib quantises sticks the way the game's pad driver does.
  A game without analog sticks ignores them.
- `<p>_state`: `size`, `state` (lib enum, `<P>_STATE_*`), `anim` (the game's animation id,
  -1 if none), `grounded`, `tick`, `position` (feet), `velocity`, `yaw`, `ground_surface`
  (section 4 flags).
- `<p>_event`: `type`, `char_id`, `tick`, `a`, `b`, `position`. Types 1..255 are standard:
  `STATE` (a = old, b = new), `LAND`, `LEAVE_GROUND`, `SOUND` (a = the game's sound id; the host
  plays it). Types 256 and up are the lib's own.
- `<p>_units_info`, `<p>_camera`, `<p>_hit`, `<p>_shape`, `<p>_collision`: sections 3, 4 and 6.
- `<p>_api`: `size`, `api_version`, `prefix` (string, for example `"fc"`), then one pointer per
  function above in table order, `get_api` excluded. A lib MAY append its own functions after the
  standard ones. Because every lib's table starts the same way, a host can load any inklib with
  `dlopen` + `dlsym("<p>_get_api")` and drive it through one struct of its own.

### ABI rules

- Plain C99 structs. Fixed-size ints only (`int32_t`, `uint32_t`, `int64_t`). Reals are `double`,
  except the stick floats in `<p>_input`. No `bool`, no `enum` types in structs (enum values are
  macros or anonymous enums, fields are `int32_t`), no `long`, no `size_t`.
- `cdecl`, `extern "C"`. No C++ in the header. No variadic functions.
- Versioned structs: every struct the host passes in or gets back, except the small fixed ones
  (`<p>_vec3`, `<p>_hit`, `<p>_shape`, `<p>_event`), starts with `uint32_t size`. The host sets it to
  `sizeof` from the header it compiled with. The lib reads only `min(size, its own size)` bytes and
  treats missing fields as zero. It writes only `min(size, its own size)` bytes back. A size smaller
  than the first version of the struct is an error. New fields go at the end, and zero MUST mean
  "old behaviour".
- The small fixed structs only change with a new `<P>_API_VERSION`.
- `<P>_API_VERSION` goes up by one on every change a host could notice. Old fields are never moved.
- No callbacks into the host except the collision callbacks. Events are polled. Logging goes to
  `<p>_last_error`, never to stdout.
- No global state that two worlds could share. One world is used from one thread at a time.
  Different worlds MAY run on different threads.
- Strings in are UTF-8 and only read during the call. Pointers in structs (`data_path`) are only read
  during the call that receives them. `collision->user` stays valid until `<p>_world_destroy`.

## 3. Units and axes

Each lib has native units: the game's own. libfc keeps PS1 integers, 16.16 fixed point and angles
0..4095. libray3's native units are already metres.

**The header MUST say them** as macros, and `<p>_units` returns the same at run time:

```c
#define TPL_NATIVE_METRES_PER_UNIT  0.01    /* one native length unit in metres */
#define TPL_NATIVE_RADIANS_PER_UNIT 1.0     /* one native angle unit in radians */
#define TPL_NATIVE_UP_AXIS          1       /* 0 = X, 1 = Y, 2 = Z */
#define TPL_NATIVE_UP_SIGN          1       /* +1 or -1 */
#define TPL_NATIVE_HANDEDNESS       1       /* +1 right-handed, -1 left-handed */
#define TPL_NATIVE_VELOCITY_PER_TICK 1      /* 1: native velocity is units per tick, 0: per second */
#define TPL_TICK_RATE               60      /* ticks per second */
```

**The shared convention** is metres, seconds, radians, Z up, right-handed, yaw 0 = +X, positive yaw
turns from +X towards +Y. Velocity is metres per second.

A host picks one with `<p>_world_config.units`. Every position, direction, velocity and angle that
crosses the API (state, camera, events, and the collision callbacks) is then in that convention.
Field of view is always radians (section 6). Sticks are always -1..1.

- `<P>_UNITS_NATIVE` = 0: the game's numbers, unchanged. libfc returns 16.16 values as exact doubles.
  This is what replays compare against when they are game logs.
- `<P>_UNITS_SHARED` = 1: converted at the boundary by a small layer (`Units.cs` in the template).

The simulation inside MUST run in native units, so the game-exact rules never see a converted
number. The conversion layer converts host answers back to native units before the simulation uses
them; it rounds the way the game would (to the nearest native integer for integer games).

## 4. Collision

The lib never sees meshes. The host answers queries through callbacks in `<p>_collision`:
`size`, `user`, then the pointers. All coordinates are in the world's unit convention.
Every callback returns 1 and fills `*out` on a hit, else 0.

Required: every host MUST provide these three, so a host written for one inklib has what any other
needs at minimum.

| Callback | Question |
|---|---|
| `ground(user, from, max_drop, out)` | First walkable ground straight down (along -up) from `from`, within `max_drop`. `out->point` is on the ground. |
| `segment(user, a, b, flags, out)` | First hit on the line from `a` to `b`. `flags`: `SEGMENT_SKIP_FLOORS` ignores surfaces facing up; `SEGMENT_CAMERA` ignores `CAMERA_PASS` surfaces. |
| `overlap(user, shape, out)` | Does this shape (sphere, capsule or axis-aligned box) touch solid geometry? `out` is one of the touches; its normal points out of the geometry. |

Optional: NULL unless the lib needs it. A lib uses only what its game really asked. If it needs an
optional callback, its header says so at the top and `<p>_world_create` fails with a clear error when
it is NULL.

| Callback | Question |
|---|---|
| `sweep(user, shape, move, out)` | First hit while moving `shape` by `move`. Needed by libs whose game sweeps its body (libray3). |

A lib MAY append its own callbacks after `sweep`. It MUST document each one and MUST NOT require one
that the game did not really need.

`<p>_hit` is `point`, `normal` (unit, pointing back at the query), `t` (0..1, how far along the query),
`surface` (flags), `actor` (host id of what was hit, 0 for level geometry).

Surface flags: bits 0..15 mean the same in every lib. Bits 16..31 are the lib's own (its game's
material bits). The host sets what it knows; zero is plain solid ground.

| Bit | Name | Meaning |
|---|---|---|
| 0 | `NO_COLLISION` | Ignore it for movement. |
| 1 | `WATER` | Water surface or volume. |
| 2 | `HURT` | Hurts on touch. |
| 3 | `DEATH` | Kills or resets on touch (pits, lava). |
| 4 | `SLIDE` | Slippery or a slide. |
| 5 | `LEDGE` | Edge that can be grabbed. |
| 6 | `CLIMB` | Wall that can be climbed. |
| 7 | `HANG` | Ceiling that can be hung from. |
| 8 | `BOUNCE` | Trampoline or bumper. |
| 9 | `CAMERA_PASS` | The camera passes through it. |
| 10..15 | reserved | Zero. |

The character never reports itself: the host leaves the character's own body out of every answer.

## 5. Determinism

- Each lib has its own fixed tick: the game's logic rate (`<P>_TICK_RATE`, 30 for libfc, 60 for
  libray3). `<p>_world_tick(world, n)` steps exactly n ticks. The host keeps its own accumulator.
- No wall clock (`DateTime`, `Stopwatch`, `Environment.TickCount`), no `System.Random`, no
  `HashCode.Combine`, no threads, no dictionary order in the simulation. A game RNG is reimplemented
  and seeded from the world config.
- Same lib build, same inputs, same collision answers: same state, tick for tick.
- Bit-exact across operating systems is a goal, not a promise: `Math.Sin`/`Cos`/`Atan2` come from
  the platform. Games that used tables or integer maths (libfc) MUST use the same tables and maths.

### Replay format

All libs share one text format. A replay is one character's inputs per tick plus expected state at
checkpoints. Every `*.replay` file under `tests/*/Replays/` runs as a test.
[TEMPLATE/Docs/REPLAY.md](TEMPLATE/Docs/REPLAY.md) is the full description; in short:

```
format: inklib-replay 1
lib: tpl 1
name: walk forward, jump, land
source: UNVERIFIED: regression, recorded from the template itself
units: native
world: flat
start: 0 0 0 0
tolerance: 0.01

ticks buttons move_x move_y look_x look_y | x y z yaw state
10 0 0 0 0 0 | 0 0 0 0 Idle
30 0 0 1 0 0 | 150 0 0 0 Walk
1 jump 0 1 0 0 | 155 8 0 0 Jump
40 0 0 1 0 0 | 355 123 0 0 Fall
30 0 1 0 0 0 | 355 0 150 -1.570796 Walk
```

- Header lines `key: value`; `#` starts a comment. `format`, `lib` and `source` are required.
- `source` starts with `VERIFIED:` (where and how it was measured) or `UNVERIFIED:` (why it exists).
- `units`: `native` or `shared`. `world`: a test world defined in the lib's tests (never game geometry).
  `start: x y z yaw`. `origin: x y z` (optional) means the checkpoints are game coordinates.
  `tuning: Field value` overrides one tuning number for this file.
- Table: input columns, `|`, check columns. `buttons` is `0`, a hex mask `0x..`, or button names
  joined with `+`. `-` in a check column means "do not check". `state` is the state name.
- A lib MAY add columns after `state`, named in the table header.

## 6. Camera

Every lib ships its character's camera, because the camera is part of how the character feels.
This is where libs differ most.

- Input: `look_x` / `look_y` in `<p>_input` plus whatever camera buttons the game had (in `buttons`).
- Output: `<p>_char_camera` fills `<p>_camera`: `size`, `style`, `position`, `target`, `up`, `fov_y`
  (vertical field of view, radians, always). `up` carries the roll. Position and target are in the
  world's units.
- Style tags: `FIRST_PERSON` = 1, `THIRD_PERSON` = 2, `CHASE` = 3, `TOP_DOWN` = 4, `FIXED` = 5.
  `style` can change at run time (Project Zero switches between fixed views and the viewfinder).
  libfc is `CHASE` / `TOP_DOWN`, libray3 is `THIRD_PERSON`, a Project Zero lib is `FIRST_PERSON`
  in the viewfinder.
- The camera uses `segment` with `SEGMENT_CAMERA` for its own collision.
- With `<P>_CHAR_NO_CAMERA` the lib skips the camera and `<p>_char_camera` returns 0.
- A lib MAY add its own camera fields (the game's rotation matrix, for example) at the end.

## 7. Provenance

Every gameplay number is VERIFIED (with its source) or UNVERIFIED (with the reason it was picked).

- In code: each tuning field carries `[Verified("source")]` or `[Unverified("reason")]`, exactly one.
- A unit test fails if a field has neither, both, or an empty text (`ProvenanceTests`).
- Every `[Unverified]` field is named in `Docs/MEASURE.md` with how to verify it. A test checks this.
- If the header exposes tuning, each field's comment says VERIFIED or UNVERIFIED; a test compares it
  with the attribute (`HeaderTests`).
- In docs: write VERIFIED (source) or UNVERIFIED next to every number. RESEARCH.md keeps facts and
  pointers, and says which are our reading.
- A source is specific: game version, level, method, date, or a document and section.
- Replays carry the same tag in `source:`. A VERIFIED replay is never re-recorded; if it fails, the
  lib is wrong.
- Every PR lists what it made VERIFIED and what is still UNVERIFIED.

## 8. Data rules

- No game data in any shipped or committed file: no levels, meshes, textures, sounds, scripts,
  executables, ELF or ISO files, raw or converted, not even "for a test". `.gitignore` blocks common
  extensions and CI fails on them; that is a backstop, not permission.
- Game data reaches a lib only at run time, from the user's own disc or install, through
  `data_path`. Asset tools in a lib read the user's copy and write to the user's disk, never to the repo.
- Single numbers with a source note are fine. Tables of numbers read from the game are fine if the lib
  reads them from the user's data at run time instead of shipping them.
- No copied code from decompilations, disassembly or game executables. Read them to understand; write
  your own code. No translated functions, structs or tables.
- Code from other open projects only with a fitting licence (MIT, BSD, Apache-2.0, zlib, public
  domain), named at the top of the file. Never GPL, LGPL, AGPL or unlicensed code.
- Each README says it is a fan project and that the user needs their own copy of the game.

## 9. Packaging

- Core: C#, `netstandard2.1`, C# 9, no NuGet dependencies, no engine references.
- Native: NativeAOT shared library for `win-x64`, `linux-x64` and `osx-arm64`, from `<Name>.Native`.
  Windows also ships `<p>.def` and an import library.
- Unity: a UPM package (`unity/`) with Core as C# source in an asmdef with `noEngineReferences`.
  Unity uses Core directly; it does not need the native library.
- A minimal C example host (`native/example/host.c`): a flat floor, one character, prints its state.
- CI on every PR: `dotnet test` on Linux and Windows; native build plus C test on linux-x64, win-x64 and
  osx-arm64; the data check.
- A release attaches the three native libraries, the header, the `.def` and the Unity package.
- Licence: TODO (octo decides; the standard does not pick one).

## 10. Docs every lib has

- `CLAUDE.md`: hard rules for anyone, human or AI, working in the repo.
- `Docs/PLAN.md`: stages; only one is built at a time.
- `Docs/RESEARCH.md`: what is known about the game and where it came from.
- `Docs/MEASURE.md`: how to verify each UNVERIFIED number in the real game.
- `Docs/REPLAY.md`: the replay format and this lib's test worlds.

## 11. Optional modules (not core)

Some hosts want more than movement. These parts are optional and are kept out of the core API so a
lib stays useful on its own. Each module:

- lives in its own header `<p>_<module>.h` and its own table from `<p>_get_<module>_api()`,
  which returns NULL if the lib does not have the module;
- MUST NOT add fields to core structs or change core behaviour when it is not used;
- follows the same ABI, units and provenance rules.

Presentation (any host): `look` (draw the character the game's way: mesh, atlas, animation frame read
from the user's data), `lights`.

The arena set, from libfc's v5-v7 shape. Together they form the later **inklib-arena** add-on, which
gets its own standard when it starts:

| Module | Shape |
|---|---|
| `damage` | Health and max health per character, team, `damage(id, amount, source)`, `DIED` event. |
| `targets` | The host registers things the character can aim at (id, position, box, team). |
| `weapons` | Weapon slots, ammo, `rearm`; firing comes from `buttons`. |
| `shots` | Shots in flight as polled data (position, velocity, owner, kind); hits as events. |
| `hud` | The game's HUD as data: quads plus atlas, aim point, lock-on mark. The host draws it. |
| `vehicle` | `pilot` / `park`: a character enters or leaves something it drives. |

Arena rules (meeting characters, scoring, unit scale between games) MUST NOT appear in a core lib.
