using UnityEngine;

/// <summary>
/// Marks the Level Editor's working copy (Level_Holder, Momentum → Level Editor) so GameBootstrap clears it on
/// Play (the runtime build would otherwise duplicate the whole world).
/// </summary>
public class LevelPreviewMarker : MonoBehaviour { }
