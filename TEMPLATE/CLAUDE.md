# CLAUDE.md

Rules for anyone (human or AI) working in this repo.

## What this is

libtpl is an inklib: a small library that moves ONE character from ONE game
exactly like the original, for any engine. It follows the inklib standard
(SPEC.md in the inklib repo; API version in `native/include/tpl.h`). It is
useful on its own. It does not draw or play sound; it tells the host what to
draw and play.

## Hard rules

1. **Core is plain C#.** `src/Tpl.Core` targets netstandard2.1, C# 9. No
   Unity, Godot or other engine references. No NuGet dependencies in Core.
2. **Deterministic fixed tick.** Core steps in fixed ticks (`Tuning.TickRate`,
   the game's logic rate). No `System.Random`, no `DateTime`, no `Stopwatch`,
   no `Environment.TickCount`, no `HashCode.Combine`, no threads, no
   dictionary order in the simulation. Same inputs give the same outputs.
3. **Native units inside.** The simulation runs in the game's own units.
   Shared units (metres, seconds, radians, Z up) exist only at the API
   boundary, in `Units.cs`. Keep `Units.cs` and the `TPL_NATIVE_*` macros equal.
4. **No game files, ever.** No levels, meshes, textures, sounds, scripts,
   executables, ELF or ISO files, raw or converted, not even for a test. Game
   data reaches the lib only at run time from the user's own copy
   (`data_path`). `.gitignore` and CI block common extensions; that is a
   backstop, not permission.
5. **No copied code.** Decompilations, disassembly and tools may be read to
   understand the game. Write our own code. Do not paste or translate their
   functions, structs or tables. Single numbers with a source note are fine.
   Code from open projects only under MIT, BSD, Apache-2.0, zlib or public
   domain, named at the top of the file. Never GPL, LGPL, AGPL or unlicensed.
6. **Every gameplay number is tagged.** Each `Tuning` field has exactly one of
   `[Verified("source")]` or `[Unverified("reason")]`, and every UNVERIFIED one
   is named in `Docs/MEASURE.md`. Tests fail otherwise. In docs, write VERIFIED
   (with source) or UNVERIFIED next to every number.
7. **Collision only through the host.** Core never sees meshes. The host
   answers ground, segment and overlap (`ICollisionWorld`, `tpl_collision`).
   Test geometry lives in the test project and is never game geometry.
8. **C API rules.** Prefix `tpl_`. Plain C structs, fixed-size ints, versioned
   structs start with `size`, no callbacks except collision, errors through
   `tpl_last_error`, never a crash on a bad argument. `tpl.h`, `tpl.def` and
   Core stay in step (`HeaderTests`). Bump `TPL_API_VERSION` on any change a
   host could notice.
9. **The camera is part of the character.** Ship the game's camera.
10. **Core stays core.** Damage, targets, weapons, shots, HUD and vehicles are
    optional modules in their own header and table (SPEC.md section 11).
    Nothing about other games or an arena goes into this lib.
11. **Keep it small. Plain docs.** Short sentences. Say what the code does.
    Add a feature only when its stage in `Docs/PLAN.md` starts.

## Commands

```
dotnet build
dotnet test
native/build_and_test.sh            # NativeAOT build + C test + example host
```

Linux needs clang and zlib headers for NativeAOT; Windows (Git Bash) needs the
Visual Studio C++ tools and clang; macOS needs the Xcode command line tools.

## Layout

```
src/Tpl.Core/          the simulation (netstandard2.1)
src/Tpl.Native/        C API exports (NativeAOT, net8.0)
tests/Tpl.Core.Tests/  xunit tests, test worlds, Replays/
native/                tpl.h, tpl.def, C test, example host, build script
unity/                 UPM package (Core as source)
Docs/                  PLAN, RESEARCH, MEASURE, REPLAY
```

## Workflow

- One PR per task. The PR description lists what became VERIFIED and what is
  still UNVERIFIED.
- CI must pass: `dotnet test` (Linux, Windows), native build and C test
  (linux-x64, win-x64, osx-arm64), data check.
