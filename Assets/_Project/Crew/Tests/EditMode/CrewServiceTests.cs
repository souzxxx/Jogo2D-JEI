using System.Collections.Generic;
using AFirma.Core;
using AFirma.Crew;
using NUnit.Framework;

namespace AFirma.Tests
{
    /// <summary>Crew: atributos efetivos, XP/nível, estados e novato grátis (ESCOPO 6.3).</summary>
    public class CrewServiceTests
    {
        CrewService crew;

        [SetUp]
        public void SetUp()
        {
            TestSupport.InitServicesWithMocks();
            crew = new CrewService(new System.Random(1));
            Services.Register((ICrewService)crew);
        }

        [TearDown]
        public void TearDown() => TestSupport.Teardown();

        // ---------------- Atributos efetivos ----------------

        [Test]
        public void EffectiveStats_WithoutTraitOrUpgrades_AreTheBase()
        {
            var thief = Thief(ThiefType.Locksmith);

            Assert.AreEqual(50f, crew.GetSkill(thief), 0.001f);
            Assert.AreEqual(40f, crew.GetStealth(thief), 0.001f);
            Assert.AreEqual(30f, crew.GetNerves(thief), 0.001f);
        }

        [Test]
        public void GetSkill_IsBaseTimesTraitTimesFerramentas()
        {
            GameState.SetLevel(Services.State.upgrades, "ferramentas", 2); // +10%
            var thief = Thief(ThiefType.Locksmith, traitId: "supersticioso"); // Perícia ×0,9

            Assert.AreEqual(49.5f, crew.GetSkill(thief), 0.001f); // 50 × 0,9 × 1,1
        }

        [Test]
        public void GetStealth_IsBaseTimesTraitTimesTenisDeMolinha()
        {
            GameState.SetLevel(Services.State.upgrades, "tenis-de-molinha", 2); // +10%
            var thief = Thief(ThiefType.Locksmith, traitId: "fominha"); // Discrição ×0,85

            Assert.AreEqual(37.4f, crew.GetStealth(thief), 0.001f); // 40 × 0,85 × 1,1
        }

        [Test]
        public void GetNerves_IsBaseTimesTrait_AndNoUpgradeTouchesIt()
        {
            GameState.SetLevel(Services.State.upgrades, "ferramentas", 2);
            GameState.SetLevel(Services.State.upgrades, "tenis-de-molinha", 2);
            var thief = Thief(ThiefType.Locksmith, traitId: "sangue-frio"); // Nervos ×1,3

            Assert.AreEqual(39f, crew.GetNerves(thief), 0.001f);
        }

        // ---------------- XP e nível ----------------

        [Test]
        public void XpToNextLevel_IsFactorTimesLevel_AndZeroAtTheMaxLevel()
        {
            Assert.AreEqual(5, crew.XpToNextLevel(Thief(ThiefType.Locksmith, level: 1)));
            Assert.AreEqual(15, crew.XpToNextLevel(Thief(ThiefType.Locksmith, level: 3)));
            Assert.AreEqual(0, crew.XpToNextLevel(Thief(ThiefType.Locksmith, level: Services.Config.maxLevel)), "No máximo não tem próximo nível.");
        }

        [Test]
        public void AddXp_BelowTheNextLevel_JustAddsXp()
        {
            int leveled = 0;
            GameEvents.ThiefLeveledUp += t => leveled++;
            var thief = Thief(ThiefType.Locksmith);

            Assert.IsFalse(crew.AddXp(thief, 3));

            Assert.AreEqual(1, thief.level);
            Assert.AreEqual(3, thief.xp);
            Assert.AreEqual(0, leveled);
        }

        [Test]
        public void AddXp_LevelsUp_GainsTheTypeStats_AndRaisesThiefLeveledUp()
        {
            var leveled = new List<ThiefState>();
            GameEvents.ThiefLeveledUp += t => leveled.Add(t);
            var thief = Thief(ThiefType.Locksmith);
            var def = Services.Db.GetThiefType(ThiefType.Locksmith);

            Assert.IsTrue(crew.AddXp(thief, 7));

            Assert.AreEqual(2, thief.level);
            Assert.AreEqual(2, thief.xp, "Sobra pro próximo nível.");
            Assert.AreEqual(50f + def.skillPerLevel, thief.skill, 0.001f);
            Assert.AreEqual(40f + def.stealthPerLevel, thief.stealth, 0.001f);
            Assert.AreEqual(30f + def.nervesPerLevel, thief.nerves, 0.001f);
            CollectionAssert.AreEqual(new[] { thief }, leveled);
        }

        [Test]
        public void AddXp_CanJumpSeveralLevels_WithOneEvent()
        {
            int leveled = 0;
            GameEvents.ThiefLeveledUp += t => leveled++;
            var thief = Thief(ThiefType.Locksmith);

            crew.AddXp(thief, 15); // 5 pro nível 2 + 10 pro nível 3

            Assert.AreEqual(3, thief.level);
            Assert.AreEqual(0, thief.xp);
            Assert.AreEqual(1, leveled);
        }

        [Test]
        public void AddXp_StopsAtTheMaxLevel_WithoutHoardingXp()
        {
            int max = Services.Config.maxLevel;
            var thief = Thief(ThiefType.Locksmith, level: max - 1);

            Assert.IsTrue(crew.AddXp(thief, 1000));
            Assert.AreEqual(max, thief.level);
            Assert.AreEqual(0, thief.xp);

            Assert.IsFalse(crew.AddXp(thief, 5));
            Assert.AreEqual(max, thief.level);
            Assert.AreEqual(0, thief.xp);
        }

        // ---------------- Estados ----------------

        [Test]
        public void SetStatus_LeavingASideJob_ClearsIt_AndRaisesCrewChanged()
        {
            int changed = 0;
            GameEvents.CrewChanged += () => changed++;
            var thief = Thief(ThiefType.Locksmith);
            thief.status = ThiefStatus.OnSideJob;
            thief.sideJobId = "chaveiro";

            crew.SetStatus(thief, ThiefStatus.OnHeist);

            Assert.AreEqual(ThiefStatus.OnHeist, thief.status);
            Assert.IsNull(thief.sideJobId, "Mandar pro golpe tira ele do bico.");
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void SetStatus_ToSideJob_KeepsTheJob()
        {
            var thief = Thief(ThiefType.Locksmith);
            thief.sideJobId = "chaveiro";

            crew.SetStatus(thief, ThiefStatus.OnSideJob);

            Assert.AreEqual("chaveiro", thief.sideJobId);
        }

        [Test]
        public void GetAvailable_ReturnsOnlyWhoIsAvailable()
        {
            var free = Thief(ThiefType.Locksmith);
            Thief(ThiefType.Hacker).status = ThiefStatus.Jailed;
            Thief(ThiefType.Driver).status = ThiefStatus.OnHeist;
            Thief(ThiefType.ConArtist).status = ThiefStatus.OnSideJob;

            var available = crew.GetAvailable();

            CollectionAssert.Contains(available, free);
            foreach (var thief in available) Assert.AreEqual(ThiefStatus.Available, thief.status);
            Assert.AreEqual(Services.State.crew.FindAll(t => t.status == ThiefStatus.Available).Count, available.Count);
        }

        [Test]
        public void GrantFreeRookie_AddsALevel1ThiefOfThatType_ReadyToWork()
        {
            int before = Services.State.crew.Count;
            int changed = 0;
            GameEvents.CrewChanged += () => changed++;

            var rookie = crew.GrantFreeRookie(ThiefType.Driver);

            Assert.AreEqual(before + 1, Services.State.crew.Count);
            CollectionAssert.Contains(Services.State.crew, rookie);
            Assert.AreEqual(ThiefType.Driver, rookie.type);
            Assert.AreEqual(1, rookie.level);
            Assert.AreEqual(ThiefStatus.Available, rookie.status);
            Assert.Greater(rookie.skill, 0f);
            Assert.AreEqual(1, changed);
        }

        // ---------------- Vagas ----------------

        [Test]
        public void Slots_StartAt3And2_AndGrowWithQuartosAndVan()
        {
            Assert.AreEqual(3, crew.MaxCrewSize);
            Assert.AreEqual(2, crew.HeistSlots);

            GameState.SetLevel(Services.State.upgrades, "quartos", 2);
            GameState.SetLevel(Services.State.upgrades, "van", 1);

            Assert.AreEqual(5, crew.MaxCrewSize);
            Assert.AreEqual(3, crew.HeistSlots);
        }

        // ---------------- Apoio ----------------

        static ThiefState Thief(ThiefType type, string traitId = null, int level = 1)
        {
            var thief = new ThiefState
            {
                id = "teste-" + Services.State.crew.Count,
                firstName = "Zé",
                nickname = type.ToString(),
                type = type,
                level = level,
                skill = 50f,
                stealth = 40f,
                nerves = 30f,
                traitId = traitId,
                status = ThiefStatus.Available
            };
            Services.State.crew.Add(thief);
            return thief;
        }
    }
}
