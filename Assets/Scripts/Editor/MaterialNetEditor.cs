using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The editor's half of MaterialNet: Unity's own verdict on a shader (ShaderUtil.ShaderHasError) and its messages, so
/// the console names the broken shader and its first errors instead of "not supported". Also the menu that runs the
/// net over the open scene by hand: Momentum → Look → Check Materials.
/// </summary>
[InitializeOnLoad]
public static class MaterialNetEditor
{
    static MaterialNetEditor()
    {
        MaterialNet.EditorError = shader =>
        {
            if (shader == null || !ShaderUtil.ShaderHasError(shader)) return null;
            var sb = new StringBuilder("Shader '" + shader.name + "' has errors:");
            var messages = ShaderUtil.GetShaderMessages(shader);
            int n = 0;
            foreach (var m in messages)
            {
                if (m.severity != UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error) continue;
                sb.Append("\n  " + m.file + "(" + m.line + "): " + m.message + (string.IsNullOrEmpty(m.messageDetails) ? "" : " — " + m.messageDetails));
                if (++n >= 6) { sb.Append("\n  …"); break; }
            }
            return sb.ToString();
        };
    }

    [MenuItem("Momentum/Look/Check Materials", false, 51)]
    static void CheckMaterials()
    {
        int rescued = MaterialNet.Run(null);
        Debug.Log("MaterialNet: " + (rescued == 0 ? "every material in the scene is on a shader Unity can use." : rescued + " renderer(s) were rescued; see the warning above for the shader and its errors."));
    }
}
