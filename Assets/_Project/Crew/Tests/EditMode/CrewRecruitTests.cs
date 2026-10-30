using System.Collections.Generic;
using AFirma.Core;
using AFirma.Crew;
using NUnit.Framework;
using static AFirma.Tests.CrewTestSupport;

namespace AFirma.Tests
{
    /// <summary>Recrutamento (Contatos), demitir e limites de vaga (ESCOPO 6.3).</summary>
    public class CrewRecruitTests
    {
        CrewService crew;

        [SetUp]
        public void SetUp() => crew = InitWithCrew();

        [TearDown]
        public void TearDown() => Teardown();

        // ---------------- Candidatos ----------------

        [Test]
        public void Candidates_AreThree_AndStayUntilTheRefreshTime()
        {
            var first = new List<RecruitCandidate>(crew.Candidates);
            Assert.AreEqual(3, first.Count);
            Assert.AreEqual(300f, crew.SecondsToCandidatesRefresh, 0.001f);

            Now += 299;
            CollectionAssert.AreEqual(first, crew.Candidates, "Ainda não deu 5 min.");

            Now += 1;
            CollectionAssert.IsEmpty(Intersection(first, crew.Candidates), "Deu 5 min: lista nova.");
            Assert.AreEqual(3, crew.Candidates.Count);
        }

        [Test]
        public void Candidates_RefreshOnTick_SoTheScreenUpdatesByItself()
        {
            var first = new List<RecruitCandidate>(crew.Candidates);
            int changed = 0;
            GameEvents.CandidatesChanged += () => changed++;

            Now += 300;
            crew.Tick(1f);

            Assert.AreEqual(1, changed);
            CollectionAssert.IsEmpty(Intersection(first, Services.State.candidates));
        }

        [Test]
        public void Candidates_WithoutContatos_AreLevel1_AndCostTheBase()
        {
            foreach (var candidate in RollMany())
            {
                Assert.AreEqual(1, candidate.thief.level);
                Assert.AreEqual(50.0, candidate.cost);
            }
        }

        [Test]
        public void Candidates_LevelGoesUpTo1PlusContatos()
        {
            GameState.SetLevel(Services.State.upgrades, "contatos", 2);
            var levels = new HashSet<int>();

            foreach (var candidate in RollMany()) levels.Add(candidate.thief.level);

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, levels);
        }

        [Test]
        public void Candidates_PriceFollowsTheLevel()
        {
            GameState.SetLevel(Services.State.upgrades, "contatos", 2);

            foreach (var candidate in RollMany())
                Assert.AreEqual(CrewRules.RecruitCost(candidate.thief.level, 0f, Services.Config), candidate.cost);
        }

        [Test]
        public void Candidates_FameLenda_AddsLevelsForFree()
        {
            GameState.SetLevel(Services.State.famePerks, "lenda", 2); // +2 níveis

            foreach (var candidate in RollMany())
            {
                Assert.AreEqual(3, candidate.thief.level);
                Assert.AreEqual(50.0, candidate.cost, "Preço do nível sorteado (1), não do nível com Lenda.");
            }
        }

        [Test]
        public void Candidates_FameReputacao_MakesThemCheaper()
        {
            GameState.SetLevel(Services.State.famePerks, "reputacao", 2); // −20%

            foreach (var candidate in RollMany()) Assert.AreEqual(40.0, candidate.cost);
        }

        [Test]
        public void Candidates_NeverAboveTheMaxLevel()
        {
            GameState.SetLevel(Services.State.upgrades, "contatos", 5);
            GameState.SetLevel(Services.State.famePerks, "lenda", 3);

            foreach (var candidate in RollMany()) Assert.LessOrEqual(candidate.thief.level, Services.Config.maxLevel);
        }

        [Test]
        public void RecruitCost_IsBaseTimesGrowthPerLevel_MinusReputacao()
        {
            var cfg = Services.Config;
            double baseCost = cfg.recruitBaseCost;
            float growth = cfg.recruitCostGrowthPerLevel;
            cfg.recruitBaseCost = 50; // números de teste: o balanceamento muda, a regra não
            cfg.recruitCostGrowthPerLevel = 1.8f;
            try
            {
                Assert.AreEqual(50.0, CrewRules.RecruitCost(1, 0f, cfg));
                Assert.AreEqual(162.0, CrewRules.RecruitCost(3, 0f, cfg));  // 50 × 1,8²
                Assert.AreEqual(130.0, CrewRules.RecruitCost(3, 20f, cfg)); // 162 × 0,8 = 129,6
            }
            finally
            {
                cfg.recruitBaseCost = baseCost;
                cfg.recruitCostGrowthPerLevel = growth;
            }
        }

        // ---------------- Troca paga ----------------

        [Test]
        public void PaidRefresh_CostsAndBringsNewCandidates()
        {
            Services.Economy.Add(100, "teste");
            var first = new List<RecruitCandidate>(crew.Candidates);

            Now += 100;

            Assert.AreEqual(25.0, crew.PaidRefreshCost);
            Assert.IsTrue(crew.RefreshCandidates(paid: true));

            Assert.AreEqual(75.0, Services.Economy.Money);
            CollectionAssert.IsEmpty(Intersection(first, crew.Candidates));
            Assert.AreEqual(300f, crew.SecondsToCandidatesRefresh, 0.001f, "O relógio da troca grátis recomeça.");
        }

        [Test]
        public void PaidRefresh_WithoutMoney_KeepsTheCandidates()
        {
            var first = new List<RecruitCandidate>(crew.Candidates);

            Assert.IsFalse(crew.RefreshCandidates(paid: true));

            CollectionAssert.AreEqual(first, crew.Candidates);
        }

        [Test]
        public void PaidRefresh_IsCheaperWithContatos()
        {
            GameState.SetLevel(Services.State.upgrades, "contatos", 2); // −20%
            Assert.AreEqual(20.0, crew.PaidRefreshCost);
        }

        // ---------------- Recrutar ----------------

        [Test]
        public void Recruit_PaysAndJoinsTheCrewAvailable()
        {
            Services.Economy.Add(1000, "teste");
            var candidate = crew.Candidates[0];
            int crewChanged = 0, candidatesChanged = 0;
            GameEvents.CrewChanged += () => crewChanged++;
            GameEvents.CandidatesChanged += () => candidatesChanged++;

            Assert.IsTrue(crew.Recruit(candidate));

            CollectionAssert.Contains(Services.State.crew, candidate.thief);
            Assert.AreEqual(ThiefStatus.Available, candidate.thief.status);
            Assert.AreEqual(1000 - candidate.cost, Services.Economy.Money);
            CollectionAssert.DoesNotContain(crew.Candidates, candidate);
            Assert.AreEqual(1, crewChanged);
            Assert.AreEqual(1, candidatesChanged);
        }

        [Test]
        public void Recruit_WithoutAFreeRoom_SaysToBuyQuartos()
        {
            Services.Economy.Add(1000, "teste");
            while (Services.State.crew.Count < crew.MaxCrewSize) Thief(ThiefType.Hacker);

            Assert.IsFalse(crew.CanRecruit(crew.Candidates[0], out var reason));
            StringAssert.Contains("Quartos", reason);
            Assert.IsFalse(crew.Recruit(crew.Candidates[0]));
            Assert.AreEqual(1000.0, Services.Economy.Money);
        }

        [Test]
        public void Recruit_WithoutMoney_SaysHowMuchIsMissing()
        {
            var candidate = crew.Candidates[0];

            Assert.IsFalse(crew.CanRecruit(candidate, out var reason));
            Assert.AreEqual("Falta " + Formulas.Money(candidate.cost) + ".", reason);
        }

        [Test]
        public void Recruit_TheSameCandidateTwice_OnlyWorksOnce()
        {
            GameState.SetLevel(Services.State.upgrades, "quartos", 2); // sobra vaga: a recusa tem que vir do candidato
            Services.Economy.Add(1000, "teste");
            var candidate = crew.Candidates[0];
            crew.Recruit(candidate);
            double money = Services.Economy.Money;

            Assert.IsFalse(crew.CanRecruit(candidate, out var reason));
            Assert.IsNotEmpty(reason);
            Assert.IsFalse(crew.Recruit(candidate));
            Assert.AreEqual(money, Services.Economy.Money);
        }

        // ---------------- Demitir ----------------

        [Test]
        public void Fire_RemovesFromTheCrew()
        {
            var thief = Thief(ThiefType.Hacker);
            int changed = 0;
            GameEvents.CrewChanged += () => changed++;

            Assert.IsTrue(crew.CanFire(thief, out var reason), reason);
            crew.Fire(thief);

            CollectionAssert.DoesNotContain(Services.State.crew, thief);
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void Fire_NeverTheLastThief()
        {
            while (Services.State.crew.Count > 1) Services.State.crew.RemoveAt(0);
            var last = Services.State.crew[0];

            Assert.IsFalse(crew.CanFire(last, out var reason));
            Assert.IsNotEmpty(reason);
            crew.Fire(last);
            CollectionAssert.Contains(Services.State.crew, last);
        }

        [Test]
        public void Fire_NeverInTheMiddleOfAHeist()
        {
            var thief = Thief(ThiefType.Hacker);
            thief.status = ThiefStatus.OnHeist;

            Assert.IsFalse(crew.CanFire(thief, out var reason));
            Assert.IsNotEmpty(reason);
            crew.Fire(thief);
            CollectionAssert.Contains(Services.State.crew, thief);
        }

        // ---------------- Apoio ----------------

        /// <summary>Sorteia 30 listas de candidatos (troca grátis a cada 5 min).</summary>
        List<RecruitCandidate> RollMany()
        {
            var all = new List<RecruitCandidate>();
            for (int i = 0; i < 30; i++)
            {
                all.AddRange(crew.Candidates);
                Now += 300;
            }
            return all;
        }

        static List<RecruitCandidate> Intersection(List<RecruitCandidate> a, IReadOnlyList<RecruitCandidate> b) =>
            a.FindAll(c => new List<RecruitCandidate>(b).Contains(c));
    }
}
