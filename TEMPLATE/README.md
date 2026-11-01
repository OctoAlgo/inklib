# libtpl

TODO: one sentence. "<Character>'s movement from <Game> (<platform>), for any engine."

This is a fan project. It contains no game files and no code from the game
or any decompilation. You need your own copy of the game.

It is an [inklib](https://github.com/OctoAlgo/inklib): it moves one
character exactly like the original, with the game's own camera. You give it
pad input once per tick and answer its collision questions. It gives back a
position, a velocity, a facing, a state, the camera, and events (landed,
play this sound, ...). Drawing and sound are yours.

## Start a new lib from this template

```
cp -r TEMPLATE ../inklib-<game>
cd ../inklib-<game>
./rename.sh <prefix> <Name>        # e.g. ./rename.sh pz Pz
git init
```

Then replace the template's numbers and rules: `Units.cs` (+ the
`TPL_NATIVE_*` macros), `Tuning.cs`, `Character.cs`, the header enums,
and the Docs.

## Build and test

Needs the .NET 8 SDK.

```
dotnet test
native/build_and_test.sh
```

## C API

Header: [native/include/tpl.h](native/include/tpl.h). Smallest host:
[native/example/host.c](native/example/host.c).

```c
tpl_world_config wc = { sizeof wc, TPL_UNITS_SHARED, NULL };  /* metres, Z up */
tpl_world *w = tpl_world_create(&wc, &collision);             /* your callbacks */
tpl_char_config cc = { sizeof cc };
int32_t id = tpl_char_add(w, &cc);
/* every tick: */
tpl_char_input(w, id, &input);
tpl_world_tick(w, 1);
tpl_char_state(w, id, &state);
tpl_char_camera(w, id, &camera);
n = tpl_events(w, events, 32);
/* rollback hosts: snapshot, hash, restore (free unless called) */
int32_t size = tpl_state_size(w);
tpl_save_state(w, buf, size);
uint64_t h = tpl_state_hash(w);
tpl_load_state(w, buf, size);
/* at the end: */
tpl_world_destroy(w);
```

## Unity

`unity/` is a UPM package with Core as C# source. Run `unity/pack.sh`, then
add the folder in the Package Manager ("Add package from disk").

## Licence

TODO (octo): pick a licence before the first publish. See LICENSE.
