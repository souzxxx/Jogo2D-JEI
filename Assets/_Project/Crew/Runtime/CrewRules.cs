using System;
using AFirma.Core;

namespace AFirma.Crew
{
    /// <summary>Contas da crew (ESCOPO 6.3). Funções puras: a UI pode usar pra mostrar preços e os testes checam os números direto.</summary>
    public static class CrewRules
    {
        /// <summary>
        /// Recruta: recruitBaseCost × recruitCostGrowthPerLevel^(nível sorteado − 1) × (1 − Fama Reputação%).
        /// Os níveis da Fama Lenda não entram no preço: são bônus.
        /// </summary>
        public static double RecruitCost(int rolledLevel, float reputationPercent, GameConfig cfg) =>
            Math.Round(cfg.recruitBaseCost * Math.Pow(cfg.recruitCostGrowthPerLevel, Math.Max(0, rolledLevel - 1))
                       * (1 - Clamp01(reputationPercent / 100f)));

        /// <summary>Trocar os candidatos na hora: recruitRefreshCost × (1 − desconto × nível de Contatos).</summary>
        public static double PaidRefreshCost(float contactsLevel, GameConfig cfg) =>
            Math.Round(cfg.recruitRefreshCost * Math.Max(0f, 1f - cfg.recruitRefreshDiscountPerContacts * contactsLevel));

        static float Clamp01(float value) => Math.Min(1f, Math.Max(0f, value));
    }
}
