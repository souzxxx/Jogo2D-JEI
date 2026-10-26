using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Van da firma (Kombi da Auto Elétrica da Cida) ou viatura. A van espera a crew e arranca na fuga;
    /// a viatura chega na sirene com o giroflex ligado. Fumaça de escapamento e, no Cavalo de pau, rodopio com fumaça de pneu.
    /// </summary>
    public class VehicleView : MonoBehaviour
    {
        public SpriteRenderer sr;
        public Sprite[] frames;
        public float frameRate = 6f;
        public ParticleSystem exhaust;
        public UnityEngine.Rendering.Universal.Light2D[] flashers = new UnityEngine.Rendering.Universal.Light2D[0];

        Vector3 target;
        bool moving;
        float speed, accel, maxSpeed = 9f;
        float frameTime;
        float spin;
        bool flashing;
        public bool Arrived => !moving;

        public void Park(Vector3 at, bool faceLeft = false)
        {
            transform.position = at;
            moving = false;
            sr.flipX = faceLeft;
        }

        /// <summary>Anda até um ponto (com aceleração), virado pra onde vai.</summary>
        public void DriveTo(Vector3 to, float startSpeed = 0f, float acceleration = 6f)
        {
            target = to;
            moving = true;
            speed = startSpeed;
            accel = acceleration;
            sr.flipX = to.x < transform.position.x;
            if (exhaust != null) exhaust.Play();
        }

        /// <summary>Cavalo de pau: rodopia no lugar antes de arrancar.</summary>
        public void Spin(float seconds = 0.8f) => spin = seconds;

        public void SetFlashing(bool on)
        {
            flashing = on;
            foreach (var l in flashers) if (l != null) l.enabled = on;
        }

        void Update()
        {
            float dt = SceneClock.Delta;
            if (spin > 0f)
            {
                spin -= dt;
                sr.flipX = Mathf.Repeat(spin * 8f, 2f) > 1f;
                if (exhaust != null && !exhaust.isPlaying) exhaust.Play();
            }
            if (moving && spin <= 0f)
            {
                speed = Mathf.Min(maxSpeed, speed + accel * dt);
                var p = Vector3.MoveTowards(transform.position, target, speed * dt);
                transform.position = new Vector3(Mathf.Round(p.x * 32f) / 32f, p.y, 0f);
                sr.flipX = target.x < transform.position.x || (Mathf.Approximately(target.x, transform.position.x) && sr.flipX);
                if ((transform.position - target).sqrMagnitude < 0.0004f)
                {
                    moving = false;
                    if (exhaust != null) exhaust.Stop();
                }
            }
            // roda girando quando anda; giroflex sempre que ligado
            bool animate = moving || flashing || spin > 0f;
            if (animate && frames != null && frames.Length > 1)
            {
                frameTime += dt * frameRate;
                sr.sprite = frames[Mathf.FloorToInt(frameTime) % frames.Length];
                for (int i = 0; i < flashers.Length; i++)
                    if (flashers[i] != null && flashing)
                        flashers[i].intensity = (Mathf.FloorToInt(frameTime) % 2 == i % 2) ? 2.2f : 0.2f;
            }
        }
    }
}
