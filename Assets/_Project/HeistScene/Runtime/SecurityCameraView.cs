using AFirma.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Câmera de segurança na parede, varrendo a sala com um cone que muda de cor com o alerta
    /// (verde → amarelo a partir de 35 → vermelho a partir de 70).
    /// Quando o Disquete usa Loop na câmera, o cone apaga e a câmera fica piscando "fora do ar" pelo tempo da habilidade.
    /// </summary>
    public class SecurityCameraView : MonoBehaviour
    {
        public SpriteRenderer body;
        public Transform pivot;
        public SpriteRenderer cone;
        public Light2D coneLight;
        public float sweepMin = -55f, sweepMax = -15f;
        public float sweepSpeed = 0.35f;

        static readonly Color Calm = new Color(0.35f, 0.66f, 0.39f, 0.32f);
        static readonly Color Suspicious = new Color(0.97f, 0.83f, 0.41f, 0.38f);
        static readonly Color Alarmed = new Color(0.8f, 0.2f, 0.22f, 0.45f);

        float t;
        float loopTimer;
        Color current = Calm;

        void OnEnable()
        {
            GameEvents.AlertChanged += OnAlert;
            GameEvents.AbilityUsed += OnAbility;
            if (Services.IsInitialized && Services.Heist != null && Services.Heist.IsRunning) OnAlert(Services.Heist.Alert);
            else OnAlert(0f);
        }

        void OnDisable()
        {
            GameEvents.AlertChanged -= OnAlert;
            GameEvents.AbilityUsed -= OnAbility;
        }

        void OnAlert(float alert)
        {
            current = alert >= GuardView.AlarmedAt ? Alarmed : alert >= GuardView.SuspiciousAt ? Suspicious : Calm;
            ApplyColor();
        }

        void OnAbility(ThiefState thief, ThiefAbilityDef ability)
        {
            if (thief == null || thief.type != ThiefType.Hacker) return;
            float seconds = ability is SimpleAbilityDef simple && simple.freezeAlertSeconds > 0f ? simple.freezeAlertSeconds : 15f;
            loopTimer = Mathf.Max(loopTimer, seconds);
        }

        public bool Looping => loopTimer > 0f;

        void ApplyColor()
        {
            if (cone != null) cone.color = current;
            if (coneLight != null)
            {
                var c = current;
                c.a = 1f;
                coneLight.color = c;
            }
        }

        void Update()
        {
            float dt = SceneClock.Delta;
            if (loopTimer > 0f)
            {
                loopTimer -= dt;
                bool flicker = Mathf.Repeat(loopTimer * 3f, 1f) > 0.5f;
                if (cone != null) cone.enabled = false;
                if (coneLight != null) coneLight.enabled = false;
                if (body != null) body.color = flicker ? new Color(0.45f, 0.9f, 0.55f) : Color.white;
                return;
            }
            if (cone != null) cone.enabled = true;
            if (coneLight != null) coneLight.enabled = true;
            if (body != null) body.color = Color.white;

            float speed = current == Alarmed ? sweepSpeed * 2.2f : sweepSpeed;
            t += dt * speed;
            float angle = Mathf.Lerp(sweepMin, sweepMax, (Mathf.Sin(t * Mathf.PI) + 1f) / 2f);
            if (pivot != null) pivot.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
