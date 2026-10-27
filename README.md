# MOMENTUM

> ## ⚠️ WORK IN PROGRESS
> this is an early, unfinished build and it changes every day. levels get regenerated, systems get replaced, numbers get retuned, nothing in here is final and plenty of it is broken on any given day. its public so people can follow along, not because its ready to play.

a first person movement platformer set in a dark deco city. you run, slide, wall run, bunny hop and chain tricks in the air for speed, the city is built out of primitives at runtime with CC0 textures, there are no character models, the levels are generated from one line of text each and then hand fixed in an in editor level editor.

## where its at right now

- thirteen levels, each generated from a "score" (one line of words, one word per space), the look is gotham dark deco, the layouts take their cues from brutalist movement games
- the two press trick: click in the air to start the bar under the crosshair, click again as the marker hits the gold, flips give speed bursts, a perfect landing pays more, a miss stumbles you
- a style grade that drives run speed and stamina regen and starves if you stop tricking
- no enemies at the moment, the chases are the only bosses, combat was cut back to knuckles
- a procedural first person camera, arms at your sides, no rigged character
- a procedural menu city, toon shading, painted skies, searchlights, airships
- the music is a generated noir jazz bed plus public domain 78s on a gramophone

## known broken things

- some transparent surfaces (the smog sheets, the airships, some lights) still render magenta on some machines, its being chased
- level layouts are mid rework, expect odd geometry, missing routes and placeholder dressing
- almost every number is a placeholder, the tuning is not done
- performance is not tuned, the city is heavy

## opening it

- unity 6000.5.10f1 with URP, open the folder in unity hub
- the first import takes a while, the Library folder is not in the repo
- open `Assets/Scenes/MainMenu.unity` and press play, the levels build themselves when you pick one
- the code is all in `Assets/Scripts`, the design notes are in `Docs/Design`, the offline checks (a compile check, a shader check, a menu preview renderer) are in `Tools`

## credits and licences

the games own code and design are mine, all rights reserved for now, look and learn from it but ask before you reuse it. the third party pieces keep their own licences and every one of them ships with its licence file next to it:

- textures: ambientCG (CC0) in `Assets/Resources/Surfaces`
- ui sprites, icons, key glyphs, ui sounds, smoke puffs and impact sounds: Kenney (CC0), kenney.nl
- extra impact samples: OpenGameArt (CC0), see `Assets/Resources/Impacts`
- fonts: Barlow, Barlow Condensed and Michroma (SIL Open Font License 1.1), see `Assets/Resources/Fonts`
- the gramophone records: 1922 to 1925 sides from the Internet Archive's Great 78 Project (public domain), listed in `Assets/Resources/Music/jazz/JAZZ_LICENSES.txt`
