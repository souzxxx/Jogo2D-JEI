using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>Ícone que pula no mundo, sobe um pouco e some (check, X, "!", algemas, ícone de habilidade...).</summary>
    public class PopIcon : MonoBehaviour
    {
        [Tooltip("Usado quando o PopIcon já vem num prefab (ex.: o \"CREC!\" do Pé-de-cabra).")]
        public float life = 1f;
        public float rise = 0.4f;

        SpriteRenderer sr;
        float age;
        Vector3 start;
        bool started;

        void Start()
        {
            if (started) return;
            started = true;
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            start = transform.position;
        }

        public static PopIcon Spawn(Sprite sprite, Vector3 position, float life = 1.2f, float rise = 0.5f, int order = SortOrder.Fx + 5)
        {
            if (sprite == null) return null;
            var go = new GameObject("Pop " + sprite.name);
            go.transform.position = position;
            var pop = go.AddComponent<PopIcon>();
            pop.sr = go.AddComponent<SpriteRenderer>();
            pop.sr.sprite = sprite;
            pop.sr.sortingOrder = order;
            pop.life = life;
            pop.rise = rise;
            pop.start = position;
            pop.started = true;
            return pop;
        }

        void Update()
        {
            if (sr == null) return;
            age += SceneClock.UiDelta;
            float t = age / life;
            // "pulinho" no começo e subida suave depois, sempre em pixel inteiro
            float y = t < 0.15f ? Mathf.Sin(t / 0.15f * Mathf.PI) * 0.15f : (t - 0.15f) * rise;
            transform.position = start + new Vector3(0f, Snap(y), 0f);
            var c = sr.color;
            c.a = t < 0.7f ? 1f : Mathf.Clamp01(1f - (t - 0.7f) / 0.3f);
            sr.color = c;
            if (age >= life) Destroy(gameObject);
        }

        static float Snap(float v) => Mathf.Round(v * 32f) / 32f;
    }
}
