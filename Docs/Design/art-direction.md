# MOMENTUM — Art direction

The guide for every future piece of the game: levels, the modular kit, the city, lighting, atmosphere, props. Set by the designer-director on 2026-10-29. It supersedes nothing in the movement rules and sits on top of the Dark Deco bible (`deco-direction.md`, the history of the look since 2026-10-07) and the HUD's house style (`ui-house-style-spec.md`).

**In one line:** level design takes VHOLUME's scale and flow; the picture is DARK DECO, the night city of the 1990s animated Gotham. Style reference only: no DC names, no logos, no Batman designs, no character likenesses, anywhere in the project.

## Architecture

- Art deco skyscrapers with STEPPED SETBACKS: three to five steps on every tall mass, the top a crown.
- Strong VERTICAL RIBS and FLUTED PILLARS on the facades; the ribs run the full height and read from far away.
- CHEVRON and SUNBURST ornament: friezes, transoms over doors, the crowns' fans.
- SPIRES and CROWNS on the tallest buildings; STATUES on cornices, over doors, on bridge piers.
- Everything is built from stacked rectangles and their steps, which is what the kit does well.

## Lighting

- NIGHT, high contrast. Most of the picture is BLACK SILHOUETTE.
- Light only the PLAYABLE surfaces: floors the line runs on, walls that are run, ledges that are vaulted, rails that are ground, poles that are swung. The route must read instantly from the light alone.
- Lit surfaces carry the picture; unlit masses are shapes against the sky. The HARD LINE stays: the lit line on the walkable edges, the black bead on the long edges (`art-style.md`).

## Atmosphere

- A RED and ORANGE SMOGGY SKY, deepest at the top, burning at the horizon.
- SEARCHLIGHTS sweeping, BLIMPS drifting, WET SURFACES catching the lights, WARM STREETLIGHTS, NEON SIGNS (warm colours and the odd red; never the cyberpunk strip).
- Smog sheets, motes in the lamps, window twinkle: the existing passes stay (`LevelKitSmog`).

## City pieces

- ELEVATED TRAIN TRACKS on their trestles, BRIDGES between towers, FIRE ESCAPES down the facades, WATER TOWERS on the roofs, BILLBOARDS on their frames, CATWALKS between plant rooms.
- Period traffic on the streets far below, trams and trains on the tracks.

## Palette

- BLACK first. The sky DEEP RED and ORANGE. Light is WARM SODIUM.
- BRASS and GOLD accents, matching the HUD: the lit line, the pads' strips, the signs, the crowns' finials.
- The colour language of play is unchanged: gold = parry, reset, flow; red = danger; cyan only ever on the blade's edge.

## The beats, mapped to the architecture

Every beat in the library is a piece of this city, never a bare slab. The validator keeps the distances and timing; the architecture is what the distances are made of.

| beat or move | what it is in the city |
|---|---|
| drops and slopes | the SETBACKS of a tower: each step down a roof, each slope a sloped setback or a collapsed floor |
| wall runs | RIBBED FACADES: the ribs are the run surface, the facade rises past any lift |
| ledge vaults and rail grinds | CORNICES: vault the cornice of a setback, grind the cornice's rail and the pipes along it |
| pole swings | STATUES and LAMP POSTS: swing round a statue's plinth column or a lamp's post to redirect |
| gaps | the gaps BETWEEN BUILDINGS, or down through a COLLAPSED FLOOR |
| connections between buildings | ELEVATED TRACKS, BRIDGES, CATWALKS, FIRE ESCAPES |
| hop stones | PILLAR TOPS, broken WALKWAY segments, ROOFTOP MACHINERY (water towers, plant rooms) |
| portals and gates | the DOORWAYS and ARCHES of the buildings the line passes through |
| tunnels | PASSAGES through a building |
| landmarks | SPIRES and SEARCHLIGHTS: the finish and the big turns are visible from far away |
| hidden routes | the high line along a facade's BALCONIES and CORNICES, lit only by a lamp |

## The modular kit

- When the kit is built, the pieces are DECO, not brutalist: setbacks, ribs, flutes, chevrons, crowns, cornices with statues. VHOLUME supplies the scale of the towers, the length of the lines and the way the route flows through a city; it supplies no surface, no material and no palette.
- Every surface belongs to a structure (the rule of 2026-10-29): rooftops on their buildings, pillars to the ground, walkways on posts, facades with buildings under them.
- Walls a run is entered from stand at least the safe height the metrics give (`LevelMetricsMath.SafeWallHeight`, 6 m today); the gym stands them at 14 m as a look.

## References

The eight images in `Docs/Reference` (the rooftops in rain with searchlights, the sepia skyline, the blue towers, the fluted tower with the terrace, the red street under a violet moon, the worm's-eye tower with searchlights, the viaduct with its light bands, the cobalt casino arches) and the seven rules they set in `deco-direction.md`. Reference only: nothing is copied, nothing is named.
