MOMENTUM — THE GRAMOPHONE (Resources/Music/jazz)
=================================================

SINCE 2026-10-16 the score's lead is THE HOTEL ROOM, a procedural noir-jazz side
(MusicHotel.cs, "Quarto de Hotel" mood) with a jazz-hop bed under it; the 78s in
this folder are only "a gramophone in the next room": a rare, low, band-passed
layer in the lobby and the calm, OFF unless MusicDirector.Gramophone is set, one
side per lobby visit, played raw (no measurement). Every chart here carries
gramophone=1. Put lead=1 in a side's chart instead and the engine below treats
it as a LEAD again: measured, mastered, with beds on its grid.

The rest of this file describes that lead path, which still works as written.

What to drop in
---------------
  * One song per file: .ogg, .mp3, .wav or .flac. Any sample rate, mono or stereo.
  * PUBLIC DOMAIN or CC0 / CC-BY ONLY (pre-1929 78s from archive.org are public
    domain in the US; Musopen and the Free Music Archive carry CC0/CC-BY jazz).
    Add every file to MUSIC_LICENSES.txt with its source and licence; CC-BY needs
    the credit line in any public build.
  * Hot jazz, swing and bebop at 110–200 bpm work best: the bed follows the
    tempo it measures, and the combat bed doubles it.
  * Nothing to rename, no tags to set: every file found here joins a POOL on the
    next import. Pools: menu (the lobby, the report card, the walk after the kill),
    city, under, office (the floors), boss. A file's chart can name its pool
    (pool=boss); the rest go round-robin through the four floor pools in name
    order, never the lobby. A level starts on its own side of its floor's pool
    (level number modulo the pool size) and rotates through the pool from there;
    the boss pool is chosen the same way, so a fight keeps its side.

What the engine does with a file
--------------------------------
  * It measures the TEMPO (onset autocorrelation, 80–220 bpm), tracks the BEATS
    through the whole song (so a band that speeds up stays on the grid), finds the
    DOWNBEAT and the KEY (chroma + Krumhansl), and reads one chord root per bar
    for the sub bass. This happens on a worker thread the first time the song is
    reached; the song plays as soon as its lead has been analysed.
  * It then renders the BEDS (explore / combat / flow / boss / menu) as clips of
    exactly the song's length, armed on the same DSP clock, so a change of mood
    swaps the bed on the bar line while the recording keeps playing.

Optional chart file (overrides the analysis)
--------------------------------------------
  Next to "<name>.ogg" put "<name>.chart.txt" with any of these lines:

      bpm=126.5        the beat the bed rides: the tracker's period (the beats still
                       follow the band's drift); the half-note pulse of a 2/2 side
      offset=0.84      seconds from the start of the file to the first downbeat
      key=Fm           the key: a letter, an optional # or b, and m / min / minor
                       for minor (F, F#, Bb, Fm, Bbmin, Eb minor all parse)
      swing=0.62       where the off-beat eighth falls, 0.5 straight … 0.67 triplet
      gain=0.8         a level trim (1 = the engine's own -15 dBFS RMS levelling)
      pool=boss        menu, city, under, office or boss (see above)
      fixed=1          a FIXED grid from bpm and offset, no tracking (only for a
                       modern, quantised recording: the 78s drift up to 5%)

  Use it for a recording the analysis gets wrong (a rubato intro, a tempo in the
  wrong octave, a key a semitone off). The twelve sides shipped here all carry one,
  baked from the offline run of the same analyser (Tools: see the chart comments).

Import settings (set automatically by Assets/Scripts/Editor/MusicImportSettings.cs)
-----------------------------------------------------------------------------------
  Files in THIS folder import as Vorbis, DecompressOnLoad, preload OFF, load in
  background ON, mono: the engine decodes a song to PCM only while it is the one
  playing (or next in the rotation) and reads its samples (AudioClip.GetData) for the
  analysis, the mastering and the stutter edits. A Compressed In Memory or Streaming
  clip cannot be read — that is how the score went silent on 2026-10-11 (the sides
  had been imported before the script knew this folder). Since 2026-10-12 the script
  re-checks every lead on each script reload and reimports any that disagrees, and
  the engine never goes silent anyway: a side it cannot read plays RAW (as decoded,
  brought up by the pump, no bed) after one "MUSIC: … lead unavailable" warning; a
  pool with nothing playable falls back to the procedural band. The F3 readout's
  MUSIC line says which (lead OK / RAW / BAND).
