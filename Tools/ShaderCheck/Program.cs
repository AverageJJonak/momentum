// SHADER CHECK: compiles the passes of the project's hand-written URP shaders with d3dcompiler_47.dll, the compiler
// Unity itself uses for D3D11, outside Unity. The compiler resolves the includes itself through an include callback
// (the Packages/ paths map to Library/PackageCache), so #if blocks and include guards behave exactly; on the way in,
// #pragma lines are dropped and zero-argument function-like macros ("#define PopMarker()", which Unity's own
// preprocessor accepts and fxc's rejects) become object-like ones. Unity's platform defines are set, and every keyword
// set listed in Jobs() is compiled as a vertex and a pixel shader. Errors come back per file and line.
//   cd Tools/ShaderCheck && dotnet run      (OK / ERROR per compile and a summary; exit code 1 on errors)
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

static class Program
{
    static readonly string Project = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    static string Shaders => Path.Combine(Project, "Assets", "Shaders");
    static string Cache => Path.Combine(Project, "Library", "PackageCache");
    static readonly Dictionary<string, string> packages = new Dictionary<string, string>();

    // The job list: shader file, pass name, vertex entry, pixel entry, keyword sets (space separated)
    static IEnumerable<(string file, string pass, string vs, string ps, string[] keywordSets)> Jobs()
    {
        string[] lit =
        {
            "",
            "_NORMALMAP _METALLICSPECGLOSSMAP _OCCLUSIONMAP _MAIN_LIGHT_SHADOWS_CASCADE _ADDITIONAL_LIGHTS _ADDITIONAL_LIGHT_SHADOWS _SHADOWS_SOFT _SCREEN_SPACE_OCCLUSION _LIGHT_COOKIES _LIGHT_LAYERS _CLUSTER_LIGHT_LOOP _WRITE_RENDERING_LAYERS FOG_LINEAR EVALUATE_SH_MIXED _REFLECTION_PROBE_BLENDING _REFLECTION_PROBE_BOX_PROJECTION _REFLECTION_PROBE_ATLAS",
            "_MAIN_LIGHT_SHADOWS _ADDITIONAL_LIGHTS_VERTEX EVALUATE_SH_VERTEX FOG_EXP2",
        };
        foreach (var f in new[] { "MomentumToon.shader", "MomentumToonTransparent.shader" })
        {
            yield return (f, "ForwardLit", "ToonVertex", "ToonFragment", lit);
            yield return (f, "ShadowCaster", "ShadowPassVertex", "ShadowPassFragment", new[] { "_CASTING_PUNCTUAL_LIGHT_SHADOW" });
            yield return (f, "DepthOnly", "DepthOnlyVertex", "DepthOnlyFragment", new[] { "" });
            yield return (f, "DepthNormals", "DepthNormalsVertex", "DepthNormalsFragment", new[] { "_NORMALMAP _WRITE_RENDERING_LAYERS" });
        }
        yield return ("MomentumToon.shader", "Outline", "OutlineVertex", "OutlineFragment", new[] { "FOG_LINEAR" });
        yield return ("MomentumSky.shader", "Sky", "SkyVertex", "SkyFragment", new[] { "" });
        yield return ("MomentumSheet.shader", "Sheet", "SheetVertex", "SheetFragment", new[] { "", "FOG_LINEAR", "FOG_EXP2" });
    }

    static int Main()
    {
        foreach (var d in Directory.GetDirectories(Cache))
        {
            string n = Path.GetFileName(d);
            int at = n.IndexOf('@');
            if (at > 0) packages[n.Substring(0, at)] = d;
        }
        int failures = 0, jobs = 0;
        foreach (var job in Jobs())
        {
            string path = Path.Combine(Shaders, job.file);
            string shader = File.ReadAllText(path);
            string block = PassBlock(shader, job.pass, out int firstLine);
            if (block == null) { Say("ERROR  " + job.file + " has no pass " + job.pass); failures++; continue; }
            string src = Clean(block);
            foreach (var kw in job.keywordSets)
            {
                foreach (var stage in new[] { ("vs_5_0", job.vs, "SHADER_STAGE_VERTEX"), ("ps_5_0", job.ps, "SHADER_STAGE_FRAGMENT") })
                {
                    jobs++;
                    string label = job.file + " · " + job.pass + " · " + stage.Item2 + " [" + (kw.Length == 0 ? "no keywords" : kw) + "]";
                    string err = Compile(src, path, stage.Item2, stage.Item1, Defines(kw, stage.Item3));
                    if (err == null) { Say("OK     " + label); continue; }
                    failures++;
                    Say("ERROR  " + label);
                    foreach (var line in err.Split((char)10))
                    {
                        string t = line.Trim();
                        if (t.Length == 0 || t.Contains(": warning X")) continue;   // errors only: the library warns on every half() conversion
                        // The pass block's own lines are numbered from the HLSLPROGRAM line of the .shader file
                        var m = Regex.Match(t, @"^(.*?)\((\d+),(\d+)(?:-\d+)?\):\s*(.*)$");
                        if (m.Success && m.Groups[1].Value.EndsWith(job.file) && int.TryParse(m.Groups[2].Value, out int ln))
                            Say("       " + job.file + ":" + (firstLine + ln - 1) + ": " + m.Groups[4].Value);
                        else Say("       " + t);
                    }
                }
            }
        }
        Say(failures == 0 ? "ALL " + jobs + " COMPILES OK" : failures + " of " + jobs + " compiles FAILED");
        return failures == 0 ? 0 : 1;
    }

    static void Say(string s) { Console.WriteLine(s); Console.Out.Flush(); }

    /// The HLSLPROGRAM … ENDHLSL text of the named pass, and the file line its first line is on
    static string PassBlock(string shader, string pass, out int firstLine)
    {
        firstLine = 0;
        int name = shader.IndexOf("Name \"" + pass + "\"", StringComparison.Ordinal);
        if (name < 0) return null;
        int start = shader.IndexOf("HLSLPROGRAM", name, StringComparison.Ordinal);
        int end = shader.IndexOf("ENDHLSL", start, StringComparison.Ordinal);
        if (start < 0 || end < 0) return null;
        for (int i = 0; i < start; i++) if (shader[i] == '\n') firstLine++;
        firstLine += 1;   // the HLSLPROGRAM line itself; the block's first line is the rest of it
        return shader.Substring(start + "HLSLPROGRAM".Length, end - start - "HLSLPROGRAM".Length);
    }

    /// What Unity's own preprocessor would have taken care of: #pragma lines go, #include_with_pragmas is an #include,
    /// a zero-argument function-like macro becomes an object-like one (fxc's preprocessor rejects "#define X()")
    static string Clean(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var raw in text.Split((char)10))
        {
            string line = raw.TrimEnd('\r');
            string t = line.TrimStart();
            if (t.StartsWith("#pragma")) line = "";
            else if (t.StartsWith("#include_with_pragmas")) line = line.Replace("#include_with_pragmas", "#include");
            else
            {
                var m = Regex.Match(line, @"^(\s*#define\s+[A-Za-z_]\w*)\(\)(.*)$");
                if (m.Success) line = m.Groups[1].Value + m.Groups[2].Value;
            }
            sb.Append(line).Append((char)10);
        }
        return sb.ToString();
    }

    static string Resolve(string inc, string parentDir)
    {
        const string pk = "Packages/";
        if (inc.StartsWith(pk))
        {
            string rest = inc.Substring(pk.Length);
            int slash = rest.IndexOf('/');
            if (slash < 0) return null;
            string pkg = rest.Substring(0, slash);
            if (!packages.TryGetValue(pkg, out string root)) return null;
            string p = Path.Combine(root, rest.Substring(slash + 1).Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(p) ? p : null;
        }
        string local = Path.Combine(parentDir, inc.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(local)) return local;
        string inShaders = Path.Combine(Shaders, inc.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(inShaders) ? inShaders : null;
    }

    static List<(string, string)> Defines(string keywords, string stage)
    {
        var d = new List<(string, string)>
        {
            ("SHADER_API_D3D11", "1"), ("SHADER_API_DESKTOP", "1"), ("SHADER_TARGET", "50"), ("SHADER_TARGET_AVAILABLE", "50"),
            ("UNITY_VERSION", "600005"), ("UNITY_COMPILER_HLSL", "1"), ("UNITY_UNIFIED_SHADER_PRECISION_MODEL", "1"),
            ("UNITY_PLATFORM_SUPPORTS_DEPTH_FETCH", "1"), ("UNITY_ENABLE_REFLECTION_BUFFERS", "1"),
            ("UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS", "1"), ("UNITY_PBS_USE_BRDF1", "1"), ("UNITY_SPECCUBE_BOX_PROJECTION", "1"),
            ("UNITY_SPECCUBE_BLENDING", "1"), ("UNITY_ENABLE_DETAIL_NORMALMAP", "1"), ("UNITY_LIGHT_PROBE_PROXY_VOLUME", "1"),
            ("UNITY_LIGHTMAP_FULL_HDR", "1"), (stage, "1"),
        };
        foreach (var k in keywords.Split(' ', StringSplitOptions.RemoveEmptyEntries)) d.Add((k, "1"));
        return d;
    }

    // ------------------------------------------------------------ d3dcompiler, with an include callback

    [StructLayout(LayoutKind.Sequential)]
    struct Macro { public IntPtr Name; public IntPtr Definition; }

    [DllImport("d3dcompiler_47.dll", CharSet = CharSet.Ansi)]
    static extern int D3DCompile(byte[] src, nuint size, string sourceName, Macro[] defines, IntPtr include, string entry, string target, uint flags1, uint flags2, out IntPtr code, out IntPtr errors);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate IntPtr GetBufferPointer(IntPtr self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate nuint GetBufferSize(IntPtr self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate uint Release(IntPtr self);

    // ID3DInclude: a C++ interface of two methods (no IUnknown): Open(type, fileName, parentData, out data, out bytes), Close(data)
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int OpenFn(IntPtr self, int includeType, IntPtr fileName, IntPtr parentData, out IntPtr data, out uint bytes);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int CloseFn(IntPtr self, IntPtr data);

    static readonly OpenFn openFn = Open;
    static readonly CloseFn closeFn = Close;
    static IntPtr includeObject;
    /// The folder each handed-out buffer came from, so a relative include inside it resolves
    static readonly Dictionary<IntPtr, string> bufferDir = new Dictionary<IntPtr, string>();
    static string rootDir;

    static IntPtr Include()
    {
        if (includeObject != IntPtr.Zero) return includeObject;
        IntPtr vtable = Marshal.AllocHGlobal(2 * IntPtr.Size);
        Marshal.WriteIntPtr(vtable, 0, Marshal.GetFunctionPointerForDelegate(openFn));
        Marshal.WriteIntPtr(vtable, IntPtr.Size, Marshal.GetFunctionPointerForDelegate(closeFn));
        includeObject = Marshal.AllocHGlobal(IntPtr.Size);
        Marshal.WriteIntPtr(includeObject, vtable);
        return includeObject;
    }

    static int Open(IntPtr self, int includeType, IntPtr fileName, IntPtr parentData, out IntPtr data, out uint bytes)
    {
        data = IntPtr.Zero; bytes = 0;
        string name = Marshal.PtrToStringAnsi(fileName) ?? "";
        string parentDir = parentData != IntPtr.Zero && bufferDir.TryGetValue(parentData, out var pd) ? pd : rootDir;
        string full = Resolve(name, parentDir);
        if (full == null) return unchecked((int)0x80070002);   // not found
        var text = Encoding.UTF8.GetBytes(Clean(File.ReadAllText(full)));
        data = Marshal.AllocHGlobal(text.Length);
        Marshal.Copy(text, 0, data, text.Length);
        bytes = (uint)text.Length;
        bufferDir[data] = Path.GetDirectoryName(full);
        return 0;
    }

    static int Close(IntPtr self, IntPtr data)
    {
        if (data != IntPtr.Zero) { bufferDir.Remove(data); Marshal.FreeHGlobal(data); }
        return 0;
    }

    /// Null when it compiled; the compiler's error text otherwise
    static string Compile(string source, string name, string entry, string target, List<(string, string)> defines)
    {
        rootDir = Path.GetDirectoryName(name);
        var macros = new Macro[defines.Count + 1];
        var pins = new List<IntPtr>();
        for (int i = 0; i < defines.Count; i++)
        {
            macros[i].Name = Marshal.StringToHGlobalAnsi(defines[i].Item1); pins.Add(macros[i].Name);
            macros[i].Definition = Marshal.StringToHGlobalAnsi(defines[i].Item2); pins.Add(macros[i].Definition);
        }
        var bytes = Encoding.UTF8.GetBytes(source);
        const uint OptimizationLevel0 = 1u << 14;   // a full compile, the fast way
        int hr = D3DCompile(bytes, (nuint)bytes.Length, name, macros, Include(), entry, target, OptimizationLevel0, 0, out IntPtr code, out IntPtr errors);
        foreach (var p in pins) Marshal.FreeHGlobal(p);
        string text = null;
        if (errors != IntPtr.Zero)
        {
            text = BlobText(errors);
            ReleaseBlob(errors);
        }
        if (code != IntPtr.Zero) ReleaseBlob(code);
        if (hr >= 0) return null;   // warnings only
        return text ?? ("HRESULT 0x" + hr.ToString("X8"));
    }

    static string BlobText(IntPtr blob)
    {
        IntPtr vtable = Marshal.ReadIntPtr(blob);
        var ptrFn = Marshal.GetDelegateForFunctionPointer<GetBufferPointer>(Marshal.ReadIntPtr(vtable, 3 * IntPtr.Size));
        var sizeFn = Marshal.GetDelegateForFunctionPointer<GetBufferSize>(Marshal.ReadIntPtr(vtable, 4 * IntPtr.Size));
        IntPtr p = ptrFn(blob);
        int n = (int)sizeFn(blob);
        var bytes = new byte[n];
        Marshal.Copy(p, bytes, 0, n);
        return Encoding.ASCII.GetString(bytes).TrimEnd('\0');
    }

    static void ReleaseBlob(IntPtr blob)
    {
        IntPtr vtable = Marshal.ReadIntPtr(blob);
        Marshal.GetDelegateForFunctionPointer<Release>(Marshal.ReadIntPtr(vtable, 2 * IntPtr.Size))(blob);
    }
}
