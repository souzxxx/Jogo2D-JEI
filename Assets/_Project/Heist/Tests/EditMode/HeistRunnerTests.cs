using System.Collections.Generic;
using AFirma.Core;
using AFirma.Heist;
using NUnit.Framework;
using static AFirma.Tests.HeistTestSupport;

namespace AFirma.Tests
{
    /// <summary>Motor do Golpe: salas, especialista, sacola e decisão de porta (ESCOPO 6.4).</summary>
    public class HeistRunnerTests
    {
        HeistRunner runner;

        [SetUp]
        public void SetUp() => runner = InitWithRunner();

        [TearDown]
        public void TearDown() => Teardown();

        // ---------------- Início ----------------

        [Test]
        public void CanStart_RefusesWithAReason()
        {
            var gazua = AddThief(ThiefType.Locksmith, 50);
            var heist = MakeHeist(Room(ObstacleType.Lock));

            var locked = MakeHeist(Room(ObstacleType.Lock));
            locked.unlockedByDefault = false;
            Assert.IsFalse(runner.CanStart(locked, Crew(gazua), out var reason));
            Assert.IsNotEmpty(reason);

            Assert.IsFalse(runner.CanStart(heist, Crew(), out reason));
            Assert.IsNotEmpty(reason);

            var crewTooBig = Crew(gazua, AddThief(ThiefType.ConArtist, 50), AddThief(ThiefType.Hacker, 50));
            Assert.IsFalse(runner.CanStart(heist, crewTooBig, out reason), "A van começa com 2 vagas.");
            Assert.IsNotEmpty(reason);

            var jailed = AddThief(ThiefType.Hacker, 50);
            jailed.status = ThiefStatus.Jailed;
            Assert.IsFalse(runner.CanStart(heist, Crew(jailed), out reason));
            StringAssert.Contains(jailed.DisplayName, reason);

            runner.StartHeist(heist, Crew(gazua));
            Assert.IsFalse(runner.CanStart(heist, Crew(AddThief(ThiefType.ConArtist, 50)), out reason), "Um golpe por vez.");
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void CanStart_AcceptsThiefOnSideJob()
        {
            var gazua = AddThief(ThiefType.Locksmith, 50);
            gazua.status = ThiefStatus.OnSideJob;
            Assert.IsTrue(runner.CanStart(MakeHeist(Room(ObstacleType.Lock)), Crew(gazua), out var reason), reason);
        }

        [Test]
        public void StartHeist_PutsCrewOnHeist_AndSavesTheActiveHeist()
        {
            var gazua = AddThief(ThiefType.Locksmith, 50);
            var vigarista = AddThief(ThiefType.ConArtist, 50);
            var save = (MemorySaveService)Services.Save;
            int savesBefore = save.SaveCount;

            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock)), Crew(gazua, vigarista));

            Assert.IsTrue(runner.IsRunning);
            Assert.AreEqual(HeistPhase.Working, runner.Phase);
            Assert.AreEqual(0, runner.RoomIndex);
            Assert.AreEqual(ThiefStatus.OnHeist, gazua.status);
            Assert.AreEqual(ThiefStatus.OnHeist, vigarista.status);
            Assert.IsTrue(Services.State.hasActiveHeist);
            Assert.AreEqual("teste", Services.State.activeHeist.heistId);
            CollectionAssert.AreEqual(new[] { gazua.id, vigarista.id }, Services.State.activeHeist.crewIds);
            Assert.Greater(save.SaveCount, savesBefore, "Salva na hora pra recuperar se o jogo fechar.");
        }

        [Test]
        public void StartHeist_WhenItCannotStart_ChangesNothing()
        {
            var gazua = AddThief(ThiefType.Locksmith, 50);
            var locked = MakeHeist(Room(ObstacleType.Lock));
            locked.unlockedByDefault = false;
            bool started = false;
            GameEvents.HeistStarted += h => started = true;

            runner.StartHeist(locked, Crew(gazua));

            Assert.IsFalse(runner.IsRunning);
            Assert.IsFalse(started);
            Assert.AreEqual(ThiefStatus.Available, gazua.status);
            Assert.IsFalse(Services.State.hasActiveHeist);
        }

        // ---------------- Poder e tempo da sala ----------------

        [Test]
        public void RoomPower_IsTheBestSpecialistSkill_EvenIfSomeoneElseIsBetter()
        {
            var crew = Crew(AddThief(ThiefType.Locksmith, 40), AddThief(ThiefType.Locksmith, 60), AddThief(ThiefType.ConArtist, 90));
            Assert.AreEqual(60f, HeistRules.RoomPower(ObstacleType.Lock, crew, Services.Crew, Services.Db), 0.001f);
        }

        [Test]
        public void RoomPower_WithoutSpecialist_Is35PercentOfTheHighestSkill()
        {
            var crew = Crew(AddThief(ThiefType.Locksmith, 40), AddThief(ThiefType.ConArtist, 80));
            Assert.AreEqual(28f, HeistRules.RoomPower(ObstacleType.System, crew, Services.Crew, Services.Db), 0.001f);
        }

        [Test]
        public void RoomPower_UsesEffectiveSkill_WithTrait()
        {
            var crew = Crew(AddThief(ThiefType.Locksmith, 50, traitId: "supersticioso")); // Perícia ×0,9
            Assert.AreEqual(45f, HeistRules.RoomPower(ObstacleType.Lock, crew, Services.Crew, Services.Db), 0.001f);
        }

        [Test]
        public void RoomPower_DriverIsNeverASpecialist()
        {
            var crew = Crew(AddThief(ThiefType.Driver, 100));
            Assert.IsFalse(HeistRules.HasSpecialist(ObstacleType.Lock, crew, Services.Db));
            Assert.IsFalse(HeistRules.HasSpecialist(ObstacleType.System, crew, Services.Db));
            Assert.IsFalse(HeistRules.HasSpecialist(ObstacleType.Person, crew, Services.Db));
        }

        [Test]
        public void RoomSeconds_IsDifficultyTimesContextOverPower()
        {
            var ctx = new HeistContext { lockDifficultyMult = 1.4f, personDifficultyMult = 0.7f };
            Assert.AreEqual(2.8f, HeistRules.RoomSeconds(Room(ObstacleType.Lock, difficulty: 100), ctx, 50f), 0.001f);
            Assert.AreEqual(1.4f, HeistRules.RoomSeconds(Room(ObstacleType.Person, difficulty: 100), ctx, 50f), 0.001f);
            Assert.AreEqual(2f, HeistRules.RoomSeconds(Room(ObstacleType.System, difficulty: 100), ctx, 50f), 0.001f);
        }

        [Test]
        public void RoomSeconds_NeverBelowHalfASecond()
        {
            Assert.AreEqual(0.5f, HeistRules.RoomSeconds(Room(ObstacleType.Lock, difficulty: 1), new HeistContext(), 100f), 0.001f);
        }

        [Test]
        public void Specialist_ClearsTheRoomFasterThanWithout()
        {
            float withSpecialist = SecondsToClearOneLockRoom(ThiefType.Locksmith); // 100 / 50 = 2 s
            Teardown();
            runner = InitWithRunner();
            float without = SecondsToClearOneLockRoom(ThiefType.ConArtist);       // 100 / (0,35 × 50) ≈ 5,71 s

            Assert.AreEqual(2f, withSpecialist, 0.1f);
            Assert.AreEqual(5.71f, without, 0.1f);
        }

        [Test]
        public void Speed2x_ClearsInHalfTheTime()
        {
            var gazua = AddThief(ThiefType.Locksmith, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 100)), Crew(gazua));
            runner.Speed = 2f;

            float seconds = TickUntil(() => !runner.IsRunning);

            Assert.AreEqual(1f, seconds, 0.1f);
        }

        [Test]
        public void NewHeist_StartsAt1x()
        {
            var gazua = AddThief(ThiefType.Locksmith, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 100)), Crew(gazua));
            runner.Speed = 2f;
            runner.Flee();

            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 100)), Crew(gazua));

            Assert.AreEqual(1f, runner.Speed, "O 2x de um golpe não vaza pro próximo.");
        }

        [Test]
        public void RoomProgress_GoesFromZeroToOneWhileWorking()
        {
            var progress = new List<float>();
            GameEvents.RoomProgress += (i, p) => progress.Add(p);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 100)), Crew(AddThief(ThiefType.Locksmith, 50)));

            TickUntil(() => !runner.IsRunning, step: 0.5f);

            CollectionAssert.AreEqual(new[] { 0.25f, 0.5f, 0.75f, 1f }, progress);
        }

        // ---------------- Sacola ----------------

        [Test]
        public void RoomLoot_AppliesContextAndTraits()
        {
            var ctx = new HeistContext { lootMult = 1.5f };
            var crew = Crew(AddThief(ThiefType.Locksmith, 50, traitId: "fominha"),     // loot ×1,2
                            AddThief(ThiefType.ConArtist, 50, traitId: "mao-boba"));   // loot ×1,3 só em sala de Pessoa

            Assert.AreEqual(234.0, HeistRules.RoomLoot(Room(ObstacleType.Person, loot: 100), ctx, crew, Services.Db), 0.001);
            Assert.AreEqual(180.0, HeistRules.RoomLoot(Room(ObstacleType.Lock, loot: 100), ctx, crew, Services.Db), 0.001);
        }

        [Test]
        public void ClearingARoom_PutsItsLootInTheBag_AndInTheSave()
        {
            double clearedLoot = -1, lastBag = -1;
            int clearedIndex = -1;
            GameEvents.RoomCleared += (i, room, loot) => { clearedIndex = i; clearedLoot = loot; };
            GameEvents.BagChanged += bag => lastBag = bag;
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, loot: 100), Room(ObstacleType.Lock, loot: 300)),
                              Crew(AddThief(ThiefType.Locksmith, 50)));

            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);

            Assert.AreEqual(0, clearedIndex);
            Assert.AreEqual(100.0, clearedLoot);
            Assert.AreEqual(100.0, runner.Bag);
            Assert.AreEqual(100.0, lastBag);
            Assert.AreEqual(100.0, Services.State.activeHeist.bag);
        }

        // ---------------- Porta ----------------

        [Test]
        public void DoorDecision_PreviewsTheNextRoom()
        {
            DoorDecision requested = null;
            GameEvents.DoorDecisionRequested += d => requested = d;
            var heist = MakeHeist(Room(ObstacleType.Lock, loot: 100), Room(ObstacleType.System, loot: 300));
            runner.StartHeist(heist, Crew(AddThief(ThiefType.Locksmith, 50, traitId: "fominha"), AddThief(ThiefType.ConArtist, 50)));

            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);

            Assert.IsNotNull(requested);
            Assert.AreSame(requested, runner.PendingDoor);
            Assert.AreEqual(1, requested.nextRoomIndex);
            Assert.AreSame(heist.rooms[1], requested.nextRoom);
            Assert.IsFalse(requested.hasSpecialist, "Ninguém da crew é Disquete.");
            Assert.AreEqual(360.0, requested.estimatedLoot); // 300 × 1,2 (Fominha)
            Assert.AreEqual(120.0, requested.currentBag);    // 100 × 1,2
            Assert.IsFalse(requested.sirenActive);
        }

        [Test]
        public void DoorDecision_SaysWhenTheCrewHasTheSpecialist()
        {
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock), Room(ObstacleType.System)),
                              Crew(AddThief(ThiefType.Locksmith, 50), AddThief(ThiefType.Hacker, 50)));

            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);

            Assert.IsTrue(runner.PendingDoor.hasSpecialist);
        }

        [Test]
        public void ChooseDoor_NextRoom_EntersIt()
        {
            int entered = -1;
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock), Room(ObstacleType.Lock)), Crew(AddThief(ThiefType.Locksmith, 50)));
            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);
            GameEvents.RoomEntered += (i, room) => entered = i;

            runner.ChooseDoor(true);

            Assert.AreEqual(1, entered);
            Assert.AreEqual(1, runner.RoomIndex);
            Assert.AreEqual(HeistPhase.Working, runner.Phase);
            Assert.IsNull(runner.PendingDoor);
            Assert.AreEqual(0f, runner.RoomProgress01);
        }

        [Test]
        public void ChooseDoor_Flee_BeforeSiren_KeepsTheWholeBag()
        {
            var gazua = AddThief(ThiefType.Locksmith, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, loot: 100), Room(ObstacleType.Lock)), Crew(gazua));
            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);

            runner.ChooseDoor(false);

            Assert.IsFalse(runner.IsRunning);
            Assert.AreEqual(HeistOutcome.CleanEscape, runner.LastResult.outcome);
            Assert.IsFalse(runner.LastResult.completed);
            Assert.AreEqual(100.0, runner.LastResult.moneyGained);
            Assert.AreEqual(100.0, Services.Economy.Money);
            Assert.IsEmpty(runner.LastResult.arrestedThiefIds);
            Assert.AreEqual(ThiefStatus.Available, gazua.status);
            Assert.IsFalse(Services.State.hasActiveHeist);
        }

        [Test]
        public void ChooseDoor_IsIgnoredWhileTheCrewIsWorking()
        {
            bool decided = false;
            GameEvents.DoorDecisionMade += next => decided = true;
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock), Room(ObstacleType.Lock)), Crew(AddThief(ThiefType.Locksmith, 50)));

            runner.ChooseDoor(true);

            Assert.IsFalse(decided);
            Assert.AreEqual(0, runner.RoomIndex);
            Assert.AreEqual(HeistPhase.Working, runner.Phase);
        }

        // ---------------- Golpe inteiro ----------------

        [Test]
        public void ClearingTheVault_CompletesTheHeist()
        {
            var heist = MakeHeist(Room(ObstacleType.Lock, loot: 100), Room(ObstacleType.Person, loot: 200));
            runner.StartHeist(heist, Crew(AddThief(ThiefType.Locksmith, 50), AddThief(ThiefType.ConArtist, 50)));

            RunToTheEnd();

            Assert.IsTrue(runner.LastResult.completed);
            Assert.AreEqual(2, runner.LastResult.roomsCleared);
            Assert.AreEqual(HeistOutcome.CleanEscape, runner.LastResult.outcome);
            Assert.AreEqual(300.0, runner.LastResult.moneyGained);
            Assert.IsTrue(Services.Progression.IsCompleted(heist));
        }

        [Test]
        public void FleeingBeforeTheVault_DoesNotCompleteTheHeist()
        {
            var heist = MakeHeist(Room(ObstacleType.Lock), Room(ObstacleType.Lock));
            runner.StartHeist(heist, Crew(AddThief(ThiefType.Locksmith, 50)));
            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);

            runner.ChooseDoor(false);

            Assert.IsFalse(Services.Progression.IsCompleted(heist));
        }

        [Test]
        public void RaisesTheSameEventsInTheSameOrderAsTheMock()
        {
            Teardown();
            InitWithoutRandomEvents();
            Services.Register((IHeistService)new MockHeistService());
            var mockOrder = RecordTwoRoomHeist();

            Teardown();
            InitWithRunner();
            var runnerOrder = RecordTwoRoomHeist();

            CollectionAssert.AreEqual(new[]
            {
                "HeistStarted", "BagChanged", "RoomEntered 0", "RoomCleared 0", "BagChanged", "DoorDecisionRequested",
                "DoorDecisionMade True", "RoomEntered 1", "RoomCleared 1", "BagChanged", "HeistFinished"
            }, runnerOrder);
            CollectionAssert.AreEqual(mockOrder, runnerOrder);
        }

        // ---------------- Eventos, habilidades e recuperação (mesmo comportamento do mock por enquanto) ----------------
        // Alerta, sirene, saídas e prisão: HeistAlertTests.

        [Test]
        public void TutorialEvent_PausesTheFirstLoterica_UntilTheChoice()
        {
            Services.State.tutorialDone = false;
            bool resolved = false;
            GameEvents.HeistEventResolved += r => resolved = true;
            runner.StartHeist(Services.Db.GetHeist("loterica"), new List<ThiefState>(Services.Crew.GetAvailable()));

            Assert.AreEqual(HeistPhase.AwaitingEvent, runner.Phase);
            Assert.IsTrue(runner.PendingEvent.def.tutorialOnly);
            Tick(5f);
            Assert.AreEqual(0f, runner.RoomProgress01, "Evento pausa o golpe.");

            runner.ChooseEventOption(FirstAvailableChoice());

            Assert.IsTrue(resolved);
            Assert.AreEqual(HeistPhase.Working, runner.Phase);
            Assert.IsTrue(Services.State.tutorialDone);
        }

        [Test]
        public void Ability_WorksOncePerHeist()
        {
            var gazua = AddThief(ThiefType.Locksmith, 50);
            int cleared = 0;
            GameEvents.RoomCleared += (i, room, loot) => cleared++;
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock), Room(ObstacleType.Lock)), Crew(gazua));

            Assert.IsTrue(runner.CanUseAbility(gazua, out var reason), reason);
            runner.UseAbility(gazua); // Pé-de-cabra: limpa a sala de fechadura na hora

            Assert.AreEqual(1, cleared);
            Assert.IsTrue(runner.HasUsedAbility(gazua));
            Assert.IsFalse(runner.CanUseAbility(gazua, out reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void RecoverInterruptedHeist_PaysTheBag_AndFreesTheCrew()
        {
            var s = Services.State;
            var thief = s.crew[0];
            thief.status = ThiefStatus.OnHeist;
            s.hasActiveHeist = true;
            s.activeHeist = new ActiveHeistRecord { heistId = "loterica", bag = 150 };
            s.activeHeist.crewIds.Add(thief.id);

            var result = runner.RecoverInterruptedHeist();

            Assert.AreEqual(HeistOutcome.Interrupted, result.outcome);
            Assert.AreEqual(150.0, Services.Economy.Money);
            Assert.AreEqual(ThiefStatus.Available, thief.status);
            Assert.IsFalse(s.hasActiveHeist);
        }

        // ---------------- Apoio ----------------

        float SecondsToClearOneLockRoom(ThiefType type)
        {
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 100)), Crew(AddThief(type, 50)));
            return TickUntil(() => !runner.IsRunning);
        }

        static List<string> RecordTwoRoomHeist()
        {
            var order = new List<string>();
            GameEvents.HeistStarted += h => order.Add("HeistStarted");
            GameEvents.RoomEntered += (i, room) => order.Add("RoomEntered " + i);
            GameEvents.RoomCleared += (i, room, loot) => order.Add("RoomCleared " + i);
            GameEvents.BagChanged += bag => order.Add("BagChanged");
            GameEvents.DoorDecisionRequested += d => order.Add("DoorDecisionRequested");
            GameEvents.DoorDecisionMade += next => order.Add("DoorDecisionMade " + next);
            GameEvents.HeistFinished += r => order.Add("HeistFinished");

            Services.Heist.StartHeist(MakeHeist(Room(ObstacleType.Lock), Room(ObstacleType.Lock)), Crew(AddThief(ThiefType.Locksmith, 50)));
            RunToTheEnd();
            return order;
        }
    }
}
