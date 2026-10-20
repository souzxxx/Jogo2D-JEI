using UnityEngine;

namespace AFirma.Core
{
    /// <summary>Manchetes do jornal O Guanabarense. Placeholders: {golpe} e {valor}.</summary>
    [CreateAssetMenu(menuName = "A Firma/Manchetes", fileName = "Headlines")]
    public class HeadlineSetDef : ScriptableObject
    {
        [TextArea(1, 2)] public string[] clean;
        [TextArea(1, 2)] public string[] hasty;
        [TextArea(1, 2)] public string[] forced;
        [TextArea(1, 2)] public string[] perfect;
        [TextArea(1, 2)] public string[] withArrests;
        [TextArea(1, 2)] public string[] interrupted;
    }
}
