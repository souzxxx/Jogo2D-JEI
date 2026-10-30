using System.Collections.Generic;
using AFirma.Core;
using AFirma.Progression;
using NUnit.Framework;
using UnityEngine;

namespace AFirma.Tests
{
    /// <summary>Progressão: escada de golpes, modificadores e contexto do golpe (ESCOPO 6.9), Fama e prestígio (ESCOPO 6.10). Dados de teste, pra não depender do conteúdo.</summary>
    public class ProgressionServiceTests
    {
        ProgressionService progression;
        GameDatabase db;
        HeistDef loterica, joalheria, cambio;
        readonly List<HeistDef> unlockEvents = new List<HeistDef>();
        readonly FixedRandom rng = new FixedRandom();
        int rolledEvents;
        readonly List<(FamePerkDef perk, int level)> perkEvents = new List<(FamePerkDef, int)>();
        readonly List<(int fame, int wanted)> prestigeEvents = new List<(int, int)>();
        MemorySaveService save;

        [SetUp]
        public void SetUp()
        {
            save = TestSupport.InitServicesWithMocks();
            db = ScriptableObject.CreateInstance<GameDatabase>();
            db.config = ScriptableObject.CreateInstance<GameConfig>();
            Services.UseDatabase(db);
            Services.State.unlockedHeists.Clear();
            Services.State.completedHeists.Clear();
            Services.State.modifierRolls.Clear();
            Services.State.wantedLevel = 0;
            Services.State.tutorialDone = true;
            Services.State.fame = 0;
            Services.State.famePerks.Clear();
            Services.State.prestigeCount = 0;
            Services.State.cycleLoot = 0;

            loterica = Heist("loterica", unlockedByDefault: true, tier: 1);
            joalheria = Heist("joalheria", unlockedBy: loterica, tier: 2);
            cambio = Heist("cambio", unlockedBy: joalheria, tier: 3);

            rng.Values.Clear();
            progression = new ProgressionService(rng);
            Services.Register((IProgressionService)progression);

            unlockEvents.Clear();
            rolledEvents = 0;
            perkEvents.Clear();
            prestigeEvents.Clear();
            GameEvents.Prestiged += (fame, wanted) => prestigeEvents.Add((fame, wanted));
            GameEvents.FamePerkPurchased += (perk, level) => perkEvents.Add((perk, level));
            GameEvents.HeistUnlocked += heist => unlockEvents.Add(heist);
            GameEvents.HeistModifiersRolled += () => rolledEvents++;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var heist in db.heists) Object.DestroyImmediate(heist);
            foreach (var mod in db.modifiers) Object.DestroyImmediate(mod);
            foreach (var perk in db.famePerks)
                if (perk != null) Object.DestroyImmediate(perk);
            Object.DestroyImmediate(db.config);
            Object.DestroyImmediate(db);
            TestSupport.Teardown();
        }

        HeistDef Heist(string id, bool unlockedByDefault = false, HeistDef unlockedBy = null, int tier = 1)
        {
            var heist = ScriptableObject.CreateInstance<HeistDef>();
            heist.id = id;
            heist.tier = tier;
            heist.unlockedByDefault = unlockedByDefault;
            heist.unlockedBy = unlockedBy;
            db.heists.Add(heist);
            return heist;
        }

        HeistModifierDef Modifier(string id, float weight = 1f)
        {
            var mod = ScriptableObject.CreateInstance<HeistModifierDef>();
            mod.id = id;
            mod.weight = weight;
            db.modifiers.Add(mod);
            return mod;
        }

        FamePerkDef Perk(string id, FamePerkEffect effect, int baseCost = 1, float growth = 1.5f, int maxLevel = 3, float valuePerLevel = 10f)
        {
            var perk = ScriptableObject.CreateInstance<FamePerkDef>();
            perk.id = id;
            perk.effect = effect;
            perk.baseCost = baseCost;
            perk.costGrowth = growth;
            perk.maxLevel = maxLevel;
            perk.valuePerLevel = valuePerLevel;
            db.famePerks.Add(perk);
            return perk;
        }

        /// <summary>Random com os sorteios na mão. Sem valor na fila, devolve 0.</summary>
        class FixedRandom : System.Random
        {
            public readonly Queue<double> Values = new Queue<double>();
            public override double NextDouble() => Values.Count > 0 ? Values.Dequeue() : 0.0;
        }

        string RolledFor(HeistDef heist) => Services.State.modifierRolls.Find(r => r.heistId == heist.id)?.modifierId;

        void Roll(HeistDef heist, HeistModifierDef mod) =>
            Services.State.modifierRolls.Add(new HeistModifierRoll { heistId = heist.id, modifierId = mod.id });

        // ---------------- Escada de golpes ----------------

        [Test]
        public void Loterica_IsAlwaysOpen_EvenWithAnEmptySave()
        {
            Assert.IsTrue(progression.IsUnlocked(loterica));
            Assert.IsFalse(progression.IsUnlocked(joalheria));
            Assert.IsFalse(progression.IsUnlocked(cambio));
        }

        [Test]
        public void MarkCompleted_ClearingTheVault_UnlocksOnlyTheNextHeist()
        {
            progression.MarkCompleted(loterica);

            Assert.IsTrue(progression.IsCompleted(loterica));
            Assert.IsTrue(progression.IsUnlocked(joalheria));
            Assert.IsFalse(progression.IsUnlocked(cambio), "Só libera o próximo degrau da escada.");
            CollectionAssert.AreEqual(new[] { joalheria }, unlockEvents);
        }

        [Test]
        public void MarkCompleted_Twice_RaisesHeistUnlockedOnlyOnce()
        {
            progression.MarkCompleted(loterica);
            progression.MarkCompleted(loterica);

            Assert.AreEqual(1, unlockEvents.Count);
            Assert.AreEqual(1, Services.State.completedHeists.FindAll(id => id == "loterica").Count);
            Assert.AreEqual(1, Services.State.unlockedHeists.FindAll(id => id == "joalheria").Count);
        }

        [Test]
        public void MarkCompleted_WalksTheWholeLadder()
        {
            progression.MarkCompleted(loterica);
            progression.MarkCompleted(joalheria);

            Assert.IsTrue(progression.IsUnlocked(cambio));
            CollectionAssert.AreEqual(new[] { joalheria, cambio }, unlockEvents);
        }

        [Test]
        public void MarkCompleted_ARepeatedHeist_DoesNotUnlockAgain()
        {
            progression.MarkCompleted(loterica);
            unlockEvents.Clear();

            progression.MarkCompleted(loterica); // farm da Lotérica

            Assert.IsEmpty(unlockEvents);
        }

        [Test]
        public void IsUnlocked_SaveWithCompletedButNotUnlocked_IsStillOpen()
        {
            Services.State.completedHeists.Add("loterica"); // save sem a liberação gravada

            Assert.IsTrue(progression.IsUnlocked(joalheria));
        }

        [Test]
        public void IsCompleted_IsFalseUntilTheVaultIsCleared()
        {
            Assert.IsFalse(progression.IsCompleted(loterica));
        }

        [Test]
        public void NullHeist_IsHandledWithoutErrors()
        {
            Assert.IsFalse(progression.IsUnlocked(null));
            Assert.IsFalse(progression.IsCompleted(null));
            Assert.DoesNotThrow(() => progression.MarkCompleted(null));
            Assert.IsNull(progression.GetModifier(null));
            Assert.AreEqual(1f, progression.GetContext(null).lootMult);
        }

        [Test]
        public void AllHeists_ComesFromTheDatabase()
        {
            CollectionAssert.AreEqual(new[] { loterica, joalheria, cambio }, progression.AllHeists);
        }

        // ---------------- Contexto do golpe ----------------

        [Test]
        public void GetContext_WithoutModifierOrWanted_IsNeutral()
        {
            var ctx = progression.GetContext(loterica);

            Assert.AreSame(loterica, ctx.heist);
            Assert.IsNull(ctx.modifier);
            Assert.AreEqual(0, ctx.wantedLevel);
            Assert.AreEqual(1f, ctx.lockDifficultyMult);
            Assert.AreEqual(1f, ctx.lootMult);
            Assert.AreEqual(1f, ctx.alertMult);
        }

        [Test]
        public void GetContext_CopiesTheRolledModifier()
        {
            var mod = Modifier("blitz");
            mod.lockDifficultyMult = 1.2f;
            mod.systemDifficultyMult = 1.3f;
            mod.personDifficultyMult = 1.4f;
            mod.lootMult = 1.5f;
            mod.alertMult = 2f;
            mod.firstRoomsAlertMult = 0.5f;
            mod.firstRoomsCount = 2;
            mod.policeCountdownDelta = -3f;
            Roll(joalheria, mod);

            var ctx = progression.GetContext(joalheria);

            Assert.AreSame(mod, ctx.modifier);
            Assert.AreEqual(1.2f, ctx.lockDifficultyMult, 0.001f);
            Assert.AreEqual(1.3f, ctx.systemDifficultyMult, 0.001f);
            Assert.AreEqual(1.4f, ctx.personDifficultyMult, 0.001f);
            Assert.AreEqual(1.5f, ctx.lootMult, 0.001f);
            Assert.AreEqual(2f, ctx.alertMult, 0.001f);
            Assert.AreEqual(0.5f, ctx.firstRoomsAlertMult, 0.001f);
            Assert.AreEqual(2, ctx.firstRoomsCount);
            Assert.AreEqual(-3f, ctx.policeCountdownDelta, 0.001f);
            Assert.IsNull(progression.GetContext(loterica).modifier, "O modificador é só do golpe que sorteou.");
        }

        [Test]
        public void GetContext_WantedLevel_MultipliesDifficultyLootAndAlert()
        {
            db.config.wantedDifficultyMult = 1.25f;
            db.config.wantedLootMult = 1.5f;
            db.config.wantedAlertMult = 1.1f;
            Services.State.wantedLevel = 2;

            var ctx = progression.GetContext(loterica);

            Assert.AreEqual(2, ctx.wantedLevel);
            Assert.AreEqual(1.5625f, ctx.lockDifficultyMult, 0.001f); // 1,25²
            Assert.AreEqual(1.5625f, ctx.systemDifficultyMult, 0.001f);
            Assert.AreEqual(1.5625f, ctx.personDifficultyMult, 0.001f);
            Assert.AreEqual(2.25f, ctx.lootMult, 0.001f);              // 1,5²
            Assert.AreEqual(1.21f, ctx.alertMult, 0.001f);             // 1,1²
        }

        [Test]
        public void GetContext_ModifierAndWanted_Stack()
        {
            var mod = Modifier("festa");
            mod.lootMult = 2f;
            Roll(loterica, mod);
            db.config.wantedLootMult = 1.5f;
            Services.State.wantedLevel = 1;

            Assert.AreEqual(3f, progression.GetContext(loterica).lootMult, 0.001f);
        }

        [Test]
        public void GetModifier_RollPointingToAMissingModifier_IsNull()
        {
            Services.State.modifierRolls.Add(new HeistModifierRoll { heistId = "loterica", modifierId = "nao-existe-mais" });

            Assert.IsNull(progression.GetModifier(loterica));
            Assert.AreEqual(1f, progression.GetContext(loterica).lootMult);
        }

        // ---------------- Modificadores ----------------

        [Test]
        public void RollModifiers_ChanceOne_EveryUnlockedHeistGetsOne()
        {
            Modifier("feriado");
            db.config.modifierChance = 1f;
            progression.MarkCompleted(loterica);

            progression.RollModifiers();

            Assert.AreEqual("feriado", RolledFor(loterica));
            Assert.AreEqual("feriado", RolledFor(joalheria));
            Assert.IsNull(RolledFor(cambio), "Golpe bloqueado não sorteia.");
            Assert.AreEqual(1, rolledEvents);
        }

        [Test]
        public void RollModifiers_ChanceZero_NobodyGetsOne()
        {
            Modifier("feriado");
            db.config.modifierChance = 0f;

            progression.RollModifiers();

            Assert.IsEmpty(Services.State.modifierRolls);
            Assert.AreEqual(1, rolledEvents);
        }

        [Test]
        public void RollModifiers_PicksByWeight()
        {
            Modifier("feriado", weight: 1f);
            Modifier("dia-de-pagamento", weight: 3f);
            db.config.modifierChance = 0.5f;

            rng.Values.Enqueue(0.1); // passa na chance
            rng.Values.Enqueue(0.8); // 0,8 × 4 = 3,2: cai no peso do Dia de pagamento (1 a 4)
            progression.RollModifiers();
            Assert.AreEqual("dia-de-pagamento", RolledFor(loterica));

            GameEvents.RaiseHeistFinished(new HeistResult());
            rng.Values.Enqueue(0.1);
            rng.Values.Enqueue(0.2); // 0,8: cai no peso do Feriado (0 a 1)
            progression.RollModifiers();
            Assert.AreEqual("feriado", RolledFor(loterica));
        }

        [Test]
        public void RollModifiers_FailedChance_LeavesTheHeistWithout()
        {
            Modifier("feriado");
            db.config.modifierChance = 0.5f;

            rng.Values.Enqueue(0.5); // 0,5 não é menor que a chance de 0,5
            progression.RollModifiers();

            Assert.IsNull(RolledFor(loterica));
        }

        [Test]
        public void RollModifiers_IgnoresZeroWeightAndMissingModifiers()
        {
            db.modifiers.Add(null);
            Modifier("nunca", weight: 0f);
            Modifier("feriado", weight: 1f);
            db.config.modifierChance = 1f;

            rng.Values.Enqueue(0.0);
            rng.Values.Enqueue(0.99);
            progression.RollModifiers();

            Assert.AreEqual("feriado", RolledFor(loterica));
        }

        [Test]
        public void RollModifiers_WithoutAnyModifier_RollsNothing()
        {
            db.config.modifierChance = 1f;

            Assert.DoesNotThrow(() => progression.RollModifiers());
            Assert.IsEmpty(Services.State.modifierRolls);
        }

        [Test]
        public void RollModifiers_LotericaBeforeTheTutorial_NeverRolls()
        {
            Modifier("feriado");
            db.config.modifierChance = 1f;
            Services.State.tutorialDone = false;
            progression.MarkCompleted(loterica);

            progression.RollModifiers();

            Assert.IsNull(RolledFor(loterica));
            Assert.AreEqual("feriado", RolledFor(joalheria));
        }

        [Test]
        public void RollModifiers_OpeningTheMapAgain_KeepsTheRollUntilAHeistFinishes()
        {
            Modifier("feriado");
            db.config.modifierChance = 1f;
            progression.RollModifiers();

            db.config.modifierChance = 0f;
            progression.RollModifiers(); // abriu o mapa de novo sem jogar
            Assert.AreEqual("feriado", RolledFor(loterica), "Abrir e fechar o mapa não troca o modificador.");
            Assert.AreEqual(1, rolledEvents);

            GameEvents.RaiseHeistFinished(new HeistResult());
            progression.RollModifiers();
            Assert.IsNull(RolledFor(loterica));
            Assert.AreEqual(2, rolledEvents);
        }

        [Test]
        public void RollModifiers_NewGame_RollsAgain()
        {
            Modifier("feriado");
            db.config.modifierChance = 1f;
            progression.RollModifiers();
            db.config.modifierChance = 0f;

            GameEvents.RaiseNewGameStarted();
            progression.RollModifiers();

            Assert.IsEmpty(Services.State.modifierRolls);
        }

        // ---------------- Fama ----------------

        [Test]
        public void GetFamePerkCost_GrowsEachLevelAndRoundsUp()
        {
            var esquema = Perk("esquema", FamePerkEffect.LootPercent, baseCost: 1, growth: 1.5f);

            Assert.AreEqual(1, progression.GetFamePerkCost(esquema));
            GameState.SetLevel(Services.State.famePerks, "esquema", 2);
            Assert.AreEqual(3, progression.GetFamePerkCost(esquema)); // 1 × 1,5² = 2,25
        }

        [Test]
        public void GetFamePerkCost_ExactValues_DoNotRoundUpByFloatError()
        {
            var lenda = Perk("lenda", FamePerkEffect.RecruitBonusLevels, baseCost: 5, growth: 1.6f);
            GameState.SetLevel(Services.State.famePerks, "lenda", 1);

            Assert.AreEqual(8, progression.GetFamePerkCost(lenda)); // 5 × 1,6 = 8, e não 9
        }

        [Test]
        public void BuyFamePerk_SpendsFameRaisesLevelAndRaisesEvent()
        {
            var esquema = Perk("esquema", FamePerkEffect.LootPercent, baseCost: 1, growth: 2f);
            Services.State.fame = 5;

            Assert.IsTrue(progression.BuyFamePerk(esquema));
            Assert.IsTrue(progression.BuyFamePerk(esquema));

            Assert.AreEqual(2, progression.GetFamePerkLevel(esquema));
            Assert.AreEqual(2, progression.Fame); // 5 − 1 − 2
            CollectionAssert.AreEqual(new[] { (esquema, 1), (esquema, 2) }, perkEvents);
        }

        [Test]
        public void CanBuyFamePerk_WithoutFame_SaysHowMuchIsMissing()
        {
            var lenda = Perk("lenda", FamePerkEffect.RecruitBonusLevels, baseCost: 3);
            Services.State.fame = 1;

            Assert.IsFalse(progression.CanBuyFamePerk(lenda, out var reason));
            Assert.AreEqual("Falta 2 de Fama.", reason);
            Assert.IsFalse(progression.BuyFamePerk(lenda));
            Assert.AreEqual(1, progression.Fame);
            Assert.IsEmpty(perkEvents);
        }

        [Test]
        public void CanBuyFamePerk_AtMaxLevel_SaysSo()
        {
            var lenda = Perk("lenda", FamePerkEffect.RecruitBonusLevels, maxLevel: 1);
            Services.State.fame = 100;
            progression.BuyFamePerk(lenda);

            Assert.IsFalse(progression.CanBuyFamePerk(lenda, out var reason));
            Assert.AreEqual("Nível máximo.", reason);
        }

        [Test]
        public void GetFamePerkLevel_FromSaveAboveTheMax_IsClampedToTheMax()
        {
            var lenda = Perk("lenda", FamePerkEffect.RecruitBonusLevels, maxLevel: 3, valuePerLevel: 1f);
            GameState.SetLevel(Services.State.famePerks, "lenda", 9);

            Assert.AreEqual(3, progression.GetFamePerkLevel(lenda));
            Assert.AreEqual(3f, progression.GetFamePerkTotal(FamePerkEffect.RecruitBonusLevels), 0.001f);
        }

        [Test]
        public void GetFamePerkTotal_SumsLevelTimesValueOfThatEffectOnly()
        {
            Perk("esquema", FamePerkEffect.LootPercent, valuePerLevel: 10f);
            Perk("rede", FamePerkEffect.SideJobPercent, valuePerLevel: 15f);
            db.famePerks.Add(null);
            GameState.SetLevel(Services.State.famePerks, "esquema", 2);
            GameState.SetLevel(Services.State.famePerks, "rede", 1);

            Assert.AreEqual(20f, progression.GetFamePerkTotal(FamePerkEffect.LootPercent), 0.001f);
            Assert.AreEqual(15f, progression.GetFamePerkTotal(FamePerkEffect.SideJobPercent), 0.001f);
            Assert.AreEqual(0f, progression.GetFamePerkTotal(FamePerkEffect.StartingMoney), 0.001f);
        }

        [Test]
        public void FamePerk_Null_IsHandledWithoutErrors()
        {
            Assert.AreEqual(0, progression.GetFamePerkLevel(null));
            Assert.AreEqual(0, progression.GetFamePerkCost(null));
            Assert.IsFalse(progression.CanBuyFamePerk(null, out _));
            Assert.IsFalse(progression.BuyFamePerk(null));
        }

        // ---------------- Prestígio ----------------

        /// <summary>Ciclo pronto pra sumir do mapa: a escada toda limpa, com dinheiro, upgrade, crew e candidatos.</summary>
        void FinishTheCycle(double cycleLoot = 250000)
        {
            progression.MarkCompleted(loterica);
            progression.MarkCompleted(joalheria);
            progression.MarkCompleted(cambio);
            Services.State.cycleLoot = cycleLoot;
            Services.State.lifetimeLoot = cycleLoot;
            Services.State.money = 99999;
            GameState.SetLevel(Services.State.upgrades, "van", 2);
            Services.State.modifierRolls.Add(new HeistModifierRoll { heistId = "loterica", modifierId = "feriado" });
            Services.State.crew.Clear();
            Services.State.crew.Add(new ThiefState { id = "veterano", level = 9, status = ThiefStatus.Available });
            Services.State.crew.Add(new ThiefState { id = "preso", level = 5, status = ThiefStatus.Jailed });
            Services.State.candidates.Add(new RecruitCandidate());
            Services.State.candidatesRefreshAtUnix = 123;
        }

        [Test]
        public void CanPrestige_BeforeTheTopOfTheLadder_SaysWhatIsMissing()
        {
            cambio.displayName = "Câmbio";
            progression.MarkCompleted(loterica);

            Assert.IsFalse(progression.CanPrestige(out var reason));
            Assert.AreEqual("Libera depois de limpar o cofre: Câmbio.", reason);
        }

        [Test]
        public void CanPrestige_AfterTheTopOfTheLadder_IsTrue()
        {
            FinishTheCycle();

            Assert.IsTrue(progression.CanPrestige(out var reason), reason);
        }

        [Test]
        public void CanPrestige_ExtraHeistWithHighTier_IsNotTheTop()
        {
            var museu = Heist("museu", unlockedBy: joalheria, tier: 9);
            museu.extra = true;
            progression.MarkCompleted(loterica);
            progression.MarkCompleted(joalheria);
            progression.MarkCompleted(museu);

            Assert.AreSame(cambio, ProgressionService.FinalHeist(db));
            Assert.IsFalse(progression.CanPrestige(out _), "Golpe extra não libera o prestígio.");
        }

        [Test]
        public void CanPrestige_DuringAHeist_IsFalse()
        {
            FinishTheCycle();
            loterica.rooms.Add(new RoomData { obstacle = ObstacleType.Lock });
            var thief = new ThiefState { id = "ze", level = 1, status = ThiefStatus.Available };
            Services.State.crew.Add(thief);
            Services.Heist.StartHeist(loterica, new[] { thief });
            Assume.That(Services.Heist.IsRunning);

            Assert.IsFalse(progression.CanPrestige(out var reason));
            Assert.AreEqual("Termina o golpe antes de sumir do mapa.", reason);
        }

        [Test]
        public void FameOnPrestige_IsSqrtOfTheCycleLoot()
        {
            db.config.fameDivisor = 10000;
            Services.State.cycleLoot = 250000;

            Assert.AreEqual(5, progression.FameOnPrestige()); // √25
        }

        [Test]
        public void Prestige_ResetsTheCycle_AndKeepsFameAndPerks()
        {
            db.config.fameDivisor = 10000;
            db.config.startingMoney = 0;
            db.config.startingCrew = new List<ThiefType> { ThiefType.ConArtist, ThiefType.Locksmith };
            var esquema = Perk("esquema", FamePerkEffect.LootPercent);
            GameState.SetLevel(Services.State.famePerks, "esquema", 2);
            Services.State.fame = 3;
            Services.State.tutorialDone = true;
            FinishTheCycle(cycleLoot: 250000);

            progression.Prestige();

            var s = Services.State;
            Assert.AreEqual(8, s.fame, "3 que tinha + 5 do ciclo.");
            Assert.AreEqual(2, progression.GetFamePerkLevel(esquema), "Bônus de Fama ficam.");
            Assert.AreEqual(1, s.wantedLevel);
            Assert.AreEqual(1, s.prestigeCount);
            Assert.AreEqual(0.0, s.cycleLoot);
            Assert.AreEqual(250000.0, s.lifetimeLoot, "O total da vida não zera.");
            Assert.AreEqual(0.0, s.money);
            Assert.IsEmpty(s.upgrades);
            CollectionAssert.AreEqual(new[] { "loterica" }, s.unlockedHeists);
            Assert.IsEmpty(s.completedHeists);
            Assert.IsEmpty(s.modifierRolls);
            Assert.IsEmpty(s.candidates);
            Assert.AreEqual(0, s.candidatesRefreshAtUnix, "A Crew sorteia candidatos novos.");
            Assert.IsTrue(s.tutorialDone, "Tutorial não volta.");

            Assert.AreEqual(2, s.crew.Count, "Dona Cida manda os novatos de novo.");
            CollectionAssert.AreEquivalent(new[] { ThiefType.ConArtist, ThiefType.Locksmith }, s.crew.ConvertAll(t => t.type));
            Assert.IsTrue(s.crew.TrueForAll(t => t.level == 1 && t.status == ThiefStatus.Available && t.traitId == null));
        }

        [Test]
        public void Prestige_HerancaAndLenda_GiveMoneyAndLevelsToTheNewCycle()
        {
            db.config.startingMoney = 50;
            Perk("heranca", FamePerkEffect.StartingMoney, valuePerLevel: 1000f);
            Perk("lenda", FamePerkEffect.RecruitBonusLevels, valuePerLevel: 1f);
            GameState.SetLevel(Services.State.famePerks, "heranca", 2);
            GameState.SetLevel(Services.State.famePerks, "lenda", 2);
            FinishTheCycle();

            progression.Prestige();

            Assert.AreEqual(2050.0, Services.State.money);
            Assert.IsTrue(Services.State.crew.TrueForAll(t => t.level == 3), "Lenda vale pros novatos da Dona Cida também.");
        }

        [Test]
        public void Prestige_LendaNeverPassesTheMaxLevel()
        {
            db.config.maxLevel = 4;
            Perk("lenda", FamePerkEffect.RecruitBonusLevels, maxLevel: 10, valuePerLevel: 1f);
            GameState.SetLevel(Services.State.famePerks, "lenda", 10);
            FinishTheCycle();

            progression.Prestige();

            Assert.IsTrue(Services.State.crew.TrueForAll(t => t.level == 4));
        }

        [Test]
        public void Prestige_RaisesPrestigedAndSaves()
        {
            db.config.fameDivisor = 10000;
            FinishTheCycle(cycleLoot: 90000);
            int savesBefore = save.SaveCount;

            progression.Prestige();

            CollectionAssert.AreEqual(new[] { (3, 1) }, prestigeEvents);
            Assert.Greater(save.SaveCount, savesBefore);
        }

        [Test]
        public void Prestige_TwiceStacksTheWantedLevel()
        {
            FinishTheCycle();
            progression.Prestige();
            FinishTheCycle();
            progression.Prestige();

            Assert.AreEqual(2, Services.State.wantedLevel);
            Assert.AreEqual(2, Services.State.prestigeCount);
        }

        [Test]
        public void Prestige_WhenNotAllowed_ChangesNothing()
        {
            Services.State.money = 500;
            Services.State.cycleLoot = 250000;

            progression.Prestige();

            Assert.AreEqual(500.0, Services.State.money);
            Assert.AreEqual(0, Services.State.wantedLevel);
            Assert.IsEmpty(prestigeEvents);
        }

        [Test]
        public void Prestige_NewCycle_RollsTheMapAgain()
        {
            Modifier("feriado");
            db.config.modifierChance = 1f;
            progression.RollModifiers();
            FinishTheCycle();

            progression.Prestige();
            progression.RollModifiers();

            Assert.AreEqual(1, Services.State.modifierRolls.Count, "Só a Lotérica está liberada no ciclo novo.");
            Assert.AreEqual(2, rolledEvents);
        }
    }
}
