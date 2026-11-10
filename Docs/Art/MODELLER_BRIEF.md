# momentum, the modelling brief

hey, so the whole game is built out of boxes right now, every wall, floor, lamp and hand is a unity primitive with a flat toon material on it. it moves great and it reads fine from far away, but up close it has none of the detail the look needs. the look is 1930s art deco at night, black first, deep red and orange sky, warm sodium light, brass and gold accents, think the old animated gotham. the pictures in `Docs/Reference` are the mood.

what I need from you is in two parts, the hands first because theyre on screen every single frame, then a kit of deco dressing pieces that snap onto the boxes the levels are made of. everything below is in metres, 1 blender unit = 1 metre.

## the rules that make it drop straight in

- metres, real size, pivots where the table says, nothing scaled in the object, apply all transforms before export
- fbx, apply transform ticked, default axes, it comes into unity the right way up, triangulate is fine either way
- no textures and no uv work unless you want to, the game shades everything with a toon ramp over flat colours, so give each piece one material slot per ROLE and name the slot one of these: `Stone`, `Dark`, `Brass`, `Glass`, `Lit`, `Suede`, `Sleeve`, `Cuff`. I swap in the games materials by that name, so the colours you pick in blender dont matter, the slot names do
- hard edges where you want a crisp toon line, smooth shading on the round stuff, keep it low poly, the style is bold shapes not fine detail, one strong silhouette per piece
- no colliders, I keep the games own collision boxes, your pieces are the skin on them, they sit 5 cm proud of the box face
- every piece that tiles has to tile on a 1 metre grid, the levels are built on it
- the pieces must be DECO not brutalist, stepped setbacks, flutes and ribs, chevrons, sunbursts, crowns, cornices, bronze and black stone, no bare concrete

## part one, the hands

the player is in first person, wearing a dark suit, and the only weapon in the game is a pair of brass knuckles, so the hands are the hero asset. right now each arm is about thirty primitives: a charcoal sleeve, a cream shirt cuff with a brass cufflink on the outer side, a grey suede glove with four fingers and a thumb, and the brass knuckle bar across the fist with a ring per finger.

the rig already exists and you already have the files for it, its the same one the animation guide describes (`Tools/Animation/ANIMATOR_GUIDE.md`, section 1, and `Tools/Animation/rig_arms.json` which the blender script `Tools/Blender/momentum_rig_import.py` turns into empties with coloured proxy shapes). model and skin to THOSE bone names, dont rename or reparent anything:

| bone | what it is |
|---|---|
| `ShoulderR` / `ShoulderL` | the shoulder pivot, 0.32 m upper arm to the elbow |
| `ElbowR` / `ElbowL` | the elbow, 0.29 m forearm to the wrist |
| `HandR` / `HandL` | the wrist, the hand frame: +Z out along the fingers, +Y the back of the hand, thumb on the inner side |
| `F00 F01 F02` | index finger, knuckle to tip, same for `F10..F12` middle, `F20..F22` ring, `F30..F32` pinky, they fold about local X |
| `T0` / `T1` | thumb base and tip |
| `Fist` | the socket the brass knuckles hang off, keep the knuckles a SEPARATE mesh parented here, the game recolours and hides them by name |

sizes to match, so the skinned hand sits where the primitives sit: palm about 8.5 cm wide, 3.6 cm thick, 9 cm long, finger phalanges 3.8 / 2.5 / 1.9 cm, thumb 3.8 + 3.1 cm, wrist narrower than the cuff, cuff about 8.4 cm across. the camera looks at them through a 58 degree vertical fov and the wrists live within 0.6 m of it, so detail on the back of the hand and the knuckles pays, detail on the upper arm never shows.

- slots: `Sleeve` (charcoal), `Cuff` (cream), `Brass` (cufflink and knuckles), `Suede` (the glove)
- budget: up to about 6k triangles an arm, four weights a vertex max, only deform bones in the export, no leaf bones
- rest pose = the rigs rest pose from the json notes, fingers relaxed, and please keep the knuckles continuing the forearms line in the fist, the ik puts the elbow where the wrist asks
- one fbx per side named `arm_R` / `arm_L`, pivot at the camera (the rigs root), bones named exactly as above
- LEAVE THE BRASS KNUCKLES OUT, the game draws its own on the `Fist` socket and recolours them, so just pose the fist as if they were there

if you do nothing else, do this, it changes how the whole game looks.

## part two, the deco dressing kit

the levels are made of 10 m wide decks, 1 m thick walls 6 to 14 m tall, gates across the line, 3 m square stones on pillars, 1 m beams on posts, and 90 degree bends of six 15 degree facets. your pieces dress those. pivot for a wall piece = the middle of the bottom edge of the face that touches the wall, +Z pointing out from the wall, +Y up. pivot for a floor piece = the middle of its base.

in the order they matter:

| # | piece | size | tiles | notes |
|---|---|---|---|---|
| 1 | cornice / string course | 1 m long, 0.5 m proud, 0.4 m tall | end to end along a wall | plus an outside corner and an inside corner. this is the ledge the player will vault and grind along, so the top is a clean flat strip at least 0.3 m deep with a rounded nose |
| 2 | fluted facade panel | 2 m wide, 4 m tall, 10 to 15 cm relief | both ways | vertical flutes or ribs, the player wall runs these, so nothing sticks out more than 15 cm. three variants: plain flutes, flutes with a chevron frieze band at the top, and a WINDOW BAND with recessed panes on a `Glass` slot so I can light the windows warm |
| 3 | gate | post 0.7 m wide × 1.0 m deep × 1 m tall, lintel 1 m long × 1 m tall × 1 m deep | post tiles in height, lintel end to end | a base, a tileable shaft, a cap for the post. a sunburst or chevron transom panel 2 m wide × 1 m tall that sits over the opening. the openings are 2.2 m and 1.25 m high, the same pieces do both |
| 4 | lamp standard | 4 m tall, post 25 cm across | no | bronze, a stepped deco lantern or globe on top on a `Lit` slot. the lower 2.5 m of the post must be a plain straight cylinder, the player swings round it |
| 5 | setback crown with a spire | footprints 6, 10 and 16 m square, 8 to 20 m tall | no | three to five steps and a brass finial, these are the landmarks you see from far away |
| 6 | statue on a plinth | 3.5 m overall | no | the plinth a plain round column 50 cm across and 2 m tall (another swing anchor), the figure on top abstract and bronze, a stylised worker, a winged figure, a torch bearer, that kind of thing |
| 7 | pillar capital + column drum | capital 3 × 3 m top, 1 m tall, on a 2.4 m shaft, drum 1 m across × 1 m tall | drum tiles in height | the capitals go on the hop stones, the drums under beams and walkways |
| 8 | catwalk + balustrade | 1.2 m wide × 2 m long, rails 1 m high both sides | end to end | plus a 10 m × 2 m bridge segment with a deco balustrade and a plain 1 m × 2 m beam cover. the player runs these at speed, nothing on the walking surface |
| 9 | fire escape flight | 2.5 m rise, 3 m run, 1.2 m wide, landing 1.2 × 2.4 m | flight to flight | iron, the player jumps flight to flight rather than walking them |
| 10 | set dressing | searchlight drum 1.5 m on a yoke (I rotate it), water tower 6 m (4 m tank on legs), billboard frame 6 × 3 m, elevated rail trestle 10 m span × 4 m tall | no | far away silhouettes, 200 to 800 triangles each |

budget for the kit: 200 to 1500 triangles a piece, the big crowns up to 3000. dont add floors, kerbs or lips the player could catch a foot on, every walking surface stays flat and every edge a player might run into is either flush or a clean 90.

## how it goes into the game

your fbx files go in `Assets/Resources/Models`, named like the table (`cornice_1m`, `cornice_corner_out`, `facade_flute_2x4`, `facade_window_2x4`, `gate_post_shaft`, `gate_lintel_1m`, `gate_transom`, `lamp_4m`, `crown_10m`, `statue_plinth`, `capital_3m`, `column_drum`, `catwalk_2m`, `bridge_10m`, `fire_escape_flight`, `searchlight`, `water_tower`, `billboard_6x3`, `trestle_10m`, `arm_R`, `arm_L`). the level builders look for those names and dress every wall, gate, stone, beam and lamp they place, so a new piece shows up everywhere the first time the game builds a level, and the collision and the speed numbers dont change at all because the boxes under it dont change.

send things one at a time if you like, the cornice and the fluted panel first, I can check them in the game the same day.
