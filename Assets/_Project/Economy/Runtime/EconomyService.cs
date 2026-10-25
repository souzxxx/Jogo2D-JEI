using System;
using AFirma.Core;

namespace AFirma.Economy
{
    /// <summary>
    /// Economia real (Nicolas). Substitui o MockEconomyService.
    ///
    /// Regras próprias: Cr$ (nunca negativo, valor inválido é ignorado) e upgrades do esconderijo (ESCOPO 6.8).
    /// Bicos e ganho offline ainda passam pelo mock até as issues #37 e #38.
    /// </summary>
    public class EconomyService : IEconomyService, ITickable
    {
        readonly MockEconomyService sideJobs = new MockEconomyService(); // #37/#38: trocar pelos bicos e offline reais

        static GameState S => Services.State;
        static GameDatabase Db => Services.Db;

        // ---------------- Cr$ ----------------

        public double Money => S.money;

        public bool CanAfford(double amount) => IsValidAmount(amount) && S.money >= amount;

        public bool TrySpend(double amount, string reason)
        {
            if (!CanAfford(amount)) return false;
            if (amount == 0) return true;
            S.money -= amount;
            GameEvents.RaiseMoneyChanged(S.money, -amount);
            return true;
        }

        public void Add(double amount, string reason)
        {
            if (!IsValidAmount(amount) || amount == 0) return;
            S.money += amount;
            GameEvents.RaiseMoneyChanged(S.money, amount);
        }

        static bool IsValidAmount(double amount) => amount >= 0 && !double.IsInfinity(amount); // NaN falha no >= 0

        // ---------------- Upgrades ----------------

        /// <summary>Nível comprado, preso entre 0 e o máximo do Def (o save pode vir de quando o máximo era outro).</summary>
        public int GetUpgradeLevel(UpgradeDef upgrade)
        {
            if (upgrade == null) return 0;
            return Math.Max(0, Math.Min(upgrade.maxLevel, GameState.GetLevel(S.upgrades, upgrade.id)));
        }

        public double GetUpgradeCost(UpgradeDef upgrade) =>
            upgrade == null ? 0 : Formulas.UpgradeCost(upgrade.baseCost, upgrade.costGrowth, GetUpgradeLevel(upgrade));

        public bool CanBuyUpgrade(UpgradeDef upgrade, out string reason)
        {
            if (upgrade == null) { reason = "Upgrade inexistente."; return false; }
            if (GetUpgradeLevel(upgrade) >= upgrade.maxLevel) { reason = "Nível máximo."; return false; }
            var cost = GetUpgradeCost(upgrade);
            if (!CanAfford(cost)) { reason = "Falta " + Formulas.Money(cost - S.money) + "."; return false; }
            reason = null;
            return true;
        }

        public bool BuyUpgrade(UpgradeDef upgrade)
        {
            if (!CanBuyUpgrade(upgrade, out _)) return false;
            if (!TrySpend(GetUpgradeCost(upgrade), "upgrade " + upgrade.id)) return false;
            var level = GetUpgradeLevel(upgrade) + 1;
            GameState.SetLevel(S.upgrades, upgrade.id, level);
            GameEvents.RaiseUpgradePurchased(upgrade, level);
            return true;
        }

        public float GetUpgradeTotal(UpgradeEffect effect)
        {
            float total = 0f;
            foreach (var upgrade in Db.upgrades)
                if (upgrade != null && upgrade.effect == effect)
                    total += GetUpgradeLevel(upgrade) * upgrade.valuePerLevel;
            return total;
        }

        // ---------------- Bicos e offline (#37/#38) ----------------

        public double IncomePerSecond => sideJobs.IncomePerSecond;

        public double GetSideJobIncome(ThiefState thief, SideJobDef job) => sideJobs.GetSideJobIncome(thief, job);

        public bool CanAssignSideJob(ThiefState thief, SideJobDef job, out string reason) =>
            sideJobs.CanAssignSideJob(thief, job, out reason);

        public void AssignSideJob(ThiefState thief, SideJobDef job) => sideJobs.AssignSideJob(thief, job);

        public void RemoveFromSideJob(ThiefState thief) => sideJobs.RemoveFromSideJob(thief);

        public double ApplyOfflineEarnings(double secondsAway) => sideJobs.ApplyOfflineEarnings(secondsAway);

        public void Tick(float deltaSeconds) => sideJobs.Tick(deltaSeconds);
    }
}
