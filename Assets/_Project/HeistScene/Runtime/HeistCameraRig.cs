using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Câmera 2D da cena (com Pixel Perfect Camera, PPU 32, referência 640x360): segue a crew de sala em sala,
    /// não sai dos limites do prédio e treme de leve na sirene e no Pé-de-cabra.
    /// </summary>
    public class HeistCameraRig : MonoBehaviour
    {
        public Camera cam;
        public float followTime = 0.35f;

        Vector2 target;
        float minX = -100f, maxX = 100f;
        Vector3 velocity;
        float shakeTime, shakeAmp;

        public float HalfWidth => cam != null ? cam.orthographicSize * cam.aspect : 10f;

        public void SetBounds(float min, float max)
        {
            minX = min;
            maxX = max;
        }

        public void Follow(Vector2 point, bool snap = false)
        {
            target = point;
            if (snap)
            {
                transform.position = Clamp(new Vector3(point.x, point.y, transform.position.z));
                velocity = Vector3.zero;
            }
        }

        public void Shake(float amplitude, float seconds)
        {
            shakeAmp = Mathf.Max(shakeAmp, amplitude);
            shakeTime = Mathf.Max(shakeTime, seconds);
        }

        Vector3 Clamp(Vector3 p)
        {
            float half = HalfWidth;
            if (maxX - minX > half * 2f) p.x = Mathf.Clamp(p.x, minX + half, maxX - half);
            else p.x = (minX + maxX) / 2f;
            return p;
        }

        void LateUpdate()
        {
            if (SceneClock.Paused) return;
            var goal = Clamp(new Vector3(target.x, target.y, transform.position.z));
            var p = Vector3.SmoothDamp(transform.position, goal, ref velocity, followTime, Mathf.Infinity, Mathf.Min(Time.unscaledDeltaTime, 0.1f));
            if (shakeTime > 0f)
            {
                shakeTime -= SceneClock.UiDelta;
                float a = shakeAmp * Mathf.Clamp01(shakeTime * 3f);
                p += new Vector3(Random.Range(-a, a), Random.Range(-a, a), 0f);
                if (shakeTime <= 0f) shakeAmp = 0f;
            }
            transform.position = p;
        }
    }
}
