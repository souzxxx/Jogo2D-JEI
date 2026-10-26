using AFirma.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Guarda ou pessoa da sala. Dá pra ler o alerta só olhando pra ele:
    /// calmo (lanterna branca, passo lento) → desconfiado "?" (lanterna amarela) a partir de 35 →
    /// alarmado "!" (lanterna vermelha, passo rápido) a partir de 70 → na sirene sai correndo.
    ///
    /// Modo Posto: é o obstáculo "Pessoa" da sala (Seu Zé, crupiê...). Fica parado olhando a crew,
    /// desconfia enquanto o 171 trabalha e, quando a sala é limpa, fica distraído ("zZ").
    /// </summary>
    public class GuardView : MonoBehaviour
    {
        public enum Mode { Patrol, Post }

        public Mode mode = Mode.Patrol;
        public SpriteFlipbook flipbook;
        public SpriteRenderer mark;
        public Light2D flashlight;
        public SpriteRenderer cone;
        public float minX, maxX;
        public float walkSpeed = 0.7f;
        public Sprite doubtIcon, alertIcon, sleepIcon;

        public const float SuspiciousAt = 35f, AlarmedAt = 70f;

        enum Level { Calm, Suspicious, Alarmed, Siren }

        Level level;
        float dir = 1f;
        float pauseTimer;
        float blink;
        bool distracted;
        bool roomActive;
        bool working;

        static readonly Color CalmLight = new Color(1f, 0.95f, 0.8f);
        static readonly Color SuspiciousLight = new Color(1f, 0.78f, 0.25f);
        static readonly Color AlarmedLight = new Color(1f, 0.25f, 0.2f);

        void OnEnable()
        {
            GameEvents.AlertChanged += OnAlert;
            GameEvents.SirenStarted += OnSiren;
            if (Services.IsInitialized && Services.Heist != null && Services.Heist.IsRunning)
            {
                OnAlert(Services.Heist.Alert);
                if (Services.Heist.SirenActive) OnSiren(0f);
            }
            else Apply();
        }

        void OnDisable()
        {
            GameEvents.AlertChanged -= OnAlert;
            GameEvents.SirenStarted -= OnSiren;
        }

        void OnAlert(float alert)
        {
            if (level == Level.Siren) return;
            var next = alert >= AlarmedAt ? Level.Alarmed : alert >= SuspiciousAt ? Level.Suspicious : Level.Calm;
            if (next == level) return;
            level = next;
            Apply();
        }

        void OnSiren(float seconds)
        {
            level = Level.Siren;
            distracted = false;
            Apply();
        }

        /// <summary>A crew entrou ou saiu da sala deste guarda.</summary>
        public void SetRoomActive(bool active, bool crewWorking)
        {
            roomActive = active;
            working = crewWorking;
            Apply();
        }

        /// <summary>Sala limpa: a pessoa de posto fica distraída (o 171 ganhou a conversa).</summary>
        public void Distract()
        {
            if (mode != Mode.Post) return;
            distracted = true;
            Apply();
        }

        void Apply()
        {
            if (flashlight != null)
            {
                flashlight.color = level == Level.Calm ? CalmLight : level == Level.Suspicious ? SuspiciousLight : AlarmedLight;
                flashlight.intensity = level == Level.Calm ? 0.8f : 1.2f;
            }
            if (cone != null)
            {
                var c = flashlight != null ? flashlight.color : CalmLight;
                c.a = level == Level.Calm ? 0.35f : 0.5f;
                cone.color = c;
            }
            Sprite icon = null;
            if (distracted && level != Level.Siren) icon = sleepIcon;
            else if (level == Level.Alarmed || level == Level.Siren) icon = alertIcon;
            else if (level == Level.Suspicious || (mode == Mode.Post && roomActive && working)) icon = doubtIcon;
            if (mark != null)
            {
                mark.sprite = icon;
                mark.enabled = icon != null;
            }
        }

        void Update()
        {
            float dt = SceneClock.Delta;
            if (mark != null && mark.enabled && (level == Level.Siren || level == Level.Alarmed))
            {
                blink += dt * 6f;
                mark.enabled = true;
                mark.color = new Color(1f, 1f, 1f, Mathf.Sin(blink) > -0.3f ? 1f : 0.2f);
            }
            else if (mark != null) mark.color = Color.white;

            if (mode == Mode.Post)
            {
                Face(true);
                if (distracted && level != Level.Siren) Play("distraido");
                else if (level >= Level.Alarmed) Play("alerta");
                else Play("idle");
                return;
            }

            if (pauseTimer > 0f)
            {
                pauseTimer -= dt;
                Play(level >= Level.Alarmed ? "alerta" : "idle");
                return;
            }

            float speed = walkSpeed * (level == Level.Siren ? 3f : level == Level.Alarmed ? 1.8f : level == Level.Suspicious ? 1.3f : 1f);
            var p = transform.localPosition;
            p.x += dir * speed * dt;
            if (p.x > maxX || p.x < minX)
            {
                p.x = Mathf.Clamp(p.x, minX, maxX);
                dir = -dir;
                pauseTimer = level == Level.Siren ? 0.1f : Random.Range(0.8f, 2f);   // olha em volta antes de voltar
            }
            transform.localPosition = p;
            Face(dir < 0f);
            Play("andar");
            if (flipbook != null) flipbook.speed = speed / walkSpeed;
        }

        void Play(string anim)
        {
            if (flipbook == null) return;
            if (!flipbook.Has(anim)) anim = "idle";
            flipbook.Play(anim);
        }

        void Face(bool left)
        {
            if (flipbook != null) flipbook.SetFlip(left);
            if (flashlight != null) flashlight.transform.localRotation = Quaternion.Euler(0f, 0f, left ? 90f : -90f);
            if (flashlight != null)
            {
                var lp = flashlight.transform.localPosition;
                lp.x = Mathf.Abs(lp.x) * (left ? -1f : 1f);
                flashlight.transform.localPosition = lp;
            }
            if (cone != null)
            {
                cone.flipX = left;
                var cp = cone.transform.localPosition;
                cp.x = Mathf.Abs(cp.x) * (left ? -1f : 1f);
                cone.transform.localPosition = cp;
            }
        }
    }
}
