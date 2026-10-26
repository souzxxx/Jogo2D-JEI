using System.Collections.Generic;
using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Escreve texto no mundo com a fonte pixel do jogo (um SpriteRenderer por letra), alinhado ao centro.
    /// Serve pros balões de fala e pro "+Cr$" que sobe da sala. Quebra linha pela largura máxima.
    /// </summary>
    public class PixelText : MonoBehaviour
    {
        public PixelFont font;
        public Color color = Color.white;
        public int sortingOrder = SortOrder.Bubble + 1;
        [Tooltip("Largura máxima da linha em pixels (0 = sem quebra).")]
        public int maxWidth;
        public int lineSpacing = 1;

        readonly List<SpriteRenderer> glyphs = new List<SpriteRenderer>();
        public Vector2Int SizePx { get; private set; }

        const float Ppu = 32f;

        public Vector2Int SetText(string text)
        {
            foreach (var g in glyphs) g.gameObject.SetActive(false);
            if (font == null || string.IsNullOrEmpty(text))
            {
                SizePx = Vector2Int.zero;
                return SizePx;
            }

            var lines = Wrap(text);
            int lineH = font.cellHeight - 2 + lineSpacing; // as 2 linhas de acento de cima só usam espaço quando tem acento
            int width = 0;
            foreach (var line in lines) width = Mathf.Max(width, font.Measure(line));
            int height = lines.Count * lineH;
            int used = 0;
            for (int l = 0; l < lines.Count; l++)
            {
                string line = lines[l];
                int x = -font.Measure(line) / 2;
                int y = height / 2 - (l + 1) * lineH;
                foreach (char c in line)
                {
                    var sprite = font.Get(c, out int advance);
                    if (sprite != null)
                    {
                        var r = Glyph(used++);
                        r.sprite = sprite;
                        r.color = color;
                        r.sortingOrder = sortingOrder;
                        r.transform.localPosition = new Vector3(x / Ppu, y / Ppu, 0f);
                    }
                    x += advance;
                }
            }
            SizePx = new Vector2Int(width, height);
            return SizePx;
        }

        public void SetColor(Color c)
        {
            color = c;
            foreach (var g in glyphs) g.color = c;
        }

        public void SetAlpha(float a)
        {
            foreach (var g in glyphs)
            {
                var c = g.color;
                c.a = a;
                g.color = c;
            }
        }

        SpriteRenderer Glyph(int index)
        {
            while (glyphs.Count <= index)
            {
                var go = new GameObject("g");
                go.transform.SetParent(transform, false);
                glyphs.Add(go.AddComponent<SpriteRenderer>());
            }
            var r = glyphs[index];
            r.gameObject.SetActive(true);
            return r;
        }

        List<string> Wrap(string text)
        {
            var lines = new List<string>();
            if (maxWidth <= 0)
            {
                lines.Add(text);
                return lines;
            }
            var words = text.Split(' ');
            string current = "";
            foreach (var word in words)
            {
                string attempt = current.Length == 0 ? word : current + " " + word;
                if (font.Measure(attempt) > maxWidth && current.Length > 0)
                {
                    lines.Add(current);
                    current = word;
                }
                else current = attempt;
            }
            if (current.Length > 0) lines.Add(current);
            return lines;
        }
    }
}
