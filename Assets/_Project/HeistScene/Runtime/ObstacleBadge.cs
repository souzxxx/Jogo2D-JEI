using AFirma.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Ícone do obstáculo flutuando na sala + anel de progresso em volta + aviso de "sem especialista".
    /// </summary>
    public class ObstacleBadge : MonoBehaviour
    {
        public SpriteRenderer plate;
        public SpriteRenderer icon;
        public SpriteRenderer ring;
        public SpriteRenderer warning;
        public Sprite[] ringFrames;
        public Sprite checkIcon;

        enum State { Hidden, Preview, Active, Cleared }
        State state;
        float bob, fade;
        Vector3 basePos;

        void Awake() => basePos = transform.localPosition;

        public void Setup(Sprite obstacleIcon)
        {
            icon.sprite = obstacleIcon;
            SetProgress(0f);
            SetWarning(false);
        }

        public void SetWarning(bool noSpecialist)
        {
            if (warning != null) warning.enabled = noSpecialist && state != State.Cleared && state != State.Hidden;
            warningWanted = noSpecialist;
        }

        bool warningWanted;

        public void ShowHidden() => SetState(State.Hidden);
        public void ShowPreview() => SetState(State.Preview);
        public void ShowActive() => SetState(State.Active);

        public void ShowCleared()
        {
            SetState(State.Cleared);
            icon.sprite = checkIcon;
            SetProgress(1f);
            fade = 1.6f;
        }

        void SetState(State s)
        {
            state = s;
            bool visible = s != State.Hidden;
            plate.enabled = icon.enabled = visible;
            ring.enabled = s == State.Active || s == State.Cleared;
            if (warning != null) warning.enabled = visible && s != State.Cleared && warningWanted;
            SetAlpha(1f);
        }

        public void SetProgress(float p)
        {
            if (ringFrames == null || ringFrames.Length == 0) return;
            int i = Mathf.Clamp(Mathf.FloorToInt(p * (ringFrames.Length - 1) + 0.0001f), 0, ringFrames.Length - 1);
            ring.sprite = ringFrames[i];
        }

        void SetAlpha(float a)
        {
            foreach (var r in new[] { plate, icon, ring, warning })
                if (r != null)
                {
                    var c = r.color;
                    c.a = a;
                    r.color = c;
                }
        }

        void Update()
        {
            float dt = SceneClock.UiDelta;
            bob += dt;
            // flutua 1 px pra cima e pra baixo; na prévia pisca chamando atenção pra próxima porta
            float y = Mathf.Sin(bob * 2.5f) > 0f ? 1f / 32f : 0f;
            transform.localPosition = basePos + new Vector3(0f, y, 0f);
            if (state == State.Preview) SetAlpha(Mathf.Sin(bob * 6f) > -0.2f ? 1f : 0.35f);
            if (state == State.Cleared && fade > 0f)
            {
                fade -= dt;
                SetAlpha(Mathf.Clamp01(fade / 0.6f));
                if (fade <= 0f) plate.enabled = icon.enabled = ring.enabled = false;
            }
        }
    }
}
