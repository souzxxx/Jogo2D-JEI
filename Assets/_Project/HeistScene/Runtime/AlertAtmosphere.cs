using AFirma.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AFirma.HeistScene
{
    /// <summary>
    /// O clima do golpe na tela inteira: vinheta que fecha conforme o alerta (e fica vermelha na sirene),
    /// bloom leve, luz ambiente do prédio que esfria com o alerta e luz de viatura (vermelho/azul) entrando
    /// pela janela durante a contagem da polícia.
    /// </summary>
    public class AlertAtmosphere : MonoBehaviour
    {
        public Volume volume;
        public Light2D globalLight;
        public Light2D policeRed;
        public Light2D policeBlue;
        public Color ambient = Color.white;
        public float ambientIntensity = 0.6f;

        Vignette vignette;
        Bloom bloom;
        float alert, shownAlert;
        bool siren;
        float flash, policeT;

        void Awake()
        {
            if (volume != null)
            {
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "Clima do golpe";
                vignette = profile.Add<Vignette>(true);
                vignette.smoothness.Override(0.45f);
                vignette.rounded.Override(false);
                bloom = profile.Add<Bloom>(true);
                bloom.intensity.Override(0.35f);
                bloom.threshold.Override(0.95f);
                volume.isGlobal = true;
                volume.profile = profile;
            }
            SetPolice(false);
        }

        void OnEnable()
        {
            GameEvents.AlertChanged += OnAlert;
            GameEvents.SirenStarted += OnSiren;
            GameEvents.HeistStarted += OnStarted;
            GameEvents.HeistFinished += OnFinished;
        }

        void OnDisable()
        {
            GameEvents.AlertChanged -= OnAlert;
            GameEvents.SirenStarted -= OnSiren;
            GameEvents.HeistStarted -= OnStarted;
            GameEvents.HeistFinished -= OnFinished;
        }

        public void SetAmbient(Color color, float intensity)
        {
            ambient = color;
            ambientIntensity = intensity;
        }

        void OnStarted(HeistDef heist)
        {
            siren = false;
            alert = shownAlert = 0f;
            SetPolice(false);
        }

        void OnAlert(float value) => alert = value;

        void OnSiren(float seconds)
        {
            siren = true;
            flash = 1f;
            SetPolice(true);
        }

        void OnFinished(HeistResult result) => siren = false;

        void SetPolice(bool on)
        {
            if (policeRed != null) policeRed.enabled = on;
            if (policeBlue != null) policeBlue.enabled = on;
        }

        void Update()
        {
            float dt = SceneClock.UiDelta;
            shownAlert = Mathf.MoveTowards(shownAlert, alert, dt * 60f);
            float a = shownAlert / 100f;
            if (vignette != null)
            {
                vignette.intensity.Override(Mathf.Lerp(0.18f, 0.48f, a) + flash * 0.2f);
                var c = siren ? new Color(0.35f, 0.02f, 0.05f) : Color.Lerp(Color.black, new Color(0.25f, 0.03f, 0.05f), Mathf.Clamp01((a - 0.6f) / 0.4f));
                vignette.color.Override(c);
            }
            flash = Mathf.Max(0f, flash - dt * 1.5f);
            if (globalLight != null)
            {
                // quanto mais alerta, mais a luz do prédio esfria e escurece: o clima pesa
                globalLight.color = Color.Lerp(ambient, new Color(0.55f, 0.6f, 0.85f), a * 0.5f);
                globalLight.intensity = ambientIntensity * Mathf.Lerp(1f, 0.82f, a) + flash * 0.25f;
            }
            if (siren && policeRed != null && policeBlue != null)
            {
                policeT += dt * 3.2f;
                bool red = Mathf.FloorToInt(policeT) % 2 == 0;
                policeRed.intensity = red ? 1.4f : 0.15f;
                policeBlue.intensity = red ? 0.15f : 1.6f;
            }
        }
    }
}
