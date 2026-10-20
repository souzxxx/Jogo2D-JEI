using System;
using System.Collections.Generic;

namespace AFirma.Core
{
    /// <summary>
    /// Versão simples da progressão. O serviço real (Nicolas) substitui este.
    /// Falta aqui: sorteio de modificadores e prestígio.
    /// </summary>
    public class MockProgressionService : IProgressionService
    {
        static GameState S => Services.State;
        static GameDatabase Db => Services.Db;
        static GameConfig Cfg => Services.Config;

        public IReadOnlyList<HeistDef> AllHeists => Db.heists;

        public bool IsUnlocked(HeistDef heist) =>
            heist != null && (heist.unlockedByDefault || S.unlockedHeists.Contains(heist.id));

        public bool IsCompleted(HeistDef heist) => heist != null && S.completedHeists.Contains(heist.id);

        public void MarkCompleted(HeistDef heist)
        {
            if (heist == null) return;
            if (!S.completedHeists.Contains(heist.id)) S.completedHeists.Add(heist.id);

            foreach (var other in Db.heists)
            {
                if (other == null || other.unlockedBy != heist || IsUnlocked(other)) continue;
                S.unlockedHeists.Add(other.id);
                GameEvents.RaiseHeistUnlocked(other);
            }
        }

        public HeistModifierDef GetModifier(HeistDef heist)
        {
            var roll = S.modifierRolls.Find(r => r.heistId == heist.id);
            return roll != null ? Db.GetModifier(roll.modifierId) : null;
        }

        public void RollModifiers()
        {
            S.modifierRolls.Clear(); // Nicolas: sortear um modificador por golpe.
            GameEvents.RaiseHeistModifiersRolled();
        }

        public HeistContext GetContext(HeistDef heist)
        {
            var ctx = new HeistContext { heist = heist, wantedLevel = S.wantedLevel };
            var mod = GetModifier(heist);
            if (mod != null)
            {
                ctx.modifier = mod;
                ctx.lockDifficultyMult = mod.lockDifficultyMult;
                ctx.systemDifficultyMult = mod.systemDifficultyMult;
                ctx.personDifficultyMult = mod.personDifficultyMult;
                ctx.lootMult = mod.lootMult;
                ctx.alertMult = mod.alertMult;
                ctx.firstRoomsAlertMult = mod.firstRoomsAlertMult;
                ctx.firstRoomsCount = mod.firstRoomsCount;
                ctx.policeCountdownDelta = mod.policeCountdownDelta;
            }

            if (Cfg != null && S.wantedLevel > 0)
            {
                var difficulty = (float)Formulas.WantedMultiplier(Cfg.wantedDifficultyMult, S.wantedLevel);
                ctx.lockDifficultyMult *= difficulty;
                ctx.systemDifficultyMult *= difficulty;
                ctx.personDifficultyMult *= difficulty;
                ctx.lootMult *= (float)Formulas.WantedMultiplier(Cfg.wantedLootMult, S.wantedLevel);
                ctx.alertMult *= (float)Formulas.WantedMultiplier(Cfg.wantedAlertMult, S.wantedLevel);
            }
            return ctx;
        }

        public int WantedLevel => S.wantedLevel;
        public int Fame => S.fame;

        public int GetFamePerkLevel(FamePerkDef perk) => GameState.GetLevel(S.famePerks, perk.id);

        public int GetFamePerkCost(FamePerkDef perk) =>
            (int)Math.Ceiling(perk.baseCost * Math.Pow(perk.costGrowth, GetFamePerkLevel(perk)));

        public bool CanBuyFamePerk(FamePerkDef perk, out string reason)
        {
            if (GetFamePerkLevel(perk) >= perk.maxLevel) { reason = "Nível máximo."; return false; }
            if (S.fame < GetFamePerkCost(perk)) { reason = "Fama insuficiente."; return false; }
            reason = null;
            return true;
        }

        public bool BuyFamePerk(FamePerkDef perk)
        {
            if (!CanBuyFamePerk(perk, out _)) return false;
            S.fame -= GetFamePerkCost(perk);
            var level = GetFamePerkLevel(perk) + 1;
            GameState.SetLevel(S.famePerks, perk.id, level);
            GameEvents.RaiseFamePerkPurchased(perk, level);
            return true;
        }

        public float GetFamePerkTotal(FamePerkEffect effect)
        {
            float total = 0f;
            foreach (var perk in Db.famePerks)
                if (perk != null && perk.effect == effect)
                    total += GetFamePerkLevel(perk) * perk.valuePerLevel;
            return total;
        }

        public bool CanPrestige(out string reason)
        {
            reason = "Prestígio ainda não implementado (mock).";
            return false;
        }

        public int FameOnPrestige() => Cfg != null ? Formulas.FameForLoot(S.cycleLoot, Cfg.fameDivisor) : 0;

        public void Prestige() { } // Nicolas: implementar "Sumir do mapa".
    }
}
