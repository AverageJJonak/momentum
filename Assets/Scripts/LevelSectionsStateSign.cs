using UnityEngine;

/// <summary>
/// A holo line whose words can change: the sign lives on its own host, so a lesson can rewrite it (the Ward's BARE
/// HANDS board). Lived in the cloakroom's file until the cloakroom went (the simplification, 2026-10-16).
/// </summary>
public static partial class LevelSections
{
    static readonly Color HoloInk = LevelKit.SignCream;   // the sign's own letters: lit cream on the charcoal plate

    public class StateSign
    {
        public TextMesh tm;
        float size, maxW;

        public StateSign(Transform root, Vector3 at, Vector3 facing, float w, float h, string line)
        {
            var host = new GameObject("StateSign").transform;
            host.SetParent(root);
            LevelKit.HoloSign(host, at, facing, w, h, line);
            tm = host.GetComponentInChildren<TextMesh>();
            size = h * 0.03f; maxW = w - 0.3f;
        }

        public void Set(string text, Color? color = null)
        {
            if (tm == null) return;
            tm.text = text;
            tm.color = color ?? HoloInk;
            LevelKit.Fit(tm, size, maxW);
        }
    }
}
