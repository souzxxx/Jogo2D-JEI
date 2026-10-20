using UnityEngine;

namespace AFirma.Core
{
    /// <summary>Base de todas as definições de dados (ScriptableObjects). O id é o que vai pro save.</summary>
    public abstract class DefBase : ScriptableObject
    {
        [Tooltip("Identificador único e estável. Vai pro save: não mude depois de publicado.")]
        public string id;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public Sprite icon;
    }
}
