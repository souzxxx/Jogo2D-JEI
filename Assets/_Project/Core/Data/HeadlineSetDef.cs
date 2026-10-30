using System;
using System.Collections.Generic;
using UnityEngine;

namespace AFirma.Core
{
    /// <summary>
    /// Manchetes do jornal O Guanabarense. Placeholders: {golpe}, {valor}, {bastos} (o cargo do Bastos no
    /// Nível de Procurado atual) e {fala} (uma fala do Bastos nesse cargo).
    /// </summary>
    [CreateAssetMenu(menuName = "A Firma/Manchetes", fileName = "Headlines")]
    public class HeadlineSetDef : ScriptableObject
    {
        [TextArea(1, 2)] public string[] clean;
        [TextArea(1, 2)] public string[] hasty;
        [TextArea(1, 2)] public string[] forced;
        [TextArea(1, 2)] public string[] perfect;
        [TextArea(1, 2)] public string[] withArrests;
        [TextArea(1, 2)] public string[] interrupted;

        [Tooltip("Manchetes próprias de cada golpe. Entram junto das de clean quando o cofre é limpo sem sirene e sem prisão.")]
        public List<HeistHeadlines> byHeist = new List<HeistHeadlines>();

        [Header("Delegado Bastos")]
        [Tooltip("Um cargo por Nível de Procurado (0 = antes do primeiro prestígio). Passou do fim, fica o último.")]
        public List<BastosRank> bastos = new List<BastosRank>();

        /// <summary>Escolhe a manchete pro resultado, no Nível de Procurado do save atual.</summary>
        public string Pick(HeistResult result, string heistName, System.Random rng) =>
            Pick(result, heistName, rng, Services.State != null ? Services.State.wantedLevel : 0);

        /// <summary>Escolhe uma manchete pro resultado e preenche os placeholders. Prisão tem prioridade.</summary>
        public string Pick(HeistResult result, string heistName, System.Random rng, int wantedLevel)
        {
            var pool = PoolFor(result);
            var name = (heistName ?? "").ToUpperInvariant();
            if (pool.Count == 0) return name;

            var rank = RankFor(wantedLevel);
            var quotes = rank != null ? rank.quotes : null;
            return pool[rng.Next(pool.Count)]
                .Replace("{golpe}", name)
                .Replace("{valor}", Formulas.Money(result.moneyGained))
                .Replace("{bastos}", rank != null && !string.IsNullOrEmpty(rank.title) ? rank.title : "BASTOS")
                .Replace("{fala}", quotes != null && quotes.Length > 0 ? quotes[rng.Next(quotes.Length)] : "");
        }

        /// <summary>
        /// As manchetes possíveis pro resultado: prisão primeiro; senão a da saída (vazio cai no clean).
        /// Cofre limpo sem sirene soma as manchetes próprias do golpe.
        /// </summary>
        public List<string> PoolFor(HeistResult result)
        {
            var pool = new List<string>();
            if (result == null) return pool;
            if (result.arrestedThiefIds.Count > 0 && withArrests != null && withArrests.Length > 0)
            {
                pool.AddRange(withArrests);
                return pool;
            }

            string[] byOutcome;
            switch (result.outcome)
            {
                case HeistOutcome.HastyEscape: byOutcome = hasty; break;
                case HeistOutcome.ForcedEscape: byOutcome = forced; break;
                case HeistOutcome.PerfectEscape: byOutcome = perfect; break;
                case HeistOutcome.Interrupted: byOutcome = interrupted; break;
                default: byOutcome = clean; break;
            }
            if (byOutcome == null || byOutcome.Length == 0) byOutcome = clean;
            if (byOutcome != null) pool.AddRange(byOutcome);

            if (result.completed && result.outcome == HeistOutcome.CleanEscape)
            {
                var own = byHeist.Find(h => h != null && h.heist != null && h.heist.id == result.heistId);
                if (own != null && own.completed != null) pool.AddRange(own.completed);
            }
            return pool;
        }

        public BastosRank RankFor(int wantedLevel)
        {
            if (bastos == null || bastos.Count == 0) return null;
            return bastos[Math.Max(0, Math.Min(wantedLevel, bastos.Count - 1))];
        }
    }

    [Serializable]
    public class HeistHeadlines
    {
        public HeistDef heist;
        [TextArea(1, 2), Tooltip("Cofre limpo sem sirene e sem prisão.")] public string[] completed;
    }

    [Serializable]
    public class BastosRank
    {
        [Tooltip("Como o jornal chama o Bastos nesse nível. Vira {bastos}.")]
        public string title;
        [TextArea(1, 2), Tooltip("Falas dele nesse cargo, sem aspas. Viram {fala}.")]
        public string[] quotes;
    }
}
