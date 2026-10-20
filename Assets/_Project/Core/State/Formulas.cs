using System;

namespace AFirma.Core
{
    /// <summary>Fórmulas compartilhadas. Os números vêm da GameConfig/Defs; aqui só a conta.</summary>
    public static class Formulas
    {
        public static float StatAtLevel(float baseValue, float perLevel, int level) =>
            baseValue + perLevel * (Math.Max(1, level) - 1);

        public static double UpgradeCost(double baseCost, float growth, int currentLevel) =>
            Math.Round(baseCost * Math.Pow(growth, currentLevel));

        public static int XpToNextLevel(int level, int factor) => factor * Math.Max(1, level);

        public static int FameForLoot(double cycleLoot, double divisor) =>
            divisor <= 0 ? 0 : (int)Math.Floor(Math.Sqrt(Math.Max(0, cycleLoot) / divisor));

        public static double WantedMultiplier(float perLevel, int wantedLevel) =>
            Math.Pow(perLevel, Math.Max(0, wantedLevel));

        // Formata na mão (ponto como separador de milhar): cultura pt-BR não é garantida no WebGL.
        public static string Money(double value) =>
            "Cr$ " + Math.Round(value).ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", ".");
    }
}
