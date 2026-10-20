using UnityEngine;

namespace AFirma.Core
{
    [CreateAssetMenu(menuName = "A Firma/Upgrade do Esconderijo", fileName = "Upgrade")]
    public class UpgradeDef : DefBase
    {
        public UpgradeEffect effect;
        public int maxLevel = 5;
        public double baseCost = 100;
        public float costGrowth = 1.6f;
        [Tooltip("Valor do efeito por nível (ex.: 1 vaga, 5%, 2 segundos).")]
        public float valuePerLevel = 1f;
    }
}
