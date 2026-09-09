using System.Collections.Generic;
using UnityEngine;

namespace Neon
{
    public enum TextAlign { Left, Center, Right }

    /// Draws a string from the 5x7 bitmap atlas as a row of SpriteRenderers.
    /// No uGUI, no TextMeshPro, no font assets - just the atlas that ships in Resources.
    public class PixelLabel : MonoBehaviour
    {
        public const int Cell = 6, CellH = 8, AtlasCols = 16, AtlasRows = 4;

        public float scale = 4f;
        public TextAlign align = TextAlign.Left;
        public Color color = Color.white;
        public int order = Layer.Ui;
        public float shadow = 1f;      // texels of drop shadow, 0 disables

        readonly List<SpriteRenderer> glyphs = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> shadows = new List<SpriteRenderer>();
        string current = "";

        public float Advance => Cell / Art.PPU * scale;
        public float LineHeight => CellH / Art.PPU * scale * 1.35f;
        public string Text => current;

        public static PixelLabel Create(Transform parent, string name, float scale,
                                        TextAlign align, Color col, int order = Layer.Ui)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = parent != null ? parent.gameObject.layer : 0;
            var l = go.AddComponent<PixelLabel>();
            l.scale = scale;
            l.align = align;
            l.color = col;
            l.order = order;
            return l;
        }

        public void SetText(string text)
        {
            text ??= "";
            if (text == current) return;
            current = text;
            Rebuild();
        }

        public void SetColor(Color col)
        {
            color = col;
            for (int i = 0; i < glyphs.Count; i++)
                if (glyphs[i] != null) glyphs[i].color = col;
        }

        void Rebuild()
        {
            string[] lines = current.Split('\n');
            int needed = 0;
            foreach (var l in lines) needed += l.Length;

            EnsurePool(glyphs, needed, false);
            EnsurePool(shadows, shadow > 0f ? needed : 0, true);

            int gi = 0;
            for (int li = 0; li < lines.Length; li++)
            {
                string line = lines[li];
                float w = line.Length * Advance;
                float x0 = align switch
                {
                    TextAlign.Center => -w * 0.5f,
                    TextAlign.Right => -w,
                    _ => 0f
                };
                float y = -li * LineHeight;

                for (int ci = 0; ci < line.Length; ci++, gi++)
                {
                    char ch = char.ToUpperInvariant(line[ci]);
                    int idx = ch - 32;
                    var sr = glyphs[gi];
                    if (idx < 0 || idx >= AtlasCols * AtlasRows || ch == ' ')
                    {
                        sr.enabled = false;
                        if (shadow > 0f) shadows[gi].enabled = false;
                        continue;
                    }
                    int col = idx % AtlasCols;
                    int row = idx / AtlasCols;
                    var rect = new Rect(col * Cell, (AtlasRows - 1 - row) * CellH, Cell, CellH);
                    var sprite = Art.Sub("ui_font", rect, Vector2.zero);

                    sr.enabled = true;
                    sr.sprite = sprite;
                    sr.color = color;
                    sr.sortingOrder = order;
                    sr.transform.localScale = Vector3.one * scale;
                    sr.transform.localPosition = new Vector3(x0 + ci * Advance, y, 0f);

                    if (shadow > 0f)
                    {
                        var sh = shadows[gi];
                        sh.enabled = true;
                        sh.sprite = sprite;
                        sh.color = new Color(0f, 0f, 0f, color.a * 0.75f);
                        sh.sortingOrder = order - 1;
                        sh.transform.localScale = Vector3.one * scale;
                        sh.transform.localPosition = new Vector3(
                            x0 + ci * Advance + shadow / Art.PPU * scale,
                            y - shadow / Art.PPU * scale, 0f);
                    }
                }
            }
            for (int i = gi; i < glyphs.Count; i++)
            {
                glyphs[i].enabled = false;
                if (i < shadows.Count) shadows[i].enabled = false;
            }
        }

        void EnsurePool(List<SpriteRenderer> pool, int count, bool isShadow)
        {
            while (pool.Count < count)
            {
                var go = new GameObject(isShadow ? "s" : "g");
                go.transform.SetParent(transform, false);
                go.layer = gameObject.layer;
                var sr = go.AddComponent<SpriteRenderer>();
                pool.Add(sr);
            }
        }
    }
}
