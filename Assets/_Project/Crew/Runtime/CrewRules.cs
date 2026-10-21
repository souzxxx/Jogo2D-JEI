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

        /// <summary>O Advogado nunca zera a pena (com o máximo de hoje, −50%, isso nem chega a valer).</summary>
        public const float MinLawyerFactor = 0.1f;

        public static float LawyerFactor(float lawyerPercent) => Math.Max(MinLawyerFactor, 1f - lawyerPercent / 100f);

        /// <summary>Segundos de cadeia: tempo do golpe × (1 − Advogado%), arredondado pra cima.</summary>
        public static long JailSeconds(float seconds, float lawyerPercent) => CeilClean(seconds * (double)LawyerFactor(lawyerPercent));

        /// <summary>Fiança: segundos restantes × bailCostPerJailSecond × manha (Pão-duro) × (1 − Advogado%), arredondada pra cima.</summary>
        public static double BailCost(float secondsLeft, float bailMult, float lawyerPercent, GameConfig cfg) =>
            CeilClean(secondsLeft * cfg.bailCostPerJailSecond * bailMult * LawyerFactor(lawyerPercent));

        // Arredonda pra cima sem deixar o ruído do float (54,0000014) virar um a mais.
        static long CeilClean(double value) => (long)Math.Ceiling(Math.Round(value, 3));

        static float Clamp01(float value) => Math.Min(1f, Math.Max(0f, value));
    }
}
