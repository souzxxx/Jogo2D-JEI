using AFirma.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AFirma.HeistScene
{
    /// <summary>Giroflex vermelho no teto da sala: liga a partir de 70% de alerta e gira mais rápido na sirene.</summary>
    public class AlarmLight : MonoBehaviour
    {
        public SpriteRenderer beacon;
        public Light2D beam;
        public float onAt = 70f;

        bool on, siren;
        float angle;

        void OnEnable()
        {
            GameEvents.AlertChanged += OnAlert;
            GameEvents.SirenStarted += OnSiren;
            bool running = Services.IsInitialized && Services.Heist != null && Services.Heist.IsRunning;
            siren = running && Services.Heist.SirenActive;
            OnAlert(running ? Services.Heist.Alert : 0f);
        }

        void OnDisable()
        {
            GameEvents.AlertChanged -= OnAlert;
            GameEvents.SirenStarted -= OnSiren;
        }

        void OnAlert(float alert) => Set(siren || alert >= onAt);

        void OnSiren(float seconds)
        {
            siren = true;
            Set(true);
        }

        void Set(bool value)
        {
            on = value;
            if (beam != null) beam.enabled = on;
            if (beacon != null) beacon.color = on ? Color.white : new Color(0.45f, 0.35f, 0.4f);
        }

        void Update()
        {
            if (!on || beam == null) return;
            angle += SceneClock.Delta * (siren ? 540f : 300f);
            beam.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            if (beacon != null) beacon.enabled = Mathf.Repeat(angle, 180f) < 120f;
        }
    }
}
