using System.Collections.Generic;
using AFirma.Core;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AFirma.Tests
{
    /// <summary>Garante que os mocks funcionam ponta a ponta: a Cena do Golpe e a UI dependem deles.</summary>
    public class MockServicesTests
    {
        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true; // o mock de áudio/golpe loga avisos
            TestSupport.InitServicesWithMocks();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            TestSupport.Teardown();
        }

        [Test]
        public void NewGame_StartsWithDonaCidaCrewAndLotericaUnlocked()
        {
            var s = Services.State;
            Assert.AreEqual(2, s.crew.Count);
            Assert.IsTrue(s.crew.Exists(t => t.type == ThiefType.ConArtist));
            Assert.IsTrue(s.crew.Exists(t => t.type == ThiefType.Locksmith));
            Assert.IsTrue(Services.Progression.IsUnlocked(Services.Db.GetHeist("loterica")));
            Assert.IsFalse(Services.Progression.IsUnlocked(Services.Db.GetHeist("joalheria")));
        }

        [Test]
        public void MockAutoHeist_RunsOnItsOwn_AndPaysTheAutoMultiplier()
        {
            var heist = Services.Db.GetHeist("loterica");
            var crew = new List<ThiefState>(Services.State.crew);
            Assert.IsFalse(Services.AutoHeist.CanSend(heist, crew, out _), "Só vai pro automático depois de limpar o cofre na mão.");
            Services.State.completedHeists.Add(heist.id);
            Assert.IsTrue(Services.AutoHeist.CanSend(heist, crew, out var reason), reason);

            HeistResult finished = null;
            string stopped = "não parou";
            GameEvents.AutoHeistFinished += (run, result) => finished = result;
            GameEvents.AutoHeistStopped += (run, why) => stopped = why;
            double before = Services.Economy.Money;

            Services.AutoHeist.Send(heist, crew, AutoHeistOrder.ToTheVault, repeat: false);
            Assert.AreEqual(1, Services.AutoHeist.Running.Count);
            Assert.AreEqual(ThiefStatus.OnHeist, crew[0].status);
            var tick = (ITickable)Services.AutoHeist;
            for (int i = 0; i < 2000 && Services.AutoHeist.Running.Count > 0; i++) tick.Tick(0.25f);

            Assert.IsNotNull(finished);
            Assert.IsTrue(finished.auto);
            Assert.IsNull(stopped);
            Assert.Greater(Services.Economy.Money, before);
            Assert.AreEqual(ThiefStatus.Available, crew[0].status);
        }

        [Test]
        public void MockHeist_RunsToTheEnd_PaysAndUnlocksNext()
        {
            var heist = Services.Db.GetHeist("loterica");
            HeistResult finished = null;
            GameEvents.HeistFinished += r => finished = r;
            int rooms = 0;
            GameEvents.RoomCleared += (i, room, loot) => rooms++;

            var crew = new List<ThiefState>(Services.Crew.GetAvailable());
            Assert.IsTrue(Services.Heist.CanStart(heist, crew, out var reason), reason);
            Services.Heist.StartHeist(heist, crew);
            Assert.IsTrue(Services.State.hasActiveHeist);

            RunUntilFinished(goToNextRoom: true);

            Assert.IsNotNull(finished);
            Assert.IsFalse(Services.Heist.IsRunning);
            Assert.IsFalse(Services.State.hasActiveHeist);
            Assert.Greater(rooms, 0);
            Assert.Greater(Services.Economy.Money, 0);
            foreach (var thief in crew)
                Assert.That(thief.status, Is.EqualTo(ThiefStatus.Available).Or.EqualTo(ThiefStatus.Jailed));
            if (finished.completed)
                Assert.IsTrue(Services.Progression.IsUnlocked(Services.Db.GetHeist("joalheria")));
        }

        [Test]
        public void FleeingAtFirstDoor_BeforeSiren_KeepsWholeBag()
        {
            var heist = Services.Db.GetHeist("loterica");
            var crew = new List<ThiefState>(Services.Crew.GetAvailable());
            Services.Heist.StartHeist(heist, crew);

            for (int i = 0; i < 2000 && Services.Heist.Phase != HeistPhase.AwaitingDoor && Services.Heist.IsRunning; i++)
            {
                if (Services.Heist.Phase == HeistPhase.AwaitingEvent) Services.Heist.ChooseEventOption(FirstAvailable());
                else TickHeist(0.25f);
            }

            if (Services.Heist.SirenActive) Assert.Inconclusive("Sirene tocou antes da primeira porta.");
            var bag = Services.Heist.Bag;
            Services.Heist.ChooseDoor(false);

            Assert.AreEqual(HeistOutcome.CleanEscape, Services.Heist.LastResult.outcome);
            Assert.AreEqual(bag, Services.Heist.LastResult.moneyGained);
            Assert.IsEmpty(Services.Heist.LastResult.arrestedThiefIds);
        }

        [Test]
        public void InterruptedHeist_IsRecoveredAsEscapeWithBag()
        {
            var s = Services.State;
            var thief = s.crew[0];
            thief.status = ThiefStatus.OnHeist;
            s.hasActiveHeist = true;
            s.activeHeist = new ActiveHeistRecord { heistId = "loterica", bag = 150 };
            s.activeHeist.crewIds.Add(thief.id);

            var result = Services.Heist.RecoverInterruptedHeist();

            Assert.AreEqual(HeistOutcome.Interrupted, result.outcome);
            Assert.AreEqual(150, Services.Economy.Money);
            Assert.AreEqual(ThiefStatus.Available, thief.status);
            Assert.IsFalse(s.hasActiveHeist);
        }

        [Test]
        public void Jail_ReleasesAfterTime_AndBailCostsMoney()
        {
            long now = 1000;
            GameClock.UnixNowProvider = () => now;
            var thief = Services.State.crew[0];

            Services.Crew.Jail(thief, 60);
            Assert.AreEqual(ThiefStatus.Jailed, thief.status);
            Assert.IsFalse(Services.Crew.CanPayBail(thief, out _), "Sem dinheiro não paga fiança.");

            Services.Economy.Add(100000, "teste");
            Assert.IsTrue(Services.Crew.PayBail(thief));
            Assert.AreEqual(ThiefStatus.Available, thief.status);

            Services.Crew.Jail(thief, 60);
            now += 61;
            ((ITickable)Services.Crew).Tick(1.1f);
            Assert.AreEqual(ThiefStatus.Available, thief.status);
        }

        [Test]
        public void Upgrade_CannotBeBoughtWithoutMoney_ThenAppliesEffect()
        {
            var van = Services.Db.GetUpgrade("van");
            int slots = Services.Crew.HeistSlots;
            Assert.IsFalse(Services.Economy.CanBuyUpgrade(van, out var reason));
            StringAssert.StartsWith("Falta", reason);

            Services.Economy.Add(Services.Economy.GetUpgradeCost(van), "teste");
            Assert.IsTrue(Services.Economy.BuyUpgrade(van));
            Assert.AreEqual(slots + 1, Services.Crew.HeistSlots);
        }

        static void RunUntilFinished(bool goToNextRoom)
        {
            for (int i = 0; i < 20000 && Services.Heist.IsRunning; i++)
            {
                switch (Services.Heist.Phase)
                {
                    case HeistPhase.AwaitingDoor: Services.Heist.ChooseDoor(goToNextRoom); break;
                    case HeistPhase.AwaitingEvent: Services.Heist.ChooseEventOption(FirstAvailable()); break;
                    default: TickHeist(0.25f); break;
                }
            }
        }

        static void TickHeist(float seconds) => ((ITickable)Services.Heist).Tick(seconds);

        static int FirstAvailable()
        {
            var choices = Services.Heist.PendingEvent.choices;
            for (int i = 0; i < choices.Count; i++)
                if (choices[i].available) return i;
            return 0;
        }
    }
}
