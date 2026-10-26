using System;
using UnityEngine;

namespace AFirma.HeistScene
{
    [Serializable]
    public class FlipbookAnim
    {
        public string name;
        public float fps = 8f;
        public bool loop = true;
        public Sprite[] frames;
        [Tooltip("Frames da segunda camada (ex.: a roupa tingida do ladrão genérico). Opcional.")]
        public Sprite[] overlay;
    }

    /// <summary>
    /// Animação por troca de sprite, sem Animator: usada pelos NPCs, pelo ladrão genérico e pelos veículos.
    /// Respeita a pausa e a velocidade do golpe (SceneClock).
    /// </summary>
    public class SpriteFlipbook : MonoBehaviour
    {
        public SpriteRenderer body;
        public SpriteRenderer overlay;
        public FlipbookAnim[] anims = new FlipbookAnim[0];
        public string startAnim = "idle";
        [Tooltip("Multiplica o fps (correr = andar mais rápido).")]
        public float speed = 1f;

        FlipbookAnim current;
        float time;
        public bool Finished { get; private set; }
        public string Current => current != null ? current.name : null;

        void Awake()
        {
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (current == null) Play(startAnim);
        }

        public bool Has(string anim) => Find(anim) != null;

        public void Play(string anim, bool restart = false)
        {
            var next = Find(anim);
            if (next == null) return;
            if (next == current && !restart) return;
            current = next;
            time = 0f;
            Finished = false;
            Apply(0);
        }

        FlipbookAnim Find(string anim)
        {
            foreach (var a in anims)
                if (a != null && a.name == anim && a.frames != null && a.frames.Length > 0) return a;
            return null;
        }

        void Update()
        {
            if (current == null) return;
            time += SceneClock.Delta * speed;
            int count = current.frames.Length;
            int index = Mathf.FloorToInt(time * current.fps);
            if (current.loop) index %= count;
            else if (index >= count)
            {
                index = count - 1;
                Finished = true;
            }
            Apply(index);
        }

        void Apply(int index)
        {
            if (body != null) body.sprite = current.frames[index];
            if (overlay != null)
            {
                bool has = current.overlay != null && current.overlay.Length > index;
                overlay.enabled = has;
                if (has) overlay.sprite = current.overlay[index];
            }
        }

        public void SetFlip(bool flipX)
        {
            if (body != null) body.flipX = flipX;
            if (overlay != null) overlay.flipX = flipX;
        }
    }
}
