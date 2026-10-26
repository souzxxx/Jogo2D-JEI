using AFirma.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AFirma.HeistScene
{
    /// <summary>Porta no vão da parede da direita da sala. Abre quando a crew decide ir pra próxima sala.</summary>
    public class DoorView : MonoBehaviour
    {
        public SpriteRenderer sr;
        public Sprite closed;
        public Sprite open;
        public SpriteRenderer highlight;

        float pulse;
        bool highlighted;

        public bool IsOpen { get; private set; }

        public void SetOpen(bool isOpen)
        {
            IsOpen = isOpen;
            if (sr != null) sr.sprite = isOpen ? open : closed;
            SetHighlight(false);
        }

        public void SetHighlight(bool on)
        {
            highlighted = on;
            if (highlight != null) highlight.enabled = on;
        }

        void Update()
        {
            if (!highlighted || highlight == null) return;
            pulse += SceneClock.UiDelta * 5f;
            highlight.color = new Color(1f, 0.85f, 0.4f, Mathf.Sin(pulse) > 0f ? 0.9f : 0.3f);
        }
    }
}
