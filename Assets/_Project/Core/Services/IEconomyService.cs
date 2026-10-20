namespace AFirma.Core
{
    /// <summary>Dinheiro, upgrades do esconderijo, bicos e ganho offline. Dono: Nicolas. Mock: MockEconomyService.</summary>
    public interface IEconomyService
    {
        double Money { get; }
        bool CanAfford(double amount);
        bool TrySpend(double amount, string reason);
        void Add(double amount, string reason);

        // Upgrades
        int GetUpgradeLevel(UpgradeDef upgrade);
        double GetUpgradeCost(UpgradeDef upgrade);
        bool CanBuyUpgrade(UpgradeDef upgrade, out string reason);
        bool BuyUpgrade(UpgradeDef upgrade);
        /// <summary>Soma do efeito de todos os níveis comprados (ex.: SkillPercent = 15 -> +15%).</summary>
        float GetUpgradeTotal(UpgradeEffect effect);

        // Bicos
        double IncomePerSecond { get; }
        double GetSideJobIncome(ThiefState thief, SideJobDef job);
        bool CanAssignSideJob(ThiefState thief, SideJobDef job, out string reason);
        void AssignSideJob(ThiefState thief, SideJobDef job);
        void RemoveFromSideJob(ThiefState thief);

        /// <summary>Aplica o ganho dos bicos do tempo fora. Retorna quanto entrou.</summary>
        double ApplyOfflineEarnings(double secondsAway);
    }
}
