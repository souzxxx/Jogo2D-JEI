using System;
using System.Collections.Generic;
using AFirma.Core;

namespace AFirma.Heist
{
    /// <summary>Regras dos eventos do golpe (ESCOPO 6.4): chance, filtros, sorteio por peso e o cartão de escolhas.</summary>
    public static class EventRules
    {
        /// <summary>Chance de evento ao entrar numa sala: eventChancePerRoom × manhas (Pé-frio ×1,5 cada), no máximo 100%.</summary>
        public static float EventChance(IReadOnlyList<ThiefState> crew, GameDatabase db)
        {
            float chance = db.config.eventChancePerRoom;
            foreach (var thief in crew)
            {
                var trait = db.GetTrait(thief.traitId);
                if (trait != null) chance *= trait.modifiers.badEventChanceMult;
            }
            return Math.Min(1f, Math.Max(0f, chance));
        }

        /// <summary>Pode sair no sorteio desta sala? O tutorial nunca entra (ele é chamado direto na 1ª Lotérica).</summary>
        public static bool CanAppear(HeistEventDef e, HeistDef heist, RoomData room) =>
            e != null && !e.tutorialOnly && e.minTier <= heist.tier && (e.anyRoom || e.roomObstacle == room.obstacle);

        /// <summary>Sorteio pelo peso. roll01 vem do rng, em [0, 1).</summary>
        public static HeistEventDef PickWeighted(IReadOnlyList<HeistEventDef> pool, double roll01)
        {
            if (pool.Count == 0) return null;
            double total = 0;
            foreach (var e in pool) total += Weight(e);
            double roll = roll01 * total;
            foreach (var e in pool)
            {
                roll -= Weight(e);
                if (roll < 0) return e;
            }
            return pool[pool.Count - 1];
        }

        static double Weight(HeistEventDef e) => Math.Max(0.01f, e.weight);

        /// <summary>
        /// Monta o cartão: escolha que pede um tipo que a crew não tem fica indisponível com motivo;
        /// chance = baseChance + chancePerStatPoint × melhor atributo da crew, no máximo 100%.
        /// </summary>
        public static HeistEventInstance BuildInstance(HeistEventDef def, IReadOnlyList<ThiefState> crew, ICrewService crewService, GameDatabase db)
        {
            var instance = new HeistEventInstance { def = def };
            foreach (var choice in def.choices)
            {
                var view = new EventChoiceView { choice = choice, available = true };
                if (choice.requiresType && !HasType(crew, choice.requiredType))
                {
                    view.available = false;
                    var typeDef = db.GetThiefType(choice.requiredType);
                    view.unavailableReason = "Precisa de um " + (typeDef != null ? typeDef.displayName : choice.requiredType.ToString()) + ".";
                }
                view.successChance = Math.Min(1f, Math.Max(0f, choice.baseChance + choice.chancePerStatPoint * BestStat(choice.check, crew, crewService)));
                instance.choices.Add(view);
            }
            return instance;
        }

        /// <summary>Tem pelo menos uma escolha que essa crew pode fazer? Sem nenhuma, o evento não abre (senão o golpe trava).</summary>
        public static bool HasAvailableChoice(HeistEventInstance instance) => instance.choices.Exists(c => c.available);

        static bool HasType(IReadOnlyList<ThiefState> crew, ThiefType type)
        {
            foreach (var thief in crew)
                if (thief.type == type) return true;
            return false;
        }

        static float BestStat(CheckStat stat, IReadOnlyList<ThiefState> crew, ICrewService crewService)
        {
            float best = 0f;
            foreach (var thief in crew)
            {
                float value = stat == CheckStat.Skill ? crewService.GetSkill(thief)
                    : stat == CheckStat.Stealth ? crewService.GetStealth(thief)
                    : stat == CheckStat.Nerves ? crewService.GetNerves(thief) : 0f;
                best = Math.Max(best, value);
            }
            return best;
        }
    }
}
