using System.Collections.Generic;
using AFirma.Core;
using AFirma.Heist;
using NUnit.Framework;
using static AFirma.Tests.HeistTestSupport;

namespace AFirma.Tests
{
    /// <summary>Fim do golpe: resultado, pagamento, XP, prisões e liberação do próximo (ESCOPO 6.5).</summary>
    public class HeistFinishTests
    {
        HeistRunner runner;

        [SetUp]
        public void SetUp() => runner = InitWithRunner();

        [TearDown]
        public void TearDown() => Teardown();

        // ---------------- Resultado ----------------

        [Test]
        public void Result_TellsTheWholeStory()
        {
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, loot: 100), Room(ObstacleType.Lock, loot: 200)),
                              Crew(AddThief(ThiefType.Locksmith, 50)));
            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);
            runner.ChooseDoor(true);
            runner.AddAlert(100f);

            TickUntil(() => !runner.IsRunning);

            var result = runner.LastResult;
            Assert.AreEqual("teste", result.heistId);
            Assert.AreEqual(HeistOutcome.HastyEscape, result.outcome);
            Assert.AreEqual(2, result.roomsCleared);
            Assert.AreEqual(2, result.totalRooms);
            Assert.IsTrue(result.completed);
            Assert.IsTrue(result.sirenHappened);
            Assert.AreEqual(300.0, result.bag);
            Assert.AreEqual(240.0, result.moneyGained);
            Assert.AreEqual(2, result.xpPerThief);
        }

        // ---------------- Pagamento ----------------

        [Test]
        public void Payment_IsBagTimesExitTimesLaundryTimesEsquema()
        {
            Assert.AreEqual(1056.0, HeistRules.Payment(1000, 0.8f, 10f, 20f)); // 1000 × 0,8 × 1,1 × 1,2
            Assert.AreEqual(100.0, HeistRules.Payment(100, 1f, 0f, 0f));
        }

        [Test]
        public void Finish_PaysWithLaundryAndEsquema_AndCountsTheLoot()
        {
            GameState.SetLevel(Services.State.upgrades, "lavanderia", 2); // +10%
            GameState.SetLevel(Services.State.famePerks, "esquema", 1);   // +10%
            FleeAtTheFirstDoor(AddThief(ThiefType.Locksmith, 50));

            Assert.AreEqual(121.0, runner.LastResult.moneyGained); // 100 × 1,1 × 1,1
            Assert.AreEqual(121.0, Services.Economy.Money);
            Assert.AreEqual(121.0, Services.State.cycleLoot);
            Assert.AreEqual(121.0, Services.State.lifetimeLoot);
        }

        [Test]
        public void Finish_ClearsTheActiveHeist_SavesAndRaisesHeistFinishedOnce()
        {
            var finished = new List<HeistResult>();
            GameEvents.HeistFinished += r => finished.Add(r);

            FleeAtTheFirstDoor(AddThief(ThiefType.Locksmith, 50));

            Assert.AreEqual(1, finished.Count);
            Assert.AreSame(runner.LastResult, finished[0]);
            Assert.AreEqual(HeistPhase.Finished, runner.Phase);
            Assert.IsFalse(Services.State.hasActiveHeist);
            var saved = Services.Save.Load(out _);
            Assert.IsFalse(saved.hasActiveHeist, "O save já sabe que o golpe acabou.");
            Assert.AreEqual(100.0, saved.money, "O save já tem o dinheiro.");
        }

        [Test]
        public void Money_IsNeverPaidTwice()
        {
            int finished = 0;
            GameEvents.HeistFinished += r => finished++;
            FleeAtTheFirstDoor(AddThief(ThiefType.Locksmith, 50));

            runner.Flee();
            runner.PerfectEscape();
            runner.ChooseDoor(false);
            Tick(30f);
            Assert.IsNull(runner.RecoverInterruptedHeist(), "Não tem golpe interrompido pra recuperar.");

            Assert.AreEqual(100.0, Services.Economy.Money);
            Assert.AreEqual(100.0, Services.State.cycleLoot);
            Assert.AreEqual(1, finished);
        }

        [Test]
        public void RecoverInterruptedHeist_PaysLikeACleanEscape_OnlyOnce()
        {
            GameState.SetLevel(Services.State.upgrades, "lavanderia", 2); // +10%
            var s = Services.State;
            s.hasActiveHeist = true;
            s.activeHeist = new ActiveHeistRecord { heistId = "loterica", bag = 100 };

            var result = runner.RecoverInterruptedHeist();
            var again = runner.RecoverInterruptedHeist();

            Assert.AreEqual(110.0, result.moneyGained);
            Assert.AreEqual(110.0, Services.Economy.Money);
            Assert.AreEqual(110.0, s.cycleLoot);
            Assert.IsNull(again);
        }

        // ---------------- XP ----------------

        [Test]
        public void Xp_EveryClearedRoomGivesXp_CriaDaRuaEarnsMore_AndLevelUpsAreInTheResult()
        {
            var cria = AddThief(ThiefType.Locksmith, 50, traitId: "cria-da-rua"); // XP ×1,3
            var other = AddThief(ThiefType.ConArtist, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock), Room(ObstacleType.Lock), Room(ObstacleType.Lock), Room(ObstacleType.Lock)),
                              Crew(cria, other));

            RunToTheEnd();

            var result = runner.LastResult;
            Assert.AreEqual(4, result.xpPerThief);
            var criaGain = result.xpGains.Find(g => g.thiefId == cria.id);
            var otherGain = result.xpGains.Find(g => g.thiefId == other.id);
            Assert.AreEqual(5, criaGain.xp, "1,3 × 4 = 5,2: a fração vai somando sala a sala.");
            Assert.AreEqual(4, otherGain.xp);
            Assert.AreEqual(2, criaGain.levelAfter, "Nível 1 → 2 com 5 XP.");
            Assert.IsTrue(criaGain.leveledUp);
            Assert.AreEqual(1, otherGain.levelAfter);
            Assert.IsFalse(otherGain.leveledUp);
            CollectionAssert.AreEqual(new[] { cria.id }, result.leveledUpThiefIds);
        }

        [Test]
        public void Xp_FleeingStillKeepsTheXpOfTheClearedRooms()
        {
            var gazua = AddThief(ThiefType.Locksmith, 50);

            FleeAtTheFirstDoor(gazua);

            Assert.AreEqual(1, gazua.xp);
            Assert.AreEqual(1, runner.LastResult.xpGains.Find(g => g.thiefId == gazua.id).xp);
        }

        // ---------------- Prisão ----------------

        [Test]
        public void Arrested_GoToJailForTheHeistTime_TheRestIsAvailable()
        {
            Services.Config.baseArrestChance = 1f;
            Services.Config.stealthArrestFactor = 1f;
            var exposed = AddThief(ThiefType.Locksmith, 50);
            exposed.stealth = 0;   // chance 100%
            var sneaky = AddThief(ThiefType.ConArtist, 50);
            sneaky.stealth = 100;  // chance 0%
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, loot: 100), Room(ObstacleType.Lock, difficulty: 100000)), Crew(exposed, sneaky));
            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);
            runner.AddAlert(100f);

            TickUntil(() => !runner.IsRunning, step: 0.25f, maxSeconds: 20f);

            Assert.AreEqual(HeistOutcome.ForcedEscape, runner.LastResult.outcome);
            CollectionAssert.AreEqual(new[] { exposed.id }, runner.LastResult.arrestedThiefIds);
            Assert.AreEqual(ThiefStatus.Jailed, exposed.status);
            Assert.AreEqual(60f, Services.Crew.SecondsLeftInJail(exposed), 1f, "jailSeconds do golpe de teste.");
            Assert.AreEqual(ThiefStatus.Available, sneaky.status);
        }

        // ---------------- Liberação do próximo golpe ----------------

        [Test]
        public void CompletingTheLoterica_UnlocksTheJoalheria()
        {
            var loterica = Services.Db.GetHeist("loterica");
            var joalheria = Services.Db.GetHeist("joalheria");
            HeistDef unlocked = null;
            GameEvents.HeistUnlocked += h => unlocked = h;
            Assert.IsFalse(Services.Progression.IsUnlocked(joalheria));

            runner.StartHeist(loterica, new List<ThiefState>(Services.Crew.GetAvailable()));
            RunToTheEnd();

            Assert.IsTrue(runner.LastResult.completed);
            Assert.IsTrue(Services.Progression.IsCompleted(loterica));
            Assert.IsTrue(Services.Progression.IsUnlocked(joalheria));
            Assert.AreSame(joalheria, unlocked);
        }

        [Test]
        public void FleeingTheLotericaBeforeTheVault_DoesNotUnlockTheJoalheria()
        {
            var loterica = Services.Db.GetHeist("loterica");
            runner.StartHeist(loterica, new List<ThiefState>(Services.Crew.GetAvailable()));
            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);

            runner.ChooseDoor(false);

            Assert.IsFalse(runner.LastResult.completed);
            Assert.IsFalse(Services.Progression.IsCompleted(loterica));
            Assert.IsFalse(Services.Progression.IsUnlocked(Services.Db.GetHeist("joalheria")));
        }

        // ---------------- Apoio ----------------

        /// <summary>Limpa a 1ª sala (Cr$ 100) e mete o pé na porta, antes da sirene.</summary>
        void FleeAtTheFirstDoor(params ThiefState[] thieves)
        {
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, loot: 100), Room(ObstacleType.Lock)), Crew(thieves));
            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);
            runner.ChooseDoor(false);
        }
    }
}
