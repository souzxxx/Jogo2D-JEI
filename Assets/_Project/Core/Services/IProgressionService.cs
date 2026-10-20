using System.Collections.Generic;

namespace AFirma.Core
{
    /// <summary>Escada de golpes, modificadores, prestígio e Fama. Dono: Nicolas. Mock: MockProgressionService.</summary>
    public interface IProgressionService
    {
        IReadOnlyList<HeistDef> AllHeists { get; }
        bool IsUnlocked(HeistDef heist);
        bool IsCompleted(HeistDef heist);
        /// <summary>Chamado pelo Motor do Golpe quando o cofre principal é limpo.</summary>
        void MarkCompleted(HeistDef heist);

        HeistModifierDef GetModifier(HeistDef heist);
        /// <summary>Sorteia os modificadores do mapa (a cada visita ao mapa).</summary>
        void RollModifiers();
        HeistContext GetContext(HeistDef heist);

        int WantedLevel { get; }
        int Fame { get; }
        int GetFamePerkLevel(FamePerkDef perk);
        int GetFamePerkCost(FamePerkDef perk);
        bool CanBuyFamePerk(FamePerkDef perk, out string reason);
        bool BuyFamePerk(FamePerkDef perk);
        float GetFamePerkTotal(FamePerkEffect effect);

        bool CanPrestige(out string reason);
        int FameOnPrestige();
        void Prestige();
    }
}
