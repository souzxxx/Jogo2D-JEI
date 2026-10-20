using UnityEngine;

namespace AFirma.Core
{
    [CreateAssetMenu(menuName = "A Firma/Bico", fileName = "SideJob")]
    public class SideJobDef : DefBase
    {
        [Tooltip("Falso = renda fixa sem ladrão (Catar latinha).")]
        public bool requiresThief = true;
        public bool hasPreferredType = true;
        public ThiefType preferredType;
        public double incomePerSecond = 1;
        public float matchMultiplier = 2f;
    }
}
