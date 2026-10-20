namespace AFirma.Core
{
    /// <summary>
    /// Versão simples da economia pra ninguém ficar travado. O serviço real (Nicolas) substitui este.
    /// Falta aqui: ganho offline, bônus de Fama na renda.
    /// </summary>
    public class MockEconomyService : IEconomyService, ITickable
    {
        double pendingIncome;

        static GameState S => Services.State;
        static GameDatabase Db => Services.Db;

        public double Money => S.money;

        public bool CanAfford(double amount) => S.money >= amount;

        public bool TrySpend(double amount, string reason)
        {
            if (amount < 0 || !CanAfford(amount)) return false;
            S.money -= amount;
            GameEvents.RaiseMoneyChanged(S.money, -amount);
            return true;
        }

        public void Add(double amount, string reason)
        {
            if (amount <= 0) return;
            S.money += amount;
            GameEvents.RaiseMoneyChanged(S.money, amount);
        }

        public int GetUpgradeLevel(UpgradeDef upgrade) => GameState.GetLevel(S.upgrades, upgrade.id);

        public double GetUpgradeCost(UpgradeDef upgrade) =>
            Formulas.UpgradeCost(upgrade.baseCost, upgrade.costGrowth, GetUpgradeLevel(upgrade));

        public bool CanBuyUpgrade(UpgradeDef upgrade, out string reason)
        {
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

        public double IncomePerSecond
        {
            get
            {
                double total = Services.Config != null ? Services.Config.scavengeIncomePerSecond : 0;
                foreach (var thief in S.crew)
                    if (thief.status == ThiefStatus.OnSideJob)
                        total += GetSideJobIncome(thief, Db.GetSideJob(thief.sideJobId));
                return total;
            }
        }

        public double GetSideJobIncome(ThiefState thief, SideJobDef job)
        {
            if (thief == null || job == null) return 0;
            double income = job.incomePerSecond * (1 + 0.1 * (thief.level - 1));
            if (job.hasPreferredType && job.preferredType == thief.type) income *= job.matchMultiplier;
            return income * (1 + GetUpgradeTotal(UpgradeEffect.SideJobPercent) / 100.0);
        }

        public bool CanAssignSideJob(ThiefState thief, SideJobDef job, out string reason)
        {
            if (job == null || !job.requiresThief) { reason = "Esse bico não usa ladrão."; return false; }
            if (thief.status != ThiefStatus.Available && thief.status != ThiefStatus.OnSideJob)
            {
                reason = thief.firstName + " não está disponível.";
                return false;
            }
            reason = null;
            return true;
        }

        public void AssignSideJob(ThiefState thief, SideJobDef job)
        {
            if (!CanAssignSideJob(thief, job, out _)) return;
            Services.Crew.SetStatus(thief, ThiefStatus.OnSideJob);
            thief.sideJobId = job.id;
            GameEvents.RaiseSideJobsChanged();
        }

        public void RemoveFromSideJob(ThiefState thief)
        {
            if (thief.status != ThiefStatus.OnSideJob) return;
            Services.Crew.SetStatus(thief, ThiefStatus.Available);
            GameEvents.RaiseSideJobsChanged();
        }

        public double ApplyOfflineEarnings(double secondsAway) => 0; // Nicolas: implementar (até 4 h a 50%).

        public void Tick(float deltaSeconds)
        {
            pendingIncome += IncomePerSecond * deltaSeconds;
            if (pendingIncome < 1) return;
            var whole = System.Math.Floor(pendingIncome);
            pendingIncome -= whole;
            Add(whole, "bicos");
        }
    }
}
