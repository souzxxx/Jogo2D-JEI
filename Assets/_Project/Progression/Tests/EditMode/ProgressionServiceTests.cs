using System.Collections.Generic;
using AFirma.Core;
using AFirma.Progression;
using NUnit.Framework;
using UnityEngine;

namespace AFirma.Tests
{
    /// <summary>Progressão: escada de golpes e contexto do golpe (ESCOPO 6.9). Golpes de teste, pra não depender do conteúdo.</summary>
    public class ProgressionServiceTests
    {
        ProgressionService progression;
        GameDatabase db;
        HeistDef loterica, joalheria, cambio;
        readonly List<HeistDef> unlockEvents = new List<HeistDef>();

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

            loterica = Heist("loterica", unlockedByDefault: true);
            joalheria = Heist("joalheria", unlockedBy: loterica);
            cambio = Heist("cambio", unlockedBy: joalheria);

            progression = new ProgressionService();
            Services.Register((IProgressionService)progression);

            unlockEvents.Clear();
            GameEvents.HeistUnlocked += heist => unlockEvents.Add(heist);
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

        HeistDef Heist(string id, bool unlockedByDefault = false, HeistDef unlockedBy = null)
        {
            var heist = ScriptableObject.CreateInstance<HeistDef>();
            heist.id = id;
            heist.unlockedByDefault = unlockedByDefault;
            heist.unlockedBy = unlockedBy;
            db.heists.Add(heist);
            return heist;
        }

        HeistModifierDef Modifier(string id)
        {
            var mod = ScriptableObject.CreateInstance<HeistModifierDef>();
            mod.id = id;
            db.modifiers.Add(mod);
            return mod;
        }

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
    }
}
