using System;
using System.Collections.Generic;
using AFirma.Core;

namespace AFirma.Progression
{
    /// <summary>
    /// Progressão real (Nicolas). Substitui o MockProgressionService.
    ///
    /// Regras próprias: escada de golpes (ESCOPO 6.9), o sorteio dos modificadores no mapa e o contexto de cada golpe
    /// (modificador sorteado + Nível de Procurado).
    /// A Fama não entra no contexto: o Motor do Golpe já aplica os bônus de Fama direto.
    /// Fama e prestígio ainda passam pelo mock até as issues #40 e #41.
    /// </summary>
    public class ProgressionService : IProgressionService
    {
        readonly MockProgressionService pending = new MockProgressionService(); // #40/#41: trocar pelos reais
        readonly Random rng;
        bool modifiersStale = true; // só sorteia de novo depois de um golpe ou ao abrir o jogo

        static GameState S => Services.State;
        static GameDatabase Db => Services.Db;
        static GameConfig Cfg => Services.Config;

        public ProgressionService(Random rng = null)
        {
            this.rng = rng ?? new Random();
            GameEvents.HeistFinished += _ => modifiersStale = true;
            GameEvents.NewGameStarted += () => modifiersStale = true;
        }

        // ---------------- Escada de golpes ----------------

        public IReadOnlyList<HeistDef> AllHeists => Db.heists;

        /// <summary>
        /// Aberto se é de começo (Lotérica), se já foi liberado ou se o golpe anterior da escada já foi concluído.
        /// O último caso conserta save em que o golpe foi concluído mas a liberação não foi gravada.
        /// </summary>
        public bool IsUnlocked(HeistDef heist)
        {
            if (heist == null) return false;
            if (heist.unlockedByDefault || S.unlockedHeists.Contains(heist.id)) return true;
            return heist.unlockedBy != null && IsCompleted(heist.unlockedBy);
        }

        public bool IsCompleted(HeistDef heist) => heist != null && S.completedHeists.Contains(heist.id);

        public void MarkCompleted(HeistDef heist)
        {
            if (heist == null) return;
            if (!S.completedHeists.Contains(heist.id)) S.completedHeists.Add(heist.id);

            foreach (var other in Db.heists)
            {
                if (other == null || other.unlockedBy == null || other.unlockedBy.id != heist.id) continue;
                if (other.unlockedByDefault || S.unlockedHeists.Contains(other.id)) continue;
                S.unlockedHeists.Add(other.id);
                GameEvents.RaiseHeistUnlocked(other);
            }
        }

        // ---------------- Contexto do golpe ----------------

        public HeistModifierDef GetModifier(HeistDef heist)
        {
            if (heist == null) return null;
            var roll = S.modifierRolls.Find(r => r.heistId == heist.id);
            return roll != null ? Db.GetModifier(roll.modifierId) : null;
        }

        /// <summary>
        /// Visita ao mapa: cada golpe liberado tem modifierChance de sortear um modificador, escolhido pelo weight.
        /// Abrir e fechar o mapa não troca o sorteio: ele só muda depois de um golpe (ou ao abrir o jogo).
        /// A Lotérica (tier 1) não sorteia antes do tutorial acabar.
        /// </summary>
        public void RollModifiers()
        {
            if (!modifiersStale) return;
            modifiersStale = false;
            S.modifierRolls.Clear();
            float chance = Cfg != null ? Cfg.modifierChance : 0f;
            foreach (var heist in Db.heists)
            {
                if (heist == null || !IsUnlocked(heist)) continue;
                if (heist.tier == 1 && !S.tutorialDone) continue;
                if (rng.NextDouble() >= chance) continue;
                var mod = PickModifier();
                if (mod != null) S.modifierRolls.Add(new HeistModifierRoll { heistId = heist.id, modifierId = mod.id });
            }
            GameEvents.RaiseHeistModifiersRolled();
        }

        HeistModifierDef PickModifier()
        {
            double total = 0;
            foreach (var mod in Db.modifiers)
                if (mod != null && mod.weight > 0) total += mod.weight;
            if (total <= 0) return null;

            double roll = rng.NextDouble() * total;
            HeistModifierDef last = null;
            foreach (var mod in Db.modifiers)
            {
                if (mod == null || mod.weight <= 0) continue;
                last = mod;
                roll -= mod.weight;
                if (roll < 0) return mod;
            }
            return last; // arredondamento do double
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

        // ---------------- Fama e prestígio (#40/#41) ----------------

        public int WantedLevel => S.wantedLevel;
        public int Fame => S.fame;

        public int GetFamePerkLevel(FamePerkDef perk) => pending.GetFamePerkLevel(perk);
        public int GetFamePerkCost(FamePerkDef perk) => pending.GetFamePerkCost(perk);
        public bool CanBuyFamePerk(FamePerkDef perk, out string reason) => pending.CanBuyFamePerk(perk, out reason);
        public bool BuyFamePerk(FamePerkDef perk) => pending.BuyFamePerk(perk);
        public float GetFamePerkTotal(FamePerkEffect effect) => pending.GetFamePerkTotal(effect);

        public bool CanPrestige(out string reason) => pending.CanPrestige(out reason);
        public int FameOnPrestige() => pending.FameOnPrestige();
        public void Prestige() => pending.Prestige();
    }
}
