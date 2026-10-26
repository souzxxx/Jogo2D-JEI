using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>Balão de fala em cima de um personagem, com a fonte pixel.</summary>
    public class SpeechBubble : MonoBehaviour
    {
        SpriteRenderer bg, tail;
        PixelText text;
        float timeLeft;
        Transform follow;
        Vector3 offset;

        public static SpeechBubble Create(HeistSceneArt art, Transform parent)
        {
            var go = new GameObject("Balão");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<SpeechBubble>();
            b.bg = new GameObject("fundo").AddComponent<SpriteRenderer>();
            b.bg.transform.SetParent(go.transform, false);
            b.bg.sprite = art.bubble;
            b.bg.drawMode = SpriteDrawMode.Sliced;
            b.bg.sortingOrder = SortOrder.Bubble;
            b.tail = new GameObject("ponta").AddComponent<SpriteRenderer>();
            b.tail.transform.SetParent(go.transform, false);
            b.tail.sprite = art.bubbleTail;
            b.tail.sortingOrder = SortOrder.Bubble + 1;
            b.text = new GameObject("texto").AddComponent<PixelText>();
            b.text.transform.SetParent(go.transform, false);
            b.text.font = art.font;
            b.text.color = new Color(0.055f, 0.047f, 0.082f, 1f);
            b.text.maxWidth = 110;
            b.text.sortingOrder = SortOrder.Bubble + 2;
            go.SetActive(false);
            return b;
        }

        public void Show(string line, Transform target, Vector3 headOffset, float seconds = 2.6f)
        {
            if (string.IsNullOrEmpty(line)) return;
            follow = target;
            offset = headOffset;
            gameObject.SetActive(true);
            var size = text.SetText(line);
            float w = (size.x + 8) / 32f, h = (size.y + 6) / 32f;
            bg.size = new Vector2(w, h);
            // o balão fica com a base 6 px acima da cabeça; a ponta aponta pra baixo
            bg.transform.localPosition = new Vector3(0f, h / 2f + 4f / 32f, 0f);
            text.transform.localPosition = new Vector3(0f, h / 2f + 5f / 32f, 0f);
            tail.transform.localPosition = new Vector3(-2f / 32f, 2f / 32f, 0f);
            timeLeft = seconds;
            Place();
        }

        public void Hide() => gameObject.SetActive(false);

        void LateUpdate()
        {
            timeLeft -= SceneClock.UiDelta;
            if (timeLeft <= 0f || follow == null)
            {
                Hide();
                return;
            }
            Place();
        }

        void Place()
        {
            if (follow == null) return;
            var p = follow.position + offset;
            transform.position = new Vector3(Mathf.Round(p.x * 32f) / 32f, Mathf.Round(p.y * 32f) / 32f, 0f);
        }
    }
}
