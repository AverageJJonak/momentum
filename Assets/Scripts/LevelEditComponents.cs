using System;
using System.Collections.Generic;
using UnityEngine;

// THE LEVEL EDITOR'S MARKS (2026-10-20). Momentum → Level Editor builds a level in the editor, under one root called
// Level_Holder, so it can be fixed by hand; these components ride on that working copy only. They live in the runtime
// assembly because a component on a scene object must, but the game never adds them, and the working copy is never
// written into a scene (HideFlags.DontSaveInEditor) and is cleared before Play. What the hand fixes become is a
// LevelEdits patch (LevelEdits.cs), which the game lays over the level it builds.

/// A piece the builders made: who it was when built, so the editor can tell what a hand changed
[AddComponentMenu("")]
public class LevelPiece : MonoBehaviour
{
    /// LevelEdits key: the name path from its root and where it stood, to the centimetre
    public string key;
    /// The object's entity id (as text) when it was marked: a Ctrl+D copy carries the same text, so a copy knows it is one
    public string origin;
    /// Its parent's key as built ("" = a root of the level)
    public string gParent;
    /// Its local transform as built
    public Vector3 gPos;
    public Quaternion gRot = Quaternion.identity;
    public Vector3 gScale = Vector3.one;
    /// Its local transform after the passes the editor runs for you (the coplanar fix): "untouched" means this
    public Vector3 bPos;
    public Quaternion bRot = Quaternion.identity;
    public Vector3 bScale = Vector3.one;
    /// Its material's name as built (untiled; "" = no renderer)
    public string gMat;
    public bool gActive = true;
    /// An edit from the saved file was laid on it when the level was generated: kept on the next save even if untouched
    public bool hadEdit;
}

/// Made by a pass after the builders (the HARD LINE beads, the reflection probes, the spawn's helpers): never an edit
[AddComponentMenu("")]
public class LevelDerived : MonoBehaviour { }

/// An addition the patch put back when the level was generated: saved again as an addition, wherever it now is
[AddComponentMenu("")]
public class LevelAdded : MonoBehaviour
{
    /// The key of the piece it is a copy of ("" = a primitive)
    public string source;
    /// Cube, Sphere, Cylinder, Capsule, Plane or Quad when it is not a copy
    public string primitive;
}

/// The player's spawn in the working copy: move the gold capsule and the level starts there
[AddComponentMenu("")]
public class LevelSpawnMarker : MonoBehaviour { }

/// On Level_Holder: which level this is and every key the builders made, so a deleted piece is noticed
[AddComponentMenu("")]
public class LevelHolder : MonoBehaviour
{
    public LevelKind kind;
    /// Every piece's key as built, parents before children
    public List<string> keys = new List<string>();
    /// The index of each key's parent in `keys` (-1 = a root of the level)
    public List<int> parentOf = new List<int>();
    /// The spawn as built (feet)
    public Vector3 spawn;
    /// Edits in the file that found no piece when the level was generated (a builder has changed since)
    public List<string> orphans = new List<string>();
    /// How many edits and additions the file laid on the level when it was generated
    public int applied;
}

/// The level editor's buttons, reached from a runtime component's context menu (GameBootstrap): the editor sets these
/// when it loads; in a build they stay empty and the menu does nothing
public static class LevelEditorHooks
{
    public static Action<GameBootstrap> Generate, Save, Clear;
}
