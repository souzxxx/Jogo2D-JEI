using UnityEngine;

namespace AFirma.Core
{
    [CreateAssetMenu(menuName = "A Firma/Bônus de Fama", fileName = "FamePerk")]
    public class FamePerkDef : DefBase
    {
        public FamePerkEffect effect;
        public int maxLevel = 5;
        public int baseCost = 1;
        public float costGrowth = 1.5f;
        public float valuePerLevel = 5f;
    }
}
