# Research

What we know about how the character moves in the game, gathered from
public sources and the user's own copy. Facts and pointers only: no code
from the game, a decompilation or a tool. Where a line says "we think",
it is our reading, not a confirmed fact.

## Sources

List each source with a link, the version or commit read, the date, and
its licence. A source without a licence may be read, never copied.

| # | Source | Version / date | Licence |
|---|---|---|---|
| 1 | (e.g. a decompilation) | commit, date | none: read only |

## 1. Engine and tick

- Logic rate, frame timing, fixed or variable step. UNVERIFIED until M0.

## 2. Units and axes

- Native length unit, angle unit, up axis, handedness, fixed point format.
  Keep `Units.cs` and the `TPL_NATIVE_*` macros in `tpl.h` in step with this.

## 3. Movement rules

- One subsection per state. Name the source of every number.

## 4. Collision

- What the game asks its collision system, and how that maps onto
  ground, segment, overlap (and sweep if needed). Material flags and how
  they map onto the standard surface bits.

## 5. Camera

- Camera style(s), how input turns it, its collision.

## 6. Open questions

- What we do not know yet, and which MEASURE.md item would settle it.
