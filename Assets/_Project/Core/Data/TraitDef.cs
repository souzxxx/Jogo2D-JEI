using UnityEngine;

namespace AFirma.Core
{
    /// <summary>Manha: traço de personalidade aleatório de cada ladrão.</summary>
    [CreateAssetMenu(menuName = "A Firma/Manha (traço)", fileName = "Trait")]
    public class TraitDef : DefBase
    {
        public StatModifiers modifiers = new StatModifiers();
    }
}
