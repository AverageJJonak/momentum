using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE CITY'S LETTERING: block capitals five cells wide and seven tall, every
/// horizontal run of lit cells one box, so a name is a few dozen boxes and the
/// whole sign welds into the city. The living signs (MenuCityLife: the
/// company's name on the Court, with the tell) and the still ones here (a
/// hotel's rooftop name in cream bulbs on a brass frame, a theatre's blade
/// sign reading down its face) share the one alphabet. Partial of MenuCity,
/// compiled by the menu harness too: plain UnityEngine, no LINQ.
/// </summary>
public static partial class MenuCity
{
    /// A LIT NAME, still: cream block letters on a charcoal plate in a brass frame on two iron posts `legH` high,
    /// facing `outward`. `at` is the posts' feet, `width` the plate's
    static void StillSign(MeshBaker boxes, Vector3 at, Vector3 outward, float width, string line, float legH)
    {
        if (Cut(at, width * 0.5f)) return;
        Vector3 right = Vector3.Cross(outward, Vector3.up);
        Quaternion turn = Quaternion.LookRotation(outward, Vector3.up);
        int cellsWide = Cells(line);
        float c = (width - 1.6f) / cellsWide;
        float plateH = 7f * c + 3.2f * c;
        Vector3 plateC = at + Vector3.up * (legH + plateH * 0.5f);
        Box(boxes, signBack, plateC, new Vector3(width, plateH, 0.3f), turn);
        Vector3 proud = outward * 0.1f;
        Box(boxes, brass, plateC + proud + Vector3.up * (plateH * 0.5f), new Vector3(width + 0.4f, 0.3f, 0.4f), turn);
        Box(boxes, brass, plateC + proud - Vector3.up * (plateH * 0.5f), new Vector3(width + 0.4f, 0.3f, 0.4f), turn);
        Box(boxes, brass, plateC + proud + right * (width * 0.5f), new Vector3(0.3f, plateH + 0.3f, 0.4f), turn);
        Box(boxes, brass, plateC + proud - right * (width * 0.5f), new Vector3(0.3f, plateH + 0.3f, 0.4f), turn);
        for (int s = -1; s <= 1; s += 2)
            Box(boxes, iron, at + right * (s * (width * 0.5f - 0.8f)) + Vector3.up * ((legH + plateH) * 0.5f), new Vector3(0.35f, legH + plateH, 0.35f), turn);
        Vector3 origin = new Vector3(plateC.x, plateC.y + plateH * 0.5f - 1.6f * c, plateC.z) - right * (cellsWide * c * 0.5f) + outward * 0.33f;
        BlockLetters(boxes, boxes, null, line, origin, right, c, turn, -1, 0);
    }

    /// A BLADE SIGN: the letters of `word` stacked down a charcoal plate on a face, reading from the top at `top`,
    /// each letter `letterW` wide, cream, the plate edged in brass
    static void BladeSign(MeshBaker boxes, Vector3 top, Vector3 outward, float letterW, string word)
    {
        if (Cut(top, 2f)) return;
        Vector3 right = Vector3.Cross(outward, Vector3.up);
        Quaternion turn = Quaternion.LookRotation(outward, Vector3.up);
        float c = letterW / 5f, h = word.Length * 8f * c + c;
        Vector3 plateC = top - Vector3.up * (h * 0.5f) + outward * 0.35f;
        Box(boxes, signBack, plateC, new Vector3(letterW + 1.2f, h, 0.3f), turn);
        Box(boxes, brass, plateC + right * (letterW * 0.5f + 0.6f) + outward * 0.1f, new Vector3(0.25f, h, 0.4f), turn);
        Box(boxes, brass, plateC - right * (letterW * 0.5f + 0.6f) + outward * 0.1f, new Vector3(0.25f, h, 0.4f), turn);
        for (int k = 0; k < word.Length; k++)
        {
            string[] g = Glyph(word[k]);
            if (g == null) continue;
            Vector3 origin = new Vector3(plateC.x, top.y - c - k * 8f * c, plateC.z) - right * (GlyphWidth(g) * c * 0.5f) + outward * 0.33f;
            BlockLetters(boxes, boxes, null, word[k].ToString(), origin, right, c, turn, -1, 0);
        }
    }

    /// One line of block letters as boxes: `origin` is the line's top-left corner on the plate's face, `right` the way
    /// it runs, `c` a cell. Every horizontal run of lit cells is one box. The letter numbered `tell` (letters only,
    /// counted from `index`) goes into `held` (and, if given, `heldRed` in red, in the same place) instead of `into`.
    /// Returns the count so far
    static int BlockLetters(MeshBaker into, MeshBaker held, MeshBaker heldRed, string line, Vector3 origin, Vector3 right, float c, Quaternion turn, int tell, int index)
    {
        float x = 0f;
        foreach (char ch in line)
        {
            string[] g = Glyph(ch);
            int wide = GlyphWidth(g);
            if (g != null)
            {
                bool isTell = index == tell;
                MeshBaker target = isTell ? held : into;
                for (int row = 0; row < 7; row++)
                {
                    string bits = g[row];
                    int col = 0;
                    while (col < bits.Length)
                    {
                        if (bits[col] != '1') { col++; continue; }
                        int run = 0;
                        while (col + run < bits.Length && bits[col + run] == '1') run++;
                        Vector3 centre = origin + right * ((x + col + run * 0.5f) * c) - Vector3.up * ((row + 0.5f) * c);
                        Vector3 size = new Vector3(run * c - 0.08f, c - 0.08f, 0.32f);
                        target.Add(glowWhite, centre, size, turn);
                        if (isTell && heldRed != null) heldRed.Add(glowRed, centre, size, turn);
                        col += run;
                    }
                }
                index++;
            }
            x += wide + 1;
        }
        return index;
    }

    /// How many cells a line is wide: its glyphs and a cell between each
    static int Cells(string line)
    {
        int cells = 0;
        foreach (char ch in line) cells += GlyphWidth(Glyph(ch)) + 1;
        return Mathf.Max(1, cells - 1);
    }

    /// A long line in two, broken at the space nearest its middle
    static string[] SplitLine(string line)
    {
        if (line.Length <= 11) return new[] { line };
        int best = -1, mid = line.Length / 2, bestOff = int.MaxValue;
        for (int i = 1; i < line.Length - 1; i++)
        {
            if (line[i] != ' ') continue;
            int off = i - mid;
            if (off < 0) off = -off;
            if (off < bestOff) { bestOff = off; best = i; }
        }
        if (best < 0) return new[] { line };
        return new[] { line.Substring(0, best), line.Substring(best + 1) };
    }

    /// Which letter of the line is the tell: one of them, by the sign's seed
    static int TellIndex(string line, int seed)
    {
        int letters = 0;
        foreach (char ch in line) if (Glyph(ch) != null) letters++;
        if (letters == 0) return -1;
        return Mathf.FloorToInt(CityTextures.Hash01(seed, 7, 1939) * letters) % letters;
    }

    // Block capitals, five cells wide and seven tall, row by row from the top, '1' a lit cell; the marks are narrower
    static readonly string[] GlyphTable =
    {
        "A 01110 10001 10001 11111 10001 10001 10001", "B 11110 10001 10001 11110 10001 10001 11110", "C 01110 10001 10000 10000 10000 10001 01110",
        "D 11110 10001 10001 10001 10001 10001 11110", "E 11111 10000 10000 11110 10000 10000 11111", "F 11111 10000 10000 11110 10000 10000 10000",
        "G 01110 10001 10000 10111 10001 10001 01111", "H 10001 10001 10001 11111 10001 10001 10001", "I 11111 00100 00100 00100 00100 00100 11111",
        "J 00111 00010 00010 00010 00010 10010 01100", "K 10001 10010 10100 11000 10100 10010 10001", "L 10000 10000 10000 10000 10000 10000 11111",
        "M 10001 11011 10101 10101 10001 10001 10001", "N 10001 10001 11001 10101 10011 10001 10001", "O 01110 10001 10001 10001 10001 10001 01110",
        "P 11110 10001 10001 11110 10000 10000 10000", "Q 01110 10001 10001 10001 10101 10010 01101", "R 11110 10001 10001 11110 10100 10010 10001",
        "S 01111 10000 10000 01110 00001 00001 11110", "T 11111 00100 00100 00100 00100 00100 00100", "U 10001 10001 10001 10001 10001 10001 01110",
        "V 10001 10001 10001 10001 10001 01010 00100", "W 10001 10001 10001 10101 10101 10101 01010", "X 10001 10001 01010 00100 01010 10001 10001",
        "Y 10001 10001 01010 00100 00100 00100 00100", "Z 11111 00001 00010 00100 01000 10000 11111",
        "0 01110 10001 10011 10101 11001 10001 01110", "1 00100 01100 00100 00100 00100 00100 01110", "2 01110 10001 00001 00010 00100 01000 11111",
        "3 11111 00010 00100 00010 00001 10001 01110", "4 00010 00110 01010 10010 11111 00010 00010", "5 11111 10000 11110 00001 00001 10001 01110",
        "6 00110 01000 10000 11110 10001 10001 01110", "7 11111 00001 00010 00100 01000 01000 01000", "8 01110 10001 10001 01110 10001 10001 01110",
        "9 01110 10001 10001 01111 00001 00010 01100",
        ". 00 00 00 00 00 11 11", ", 00 00 00 00 11 01 10", "' 11 01 10 00 00 00 00", "- 00000 00000 00000 11111 00000 00000 00000",
        "& 01100 10010 10010 01100 10101 10010 01101",
    };
    static Dictionary<char, string[]> glyphs;

    /// A glyph's seven rows, or null for a space and for any mark the signs have no letter for (both three cells wide)
    static string[] Glyph(char ch)
    {
        if (glyphs == null)
        {
            glyphs = new Dictionary<char, string[]>();
            foreach (var entry in GlyphTable)
            {
                var parts = entry.Split(' ');
                if (parts.Length < 8) continue;
                var rows = new string[7];
                for (int i = 0; i < 7; i++) rows[i] = parts[i + 1];
                glyphs[parts[0][0]] = rows;
            }
        }
        return glyphs.TryGetValue(char.ToUpperInvariant(ch), out var g) ? g : null;
    }

    static int GlyphWidth(string[] g) => g == null ? 3 : g[0].Length;
}
