using System;

namespace AFirma.Core
{
    /// <summary>Cria ladrões novos (crew inicial, recrutas, novato da Dona Cida).</summary>
    public static class ThiefFactory
    {
        const float StatVariance = 0.15f;

        public static ThiefState Create(GameDatabase db, ThiefType type, int level, Random rng, bool allowTrait = true)
        {
            var def = db.GetThiefType(type);
            var thief = new ThiefState
            {
                id = Guid.NewGuid().ToString("N"),
                type = type,
                level = Math.Max(1, level),
                nickname = def != null ? def.displayName : type.ToString(),
                firstName = PickName(db, rng),
                status = ThiefStatus.Available
            };

            if (def != null)
            {
                thief.skill = Vary(Formulas.StatAtLevel(def.baseSkill, def.skillPerLevel, thief.level), rng);
                thief.stealth = Vary(Formulas.StatAtLevel(def.baseStealth, def.stealthPerLevel, thief.level), rng);
                thief.nerves = Vary(Formulas.StatAtLevel(def.baseNerves, def.nervesPerLevel, thief.level), rng);
            }

            if (allowTrait && db.traits.Count > 0)
            {
                var trait = db.traits[rng.Next(db.traits.Count)];
                thief.traitId = trait != null ? trait.id : null;
            }

            return thief;
        }

        static string PickName(GameDatabase db, Random rng)
        {
            var names = db.names != null ? db.names.firstNames : null;
            if (names == null || names.Length == 0) return "Fulano";
            return names[rng.Next(names.Length)];
        }

        static float Vary(float value, Random rng)
        {
            var factor = 1f + ((float)rng.NextDouble() * 2f - 1f) * StatVariance;
            return (float)Math.Round(value * factor, 1);
        }
    }
}
