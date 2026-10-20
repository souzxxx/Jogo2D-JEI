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
    }
}
