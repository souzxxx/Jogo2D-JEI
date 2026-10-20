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

        /// <summary>Escolhe uma manchete pro resultado e preenche {golpe} e {valor}. Prisão tem prioridade.</summary>
        public string Pick(HeistResult result, string heistName, System.Random rng)
        {
            string[] pool;
            if (result.arrestedThiefIds.Count > 0 && withArrests != null && withArrests.Length > 0) pool = withArrests;
            else
                switch (result.outcome)
                {
                    case HeistOutcome.HastyEscape: pool = hasty; break;
                    case HeistOutcome.ForcedEscape: pool = forced; break;
                    case HeistOutcome.PerfectEscape: pool = perfect; break;
                    case HeistOutcome.Interrupted: pool = interrupted; break;
                    default: pool = clean; break;
                }
            if (pool == null || pool.Length == 0) pool = clean;

            var name = (heistName ?? "").ToUpperInvariant();
            if (pool == null || pool.Length == 0) return name;
            return pool[rng.Next(pool.Length)]
                .Replace("{golpe}", name)
                .Replace("{valor}", Formulas.Money(result.moneyGained));
        }
    }
}
