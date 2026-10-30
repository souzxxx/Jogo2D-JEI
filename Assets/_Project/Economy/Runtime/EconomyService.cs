using System;
using AFirma.Core;

namespace AFirma.Economy
{
    /// <summary>
    /// Economia real (Nicolas). Substitui o MockEconomyService.
    ///
    /// Regras próprias: Cr$ (nunca negativo, valor inválido é ignorado), upgrades do esconderijo (ESCOPO 6.8),
    /// bicos com o Catar latinha sempre rendendo e o ganho offline ao abrir o jogo (ESCOPO 6.7).
    /// </summary>
    public class EconomyService : IEconomyService, ITickable
    {
        double pendingIncome; // fração de Cr$ dos bicos que ainda não virou dinheiro inteiro

        static GameState S => Services.State;
        static GameDatabase Db => Services.Db;
        static GameConfig Cfg => Services.Config;

        public EconomyService()
        {
            GameEvents.GameLoaded += OnGameLoaded;
        }

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

        // ---------------- Bicos (ESCOPO 6.7) ----------------

        /// <summary>Catar latinha (sempre rolando, sem ladrão) mais todo mundo que está no bico.</summary>
        public double IncomePerSecond
        {
            get
            {
                double total = Cfg != null ? Math.Max(0, Cfg.scavengeIncomePerSecond) : 0;
                foreach (var thief in S.crew)
                    if (thief.status == ThiefStatus.OnSideJob)
                        total += GetSideJobIncome(thief, Db.GetSideJob(thief.sideJobId));
                return total;
            }
        }

        /// <summary>renda × (1 + bônus por nível acima do 1) × (multiplicador se é o tipo certo) × (1 + Ponto de bico%) × (1 + Fama Rede%).</summary>
        public double GetSideJobIncome(ThiefState thief, SideJobDef job)
        {
            if (thief == null || job == null || !job.requiresThief) return 0;
            float levelBonus = Cfg != null ? Cfg.sideJobLevelBonus : 0f;
            double income = job.incomePerSecond * (1 + levelBonus * (Math.Max(1, thief.level) - 1));
            if (job.hasPreferredType && job.preferredType == thief.type) income *= job.matchMultiplier;
            income *= 1 + GetUpgradeTotal(UpgradeEffect.SideJobPercent) / 100.0;
            income *= 1 + FamePerkTotal(FamePerkEffect.SideJobPercent) / 100.0;
            return Math.Max(0, income);
        }

        public bool CanAssignSideJob(ThiefState thief, SideJobDef job, out string reason)
        {
            if (job == null || !job.requiresThief) { reason = "Esse bico não usa ladrão."; return false; }
            if (thief == null || !S.crew.Contains(thief)) { reason = "Esse ladrão não é da crew."; return false; }
            switch (thief.status)
            {
                case ThiefStatus.OnHeist: reason = thief.firstName + " está num golpe."; return false;
                case ThiefStatus.Jailed: reason = thief.firstName + " está em cana."; return false;
                case ThiefStatus.OnSideJob when thief.sideJobId == job.id: reason = thief.firstName + " já está nesse bico."; return false;
            }
            reason = null;
            return true;
        }

        /// <summary>Manda pro bico (ou troca de bico). Ir pro golpe tira do bico: o Crew limpa o sideJobId em qualquer outro status.</summary>
        public void AssignSideJob(ThiefState thief, SideJobDef job)
        {
            if (!CanAssignSideJob(thief, job, out _)) return;
            thief.sideJobId = job.id; // antes do SetStatus, pra quem ouve CrewChanged já ver o bico novo
            Services.Crew.SetStatus(thief, ThiefStatus.OnSideJob);
            GameEvents.RaiseSideJobsChanged();
        }

        public void RemoveFromSideJob(ThiefState thief)
        {
            if (thief == null || thief.status != ThiefStatus.OnSideJob) return;
            Services.Crew.SetStatus(thief, ThiefStatus.Available);
            GameEvents.RaiseSideJobsChanged();
        }

        /// <summary>Paga os bicos em Cr$ inteiros; a fração fica guardada pro próximo frame.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0) return;
            pendingIncome += IncomePerSecond * deltaSeconds;
            if (pendingIncome < 1) return;
            var whole = Math.Floor(pendingIncome);
            pendingIncome -= whole;
            Add(whole, "bicos");
        }

        static float FamePerkTotal(FamePerkEffect effect) =>
            Services.Progression != null ? Services.Progression.GetFamePerkTotal(effect) : 0f;

        // ---------------- Offline (ESCOPO 6.7) ----------------

        /// <summary>Ao abrir o jogo: paga os bicos do tempo desde o último save.</summary>
        void OnGameLoaded()
        {
            if (S == null) return;
            ApplyOfflineEarnings(GameClock.NowUnix - S.lastSaveUnix);
        }

        /// <summary>
        /// Bicos do tempo fora, até offlineCapHours e a offlineRate da renda, em Cr$ inteiros.
        /// Pouco tempo fora (recarregar a página) ou relógio que voltou no tempo não paga nada.
        /// </summary>
        public double ApplyOfflineEarnings(double secondsAway)
        {
            if (Cfg == null || double.IsNaN(secondsAway) || secondsAway < Math.Max(0, Cfg.offlineMinSeconds)) return 0;
            double seconds = Math.Min(secondsAway, Math.Max(0, Cfg.offlineCapHours) * 3600.0);
            double amount = Math.Floor(IncomePerSecond * seconds * Cfg.offlineRate);
            if (amount <= 0) return 0;
            Add(amount, "offline");
            GameEvents.RaiseOfflineEarnings(amount, secondsAway);
            return amount;
        }
    }
}
