using AFirma.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AFirma.HeistScene
{
    /// <summary>
    /// O obstáculo da sala no mundo: grade com cadeado, cofre, painel de alarme, CFTV... (fechado/aberto)
    /// ou a pessoa de posto (Pessoa). Cada sala tem um por tipo de obstáculo e liga só o da RoomData.
    /// </summary>
    public class ObstacleProp : MonoBehaviour
    {
        public ObstacleType obstacle;
        public SpriteRenderer sr;
        public Sprite closed;
        public Sprite open;
        public GuardView person;
        [Tooltip("Onde o especialista fica pra trabalhar (filho do adereço).")]
        public Transform workPoint;

        float shake;
        Vector3 basePos;
        bool hasBase;

        public void SetOpen(bool isOpen)
        {
            if (sr != null && closed != null) sr.sprite = isOpen ? (open != null ? open : closed) : closed;
            if (isOpen && person != null) person.Distract();
        }

        /// <summary>Tremidinha enquanto o Gazua mexe na fechadura ou o cofre resiste.</summary>
        public void Jiggle(float amount = 1f)
        {
            if (sr == null) return;
            if (!hasBase)
            {
                basePos = sr.transform.localPosition;
                hasBase = true;
            }
            shake = Mathf.Max(shake, 0.25f * amount);
        }

        void Update()
        {
            if (shake <= 0f || sr == null) return;
            shake -= SceneClock.Delta;
            float px = (Mathf.Repeat(Time.unscaledTime * 20f, 2f) < 1f ? 1f : -1f) / 32f;
            sr.transform.localPosition = shake > 0f ? basePos + new Vector3(px, 0f, 0f) : basePos;
        }
    }
}
