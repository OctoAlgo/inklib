# CLAUDE.md

Rules for anyone (human or AI) working in this repo.

## What this is

The inklib standard ([SPEC.md](SPEC.md)), the copyable template
([TEMPLATE/](TEMPLATE/), its own rules in TEMPLATE/CLAUDE.md) and where the
existing libs stand ([COMPLIANCE.md](COMPLIANCE.md)).

## Rules

- Closeness to the original game is the top priority of every inklib.
- Keep SPEC.md, the template and COMPLIANCE.md in step: a rule in the spec
  gets template code and a test, and a row per lib in COMPLIANCE.md.
- The template must build and pass CI (`.github/workflows/template.yml`,
  including copy-and-rename). Recording replays again changes their hash;
  update `TEMPLATE/native/tests/test_tpl.c` too.
- No game data, no copied code from other projects (faithful ports of the game's own logic are allowed, SPEC section 9), no licence chosen (TODO for octo).
- Small PRs. Push after every finished step. Keep "Next" below current.

## Next

1. Licence: octo decides (README, TEMPLATE/LICENSE, unity/package.json).
2. libfc: apply COMPLIANCE.md (API v8): optional parts into modules,
   camera out of the state, versioned structs, shared units, snapshot,
   event ids, move lockstep code out to inklib-net, measure tick step and
   input delay.
3. libray3: apply COMPLIANCE.md: DetMathF (copy TEMPLATE/src/Tpl.Core/DetMathF.cs)
   for the 13 platform-maths sites, touch tolerance, hooks in ray3_hooks.h,
   (float32 stays), TickSeconds 0.016, 1-tick input delay, world API,
   snapshot, events, macOS CI, inklib-replay format.
4. Template: an example of a lib-owned store (SPEC.md section 6, "Lib-owned
   world state") once the Splatoon lib starts.
5. inklib-net and inklib-arena: spec only; start code when two libs comply.
6. Template: implement the host hooks (SPEC.md section 14) and loadout
   (section 12) as a reference, with snapshot tests. Spec only so far.
7. COMPLIANCE.md: add an sp2 (Splatoon 2) table once its repo can be read.
