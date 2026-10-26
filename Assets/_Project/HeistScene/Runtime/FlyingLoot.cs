using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>Uma cédula (ou moeda) voando em arco da sala até a sacola da crew.</summary>
    public class FlyingLoot : MonoBehaviour
    {
        Vector3 from;
        Transform target;
        Vector3 targetOffset;
        float duration, age, delay, arc;
        SpriteRenderer sr;
        System.Action onArrive;

        public static FlyingLoot Spawn(Sprite sprite, Vector3 from, Transform target, Vector3 targetOffset, float delay, System.Action onArrive)
        {
            var go = new GameObject("Grana");
            go.transform.position = from;
            var fl = go.AddComponent<FlyingLoot>();
            fl.sr = go.AddComponent<SpriteRenderer>();
            fl.sr.sprite = sprite;
            fl.sr.sortingOrder = SortOrder.Fx + 2;
            fl.sr.enabled = false;
            fl.from = from + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.1f, 0.3f), 0f);
            fl.target = target;
            fl.targetOffset = targetOffset;
            fl.delay = delay;
            fl.duration = Random.Range(0.55f, 0.8f);
            fl.arc = Random.Range(0.8f, 1.4f);
            fl.onArrive = onArrive;
            return fl;
        }

        void Update()
        {
            float dt = SceneClock.UiDelta;
            if (delay > 0f)
            {
                delay -= dt;
                return;
            }
            sr.enabled = true;
            age += dt;
            float t = Mathf.Clamp01(age / duration);
            var to = target != null ? target.position + targetOffset : from;
            var p = Vector3.Lerp(from, to, t * t * (3f - 2f * t));
            p.y += Mathf.Sin(t * Mathf.PI) * arc;
            transform.position = new Vector3(Mathf.Round(p.x * 32f) / 32f, Mathf.Round(p.y * 32f) / 32f, 0f);
            sr.flipX = (age * 10f) % 2f > 1f; // a nota "vira" no ar
            if (t >= 1f)
            {
                onArrive?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
