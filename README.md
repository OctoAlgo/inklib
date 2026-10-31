# inklib

A standard and a template for small libraries that move one character from a
game exactly like the original, for any engine. Each lib reads the player's
own game data and reverse-engineered rules. [libsm64](https://github.com/libsm64/libsm64)
is the model.

- [SPEC.md](SPEC.md): the standard. Naming, C API shape, units, collision,
  determinism and replays, camera, provenance, data rules, packaging,
  optional modules.
- [TEMPLATE/](TEMPLATE/): copy this to start a new lib (`rename.sh`). It
  builds and its tests pass in CI.
- [COMPLIANCE.md](COMPLIANCE.md): where the existing libs stand.

There is no inklib runtime package. Users copy the template; nothing
depends on this repo at run time.

Libs: inklib-fc (Future Cop: L.A.P.D., today libfc), inklib-ray3
(Rayman 3, today libray3). Planned: a first-person Project Zero lib.

This repo contains no game data and no code from any game.

Licence: TODO (octo).
