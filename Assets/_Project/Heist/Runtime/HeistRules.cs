using System;
using System.Collections.Generic;
using AFirma.Core;

namespace AFirma.Heist
{
    /// <summary>
    /// Regras da sala (ESCOPO 6.4): quem é especialista, poder da crew, tempo pra limpar e loot.
    /// Funções puras: a HUD pode usar pra mostrar estimativas e os testes checam os números direto.
    /// </summary>
    public static class HeistRules
    {
        /// <summary>Nenhuma sala termina mais rápido que isso, nem com a crew muito forte.</summary>
        public const float MinRoomSeconds = 0.5f;

        public static bool IsSpecialist(ThiefState thief, ObstacleType obstacle, GameDatabase db)
        {
            var def = db.GetThiefType(thief.type);
            return def != null && def.hasSpecialty && def.specialty == obstacle;
        }

        public static bool HasSpecialist(ObstacleType obstacle, IReadOnlyList<ThiefState> crew, GameDatabase db)
        {
            foreach (var thief in crew)
                if (IsSpecialist(thief, obstacle, db)) return true;
            return false;
        }

        /// <summary>
        /// Perícia efetiva do melhor especialista do obstáculo.
        /// Sem especialista: noSpecialistPowerFactor × a maior Perícia da crew.
        /// </summary>
        public static float RoomPower(ObstacleType obstacle, IReadOnlyList<ThiefState> crew, ICrewService crewService, GameDatabase db)
        {
            bool hasSpecialist = false;
            float bestSpecialist = 0f, best = 0f;
            foreach (var thief in crew)
            {
                float skill = crewService.GetSkill(thief);
                best = Math.Max(best, skill);
                if (IsSpecialist(thief, obstacle, db))
                {
                    hasSpecialist = true;
                    bestSpecialist = Math.Max(bestSpecialist, skill);
                }
            }
            return hasSpecialist ? bestSpecialist : db.config.noSpecialistPowerFactor * best;
        }

        /// <summary>Tempo ≈ dificuldade × multiplicador do contexto ÷ poder.</summary>
        public static float RoomSeconds(RoomData room, HeistContext ctx, float power) =>
            Math.Max(MinRoomSeconds, room.difficulty * ctx.DifficultyMult(room.obstacle) / Math.Max(1f, power));

        /// <summary>Loot da sala × contexto × manhas da crew (Fominha em tudo, Mão-boba só em sala de Pessoa).</summary>
        public static double RoomLoot(RoomData room, HeistContext ctx, IReadOnlyList<ThiefState> crew, GameDatabase db)
        {
            double mult = ctx.lootMult;
            foreach (var thief in crew)
            {
                var trait = db.GetTrait(thief.traitId);
                if (trait == null) continue;
                mult *= trait.modifiers.lootMult;
                if (room.obstacle == ObstacleType.Person) mult *= trait.modifiers.personRoomLootMult;
            }
            return Math.Round(room.loot * mult);
        }

        /// <summary>
        /// Alerta por segundo enquanto a crew trabalha:
        /// taxa da sala × (1 − stealthAlertFactor × Discrição média/100) × (noSpecialistAlertMult sem especialista)
        /// × contexto × (firstRoomsAlertMult nas primeiras salas) × manhas × (1 − Fama Sangue-frio%).
        /// </summary>
        public static float AlertPerSecond(RoomData room, int roomIndex, HeistContext ctx, IReadOnlyList<ThiefState> crew,
                                           ICrewService crewService, GameDatabase db, float fameAlertReductionPercent)
        {
            var cfg = db.config;
            float stealth = 0f;
            foreach (var thief in crew) stealth += crewService.GetStealth(thief);
            if (crew.Count > 0) stealth /= crew.Count;

            float rate = room.alertPerSecond * (1f - cfg.stealthAlertFactor * Clamp01(stealth / 100f));
            if (!HasSpecialist(room.obstacle, crew, db)) rate *= cfg.noSpecialistAlertMult;
            rate *= ctx.alertMult;
            if (roomIndex < ctx.firstRoomsCount) rate *= ctx.firstRoomsAlertMult;
            foreach (var thief in crew)
            {
                var trait = db.GetTrait(thief.traitId);
                if (trait != null) rate *= trait.modifiers.alertMult;
            }
            rate *= 1f - Clamp01(fameAlertReductionPercent / 100f);
            return Math.Max(0f, rate);
        }

        /// <summary>Segundos até a polícia chegar: base + modificador + Rádio da polícia + Pé-na-Tábua, com um mínimo.</summary>
        public static float PoliceCountdown(HeistContext ctx, IReadOnlyList<ThiefState> crew, GameDatabase db, float radioSeconds)
        {
            var cfg = db.config;
            float seconds = cfg.policeCountdownSeconds + ctx.policeCountdownDelta + radioSeconds + DriverBonus(crew, db, d => d.policeCountdownBonus);
            return Math.Max(cfg.minPoliceCountdownSeconds, seconds);
        }

        /// <summary>Fração da sacola que chega no caixa em cada saída. O Pé-na-Tábua salva um pouco nas fugas com sirene.</summary>
        public static float LootKeep(HeistOutcome outcome, IReadOnlyList<ThiefState> crew, GameDatabase db)
        {
            var cfg = db.config;
            float bonus = DriverBonus(crew, db, d => d.escapeLootBonus);
            switch (outcome)
            {
                case HeistOutcome.HastyEscape: return Math.Min(1f, cfg.hastyEscapeLootKeep + bonus);
                case HeistOutcome.ForcedEscape: return Math.Min(1f, cfg.forcedEscapeLootKeep + bonus);
                default: return 1f;
            }
        }

        /// <summary>
        /// Chance de um ladrão ser preso na fuga forçada:
        /// (baseArrestChance − redução do Pé-na-Tábua) × (1 − stealthArrestFactor × Discrição dele/100).
        /// </summary>
        public static float ArrestChance(ThiefState thief, IReadOnlyList<ThiefState> crew, ICrewService crewService, GameDatabase db)
        {
            var cfg = db.config;
            float chance = Math.Max(0f, cfg.baseArrestChance - DriverBonus(crew, db, d => d.arrestChanceReduction));
            return chance * (1f - cfg.stealthArrestFactor * Clamp01(crewService.GetStealth(thief) / 100f));
        }

        /// <summary>O que entra no caixa: sacola × saída × (1 + Lavanderia%) × (1 + Fama Esquema%).</summary>
        public static double Payment(double bag, float keep, float laundryPercent, float esquemaPercent) =>
            Math.Round(bag * keep * (1 + laundryPercent / 100.0) * (1 + esquemaPercent / 100.0));

        /// <summary>XP por sala limpa, com a manha (Cria da rua ×1,3). Pode dar fração: o runner vai somando sala a sala.</summary>
        public static float XpPerRoom(ThiefState thief, GameDatabase db)
        {
            var trait = db.GetTrait(thief.traitId);
            return db.config.xpPerRoom * (trait != null ? trait.modifiers.xpMult : 1f);
        }

        /// <summary>Maior passiva de fuga da crew. Dois motoristas não somam.</summary>
        public static float DriverBonus(IReadOnlyList<ThiefState> crew, GameDatabase db, Func<ThiefTypeDef, float> pick)
        {
            float best = 0f;
            foreach (var thief in crew)
            {
                var def = db.GetThiefType(thief.type);
                if (def != null) best = Math.Max(best, pick(def));
            }
            return best;
        }

        static float Clamp01(float value) => Math.Min(1f, Math.Max(0f, value));
    }
}
