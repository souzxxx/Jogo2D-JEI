using UnityEngine;

namespace AFirma.Core
{
    [CreateAssetMenu(menuName = "A Firma/Bico", fileName = "SideJob")]
    public class SideJobDef : DefBase
    {
        public const int SceneWidth = 96;
        public const int SceneHeight = 64;
        public const int MinSceneFrames = 4;
        public const int MaxSceneFrames = 8;

        [Tooltip("Falso = renda fixa sem ladrão (Catar latinha).")]
        public bool requiresThief = true;
        public bool hasPreferredType = true;
        public ThiefType preferredType;
        public double incomePerSecond = 1;
        public float matchMultiplier = 2f;

        [Header("Cena do bico (ESCOPO 6.7): 96×64, 4 a 8 quadros em loop, ~8 fps")]
        [Tooltip("Quadros com o ladrão trabalhando (sprite 32×32 do tipo dele). Vazio = a vitrine mostra só o ícone.")]
        public Sprite[] sceneFrames = new Sprite[0];
        public float sceneFps = 8f;
        [Tooltip("Cena parada e apagada, com a placa \"volto já\", pra quando não tem ninguém no bico.")]
        public Sprite sceneEmpty;

        public bool HasScene => sceneFrames != null && sceneFrames.Length > 0;

        /// <summary>Quadro da cena depois de tantos segundos (em loop). Sem cena: null.</summary>
        public Sprite SceneFrameAt(float seconds)
        {
            if (!HasScene) return null;
            int index = Mathf.FloorToInt(Mathf.Max(0f, seconds) * Mathf.Max(0.1f, sceneFps)) % sceneFrames.Length;
            return sceneFrames[index];
        }
    }
}
