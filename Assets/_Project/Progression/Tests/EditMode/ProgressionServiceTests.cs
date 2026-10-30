using System.Collections.Generic;
using AFirma.Core;
using AFirma.Progression;
using NUnit.Framework;
using UnityEngine;

namespace AFirma.Tests
{
    /// <summary>Progressão: escada de golpes, modificadores e contexto do golpe (ESCOPO 6.9). Golpes de teste, pra não depender do conteúdo.</summary>
    public class ProgressionServiceTests
    {
        ProgressionService progression;
        GameDatabase db;
        HeistDef loterica, joalheria, cambio;
        readonly List<HeistDef> unlockEvents = new List<HeistDef>();
        readonly FixedRandom rng = new FixedRandom();
        int rolledEvents;

        [SetUp]
        public void SetUp()
        {
            TestSupport.InitServicesWithMocks();
            db = ScriptableObject.CreateInstance<GameDatabase>();
            db.config = ScriptableObject.CreateInstance<GameConfig>();
            Services.UseDatabase(db);
            Services.State.unlockedHeists.Clear();
            Services.State.completedHeists.Clear();
            Services.State.modifierRolls.Clear();
            Services.State.wantedLevel = 0;
            Services.State.tutorialDone = true;

            loterica = Heist("loterica", unlockedByDefault: true, tier: 1);
            joalheria = Heist("joalheria", unlockedBy: loterica, tier: 2);
            cambio = Heist("cambio", unlockedBy: joalheria, tier: 3);

            rng.Values.Clear();
            progression = new ProgressionService(rng);
            Services.Register((IProgressionService)progression);

            unlockEvents.Clear();
            rolledEvents = 0;
            GameEvents.HeistUnlocked += heist => unlockEvents.Add(heist);
            GameEvents.HeistModifiersRolled += () => rolledEvents++;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var heist in db.heists) Object.DestroyImmediate(heist);
            foreach (var mod in db.modifiers) Object.DestroyImmediate(mod);
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
    }
}
