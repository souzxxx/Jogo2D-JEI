using UnityEngine;

namespace AFirma.Core
{
    [CreateAssetMenu(menuName = "A Firma/Modificador de Golpe", fileName = "HeistModifier")]
    public class HeistModifierDef : DefBase
    {
        public float lockDifficultyMult = 1f;
        public float systemDifficultyMult = 1f;
        public float personDifficultyMult = 1f;
        public float lootMult = 1f;
        public float alertMult = 1f;
        [Tooltip("Multiplicador de alerta só nas primeiras salas.")]
        public float firstRoomsAlertMult = 1f;
        public int firstRoomsCount;
        public float policeCountdownDelta;
        public float weight = 1f;
    }
}
