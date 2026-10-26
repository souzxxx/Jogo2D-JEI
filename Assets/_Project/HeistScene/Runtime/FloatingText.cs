using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>Texto que sobe e some (ex.: "+Cr$ 150" quando a sala é limpa).</summary>
    public class FloatingText : MonoBehaviour
    {
        PixelText text;
        float age, life;
        Vector3 start;

        public static FloatingText Spawn(PixelFont font, string message, Color color, Vector3 position, float life = 1.6f)
        {
            if (font == null) return null;
            var go = new GameObject("Texto " + message);
            go.transform.position = position;
            var ft = go.AddComponent<FloatingText>();
            ft.text = go.AddComponent<PixelText>();
            ft.text.font = font;
            ft.text.color = color;
            ft.text.sortingOrder = SortOrder.Fx + 6;
            ft.text.SetText(message);
            ft.life = life;
            ft.start = position;
            // sombra: mesma frase 1 px abaixo, escura
            var shadow = new GameObject("sombra").AddComponent<PixelText>();
            shadow.transform.SetParent(go.transform, false);
            shadow.transform.localPosition = new Vector3(1f / 32f, -1f / 32f, 0f);
            shadow.font = font;
            shadow.color = new Color(0.05f, 0.05f, 0.08f, 1f);
            shadow.sortingOrder = SortOrder.Fx + 5;
            shadow.SetText(message);
            return ft;
        }

        void Update()
        {
            age += SceneClock.UiDelta;
            float t = age / life;
            transform.position = start + new Vector3(0f, Mathf.Round(t * 0.8f * 32f) / 32f, 0f);
            float a = t < 0.6f ? 1f : Mathf.Clamp01(1f - (t - 0.6f) / 0.4f);
            foreach (var pt in GetComponentsInChildren<PixelText>()) pt.SetAlpha(a);
            if (age >= life) Destroy(gameObject);
        }
    }
}
