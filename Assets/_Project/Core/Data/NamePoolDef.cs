using UnityEngine;

namespace AFirma.Core
{
    [CreateAssetMenu(menuName = "A Firma/Lista de Nomes", fileName = "NamePool")]
    public class NamePoolDef : ScriptableObject
    {
        public string[] firstNames = { "Juninho", "Tião" };
    }
}
