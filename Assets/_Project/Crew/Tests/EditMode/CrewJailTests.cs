using System.Collections.Generic;
using AFirma.Core;
using AFirma.Crew;
using NUnit.Framework;
using static AFirma.Tests.CrewTestSupport;

namespace AFirma.Tests
{
    /// <summary>Prisão e fiança em tempo real (ESCOPO 6.6).</summary>
    public class CrewJailTests
    {
        CrewService crew;

        [SetUp]
        public void SetUp() => crew = InitWithCrew();

        [TearDown]
        public void TearDown() => Teardown();

        // ---------------- Prender ----------------

        [Test]
        public void Jail_LocksTheThiefForThatLong_AndRaisesThiefJailed()
        {
            var thief = Thief(ThiefType.Locksmith);
            var jailed = new List<ThiefState>();
            GameEvents.ThiefJailed += t => jailed.Add(t);

            crew.Jail(thief, 60f);

            Assert.AreEqual(ThiefStatus.Jailed, thief.status);
            Assert.AreEqual(60f, crew.SecondsLeftInJail(thief));
            CollectionAssert.AreEqual(new[] { thief }, jailed);
        }

        [Test]
        public void Jail_TakesTheThiefOutOfTheSideJob()
        {
            var thief = Thief(ThiefType.Locksmith);
            thief.status = ThiefStatus.OnSideJob;
            thief.sideJobId = "chaveiro-24h";

            crew.Jail(thief, 60f);

            Assert.IsNull(thief.sideJobId);
        }

        [Test]
        public void Jail_AdvogadoCutsTheTime()
        {
            GameState.SetLevel(Services.State.upgrades, "advogado", 3); // −30%
            var thief = Thief(ThiefType.Locksmith);

            crew.Jail(thief, 60f);

            Assert.AreEqual(42f, crew.SecondsLeftInJail(thief));
        }

        [Test]
        public void JailSeconds_DoesNotRoundUpBecauseOfFloatNoise()
        {
            Assert.AreEqual(54, CrewRules.JailSeconds(60f, 10f)); // 60 × 0,9 = 54,0000014 em float
            Assert.AreEqual(42, CrewRules.JailSeconds(60f, 30f));
            Assert.AreEqual(31, CrewRules.JailSeconds(61f, 50f)); // 30,5 sobe pra 31
        }

        [Test]
        public void Jail_WhileAlreadyJailed_KeepsTheLongerSentence()
        {
            var thief = Thief(ThiefType.Locksmith);
            crew.Jail(thief, 60f);

            crew.Jail(thief, 10f);
            Assert.AreEqual(60f, crew.SecondsLeftInJail(thief), "Um evento não encurta a pena.");

            crew.Jail(thief, 100f);
            Assert.AreEqual(100f, crew.SecondsLeftInJail(thief));
        }

        [Test]
        public void Jailed_CannotGoToAHeistNorToASideJob()
        {
            var thief = Thief(ThiefType.Locksmith);
            crew.Jail(thief, 60f);

            Assert.IsFalse(Services.Heist.CanStart(Services.Db.GetHeist("loterica"), new List<ThiefState> { thief }, out var reason));
            Assert.IsNotEmpty(reason);
            Assert.IsFalse(Services.Economy.CanAssignSideJob(thief, Services.Db.GetSideJob("chaveiro-24h"), out reason));
            Assert.IsNotEmpty(reason);
            CollectionAssert.DoesNotContain(crew.GetAvailable(), thief);
        }

        // ---------------- Soltar pelo tempo ----------------

        [Test]
        public void Release_WhenTheTimeIsUp_OnTheNextTick()
        {
            var thief = Thief(ThiefType.Locksmith);
            crew.Jail(thief, 60f);
            var released = new List<ThiefState>();
            GameEvents.ThiefReleased += t => released.Add(t);

            Now += 59;
            crew.Tick(1f);
            Assert.AreEqual(ThiefStatus.Jailed, thief.status, "Falta 1 s.");

            Now += 1;
            crew.Tick(1f);

            Assert.AreEqual(ThiefStatus.Available, thief.status);
            Assert.AreEqual(0f, crew.SecondsLeftInJail(thief));
            CollectionAssert.AreEqual(new[] { thief }, released);
        }

        [Test]
        public void Release_WhenTheGameLoads_IfTheTimeRanOutWithTheGameClosed()
        {
            var thief = Thief(ThiefType.Locksmith);
            thief.status = ThiefStatus.Jailed;
            thief.jailReleaseUnix = Now - 10; // cumpriu a pena com o jogo fechado
            var stillInside = Thief(ThiefType.Hacker);
            stillInside.status = ThiefStatus.Jailed;
            stillInside.jailReleaseUnix = Now + 10;

            GameEvents.RaiseGameLoaded();

            Assert.AreEqual(ThiefStatus.Available, thief.status);
            Assert.AreEqual(ThiefStatus.Jailed, stillInside.status);
        }

        // ---------------- Fiança ----------------

        [Test]
        public void Bail_IsSecondsLeftTimesCostPerSecond_AndGoesDownWithTime()
        {
            var thief = Thief(ThiefType.Locksmith);
            crew.Jail(thief, 60f);

            Assert.AreEqual(120.0, crew.GetBailCost(thief)); // 60 s × Cr$ 2
            Now += 30;
            Assert.AreEqual(60.0, crew.GetBailCost(thief));
        }

        [Test]
        public void Bail_PaoDuroPaysLess()
        {
            var thief = Thief(ThiefType.Locksmith, traitId: "pao-duro"); // fiança ×0,6
            crew.Jail(thief, 60f);

            Assert.AreEqual(72.0, crew.GetBailCost(thief));
        }

        [Test]
        public void Bail_AdvogadoCutsTimeAndPrice()
        {
            GameState.SetLevel(Services.State.upgrades, "advogado", 3); // −30%
            var thief = Thief(ThiefType.Locksmith);
            crew.Jail(thief, 60f);

            Assert.AreEqual(59.0, crew.GetBailCost(thief)); // 42 s × Cr$ 2 × 0,7 = 58,8
        }

        [Test]
        public void PayBail_SpendsAndReleasesNow()
        {
            Services.Economy.Add(200, "teste");
            var thief = Thief(ThiefType.Locksmith);
            crew.Jail(thief, 60f);
            int released = 0;
            GameEvents.ThiefReleased += t => released++;

            Assert.IsTrue(crew.PayBail(thief));

            Assert.AreEqual(80.0, Services.Economy.Money);
            Assert.AreEqual(ThiefStatus.Available, thief.status);
            Assert.AreEqual(1, released);
        }

        [Test]
        public void PayBail_WithoutMoney_SaysHowMuchIsMissing()
        {
            Services.Economy.Add(20, "teste");
            var thief = Thief(ThiefType.Locksmith);
            crew.Jail(thief, 60f);

            Assert.IsFalse(crew.CanPayBail(thief, out var reason));
            Assert.AreEqual("Falta " + Formulas.Money(100) + ".", reason);
            Assert.IsFalse(crew.PayBail(thief));
            Assert.AreEqual(ThiefStatus.Jailed, thief.status);
            Assert.AreEqual(20.0, Services.Economy.Money);
        }

        [Test]
        public void PayBail_ForSomeoneFree_IsRefused()
        {
            var thief = Thief(ThiefType.Locksmith);

            Assert.IsFalse(crew.CanPayBail(thief, out var reason));
            Assert.IsNotEmpty(reason);
        }
    }
}
