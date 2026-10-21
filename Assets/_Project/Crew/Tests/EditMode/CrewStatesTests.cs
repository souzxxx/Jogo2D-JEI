using System.Collections.Generic;
using AFirma.Core;
using AFirma.Crew;
using NUnit.Framework;
using static AFirma.Tests.CrewTestSupport;

namespace AFirma.Tests
{
    /// <summary>Estados e erros da crew: save que não fecha e anti dead-end (ESCOPO 10).</summary>
    public class CrewStatesTests
    {
        CrewService crew;

        [SetUp]
        public void SetUp() => crew = InitWithCrew();

        [TearDown]
        public void TearDown() => Teardown();

        // ---------------- Save que não fecha ----------------

        [Test]
        public void Load_ThiefOnHeistWithoutAnActiveHeist_BecomesAvailable()
        {
            var thief = Thief(ThiefType.Locksmith);
            thief.status = ThiefStatus.OnHeist;

            GameEvents.RaiseGameLoaded();

            Assert.AreEqual(ThiefStatus.Available, thief.status);
        }

        [Test]
        public void Load_ThiefOfTheInterruptedHeist_IsLeftForTheHeistRecovery()
        {
            var thief = Thief(ThiefType.Locksmith);
            thief.status = ThiefStatus.OnHeist;
            Services.State.hasActiveHeist = true;
            Services.State.activeHeist = new ActiveHeistRecord { heistId = "loterica", bag = 50 };
            Services.State.activeHeist.crewIds.Add(thief.id);

            GameEvents.RaiseGameLoaded();

            Assert.AreEqual(ThiefStatus.OnHeist, thief.status, "O RecoverInterruptedHeist é quem devolve e paga a sacola.");
        }

        [Test]
        public void Load_ThiefOnASideJobThatDoesNotExist_BecomesAvailable()
        {
            var gone = OnSideJob("bico-que-sumiu");
            var none = OnSideJob(null);
            var noThiefJob = OnSideJob("catar-latinha"); // não usa ladrão
            var valid = OnSideJob("chaveiro-24h");

            GameEvents.RaiseGameLoaded();

            Assert.AreEqual(ThiefStatus.Available, gone.status);
            Assert.IsNull(gone.sideJobId);
            Assert.AreEqual(ThiefStatus.Available, none.status);
            Assert.AreEqual(ThiefStatus.Available, noThiefJob.status);
            Assert.AreEqual(ThiefStatus.OnSideJob, valid.status);
            Assert.AreEqual("chaveiro-24h", valid.sideJobId);
        }

        // ---------------- Anti dead-end ----------------

        [Test]
        public void AllJailedAndBroke_DonaCidaSendsARookie()
        {
            JailEveryone();
            var bips = RecordBips();

            crew.Tick(1f);

            var free = Services.State.crew.FindAll(t => t.status == ThiefStatus.Available);
            Assert.AreEqual(1, free.Count);
            Assert.AreEqual(1, free[0].level);
            Assert.AreEqual(1, bips.Count);
            Assert.AreEqual("Dona Cida", bips[0].Key);
            StringAssert.Contains(free[0].DisplayName, bips[0].Value);
            StringAssert.DoesNotContain("{nome}", bips[0].Value);
        }

        [Test]
        public void EmptyCrewAndBroke_DonaCidaSendsARookie()
        {
            Services.State.crew.Clear();

            crew.Tick(1f);

            Assert.AreEqual(1, Services.State.crew.Count);
            Assert.AreEqual(ThiefStatus.Available, Services.State.crew[0].status);
        }

        [Test]
        public void DonaCida_SendsOnlyOne()
        {
            JailEveryone();
            int before = Services.State.crew.Count;

            crew.Tick(1f);
            crew.Tick(1f);
            crew.Tick(1f);

            Assert.AreEqual(before + 1, Services.State.crew.Count);
        }

        [Test]
        public void DonaCida_StaysHome_WhenThereIsMoneyToRecruit()
        {
            JailEveryone();
            Services.Economy.Add(1000, "teste");
            int before = Services.State.crew.Count;

            crew.Tick(1f);

            Assert.AreEqual(before, Services.State.crew.Count);
        }

        [Test]
        public void DonaCida_StaysHome_WhenSomeoneIsFree()
        {
            JailEveryone();
            Services.State.crew[0].status = ThiefStatus.OnSideJob; // dá pra tirar do bico e mandar pro golpe
            int before = Services.State.crew.Count;

            crew.Tick(1f);

            Assert.AreEqual(before, Services.State.crew.Count);
        }

        [Test]
        public void DonaCida_Comes_WhenTheHideoutIsFullOfPrisoners_EvenWithMoney()
        {
            while (Services.State.crew.Count < crew.MaxCrewSize) Thief(ThiefType.Hacker);
            JailEveryone();
            Services.Economy.Add(1000, "teste");
            int before = Services.State.crew.Count;

            crew.Tick(1f);

            Assert.AreEqual(before + 1, Services.State.crew.Count, "Sem vaga não dá pra recrutar: conta como sem saída.");
        }

        // ---------------- Apoio ----------------

        void JailEveryone()
        {
            foreach (var thief in Services.State.crew) crew.Jail(thief, 600f);
        }

        static ThiefState OnSideJob(string jobId)
        {
            var thief = Thief(ThiefType.Locksmith);
            thief.status = ThiefStatus.OnSideJob;
            thief.sideJobId = jobId;
            return thief;
        }

        static List<KeyValuePair<string, string>> RecordBips()
        {
            var bips = new List<KeyValuePair<string, string>>();
            GameEvents.Bip += (sender, message) => bips.Add(new KeyValuePair<string, string>(sender, message));
            return bips;
        }
    }
}
