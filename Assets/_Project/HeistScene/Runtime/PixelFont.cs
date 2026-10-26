using System.Collections.Generic;
using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Fonte pixel do jogo (maiúscula, minúscula, número e acento). O atlas é gerado por
    /// HeistScene/Art/Source~/gerar_cena.py e fatiado pelo HeistSceneBuilder.
    /// A UI pode usar também: Get(c) devolve o sprite da letra.
    /// </summary>
    [CreateAssetMenu(menuName = "A Firma/Cena do Golpe/Fonte pixel", fileName = "FontePixel")]
    public class PixelFont : ScriptableObject
    {
        [Tooltip("Caracteres na mesma ordem dos glyphs.")]
        public string chars;
        public Sprite[] glyphs;
        public int[] widths;
        public int cellHeight = 11;
        [Tooltip("Linhas abaixo da base (descendente do g, p, q...).")]
        public int descent = 2;
        public int spacing = 1;
        public int spaceWidth = 3;

        Dictionary<char, int> index;

        public Sprite Get(char c, out int advance)
        {
            if (index == null)
            {
                index = new Dictionary<char, int>();
                for (int i = 0; i < chars.Length && i < glyphs.Length; i++) index[chars[i]] = i;
            }
            if (c == ' ')
            {
                advance = spaceWidth + spacing;
                return null;
            }
            if (!index.TryGetValue(c, out int k) && !index.TryGetValue('?', out k))
            {
                advance = spaceWidth;
                return null;
            }
            advance = widths[k] + spacing;
            return glyphs[k];
        }

        public int Measure(string text)
        {
            int w = 0;
            foreach (char c in text)
            {
                Get(c, out int advance);
                w += advance;
            }
            return Mathf.Max(0, w - spacing);
        }
    }
}
