using System.Collections.Generic;
using AFirma.Core;
using AFirma.Economy;
using NUnit.Framework;
using UnityEngine;

namespace AFirma.Tests
{
    /// <summary>Economia: Cr$, upgrades do esconderijo (ESCOPO 6.8), bicos e ganho offline (ESCOPO 6.7). Dados de teste, pra não depender dos números da planilha.</summary>
    public class EconomyServiceTests
    {
        EconomyService economy;
        GameDatabase db;
        readonly List<(double current, double delta)> moneyEvents = new List<(double, double)>();
        readonly List<(UpgradeDef upgrade, int level)> upgradeEvents = new List<(UpgradeDef, int)>();
        int sideJobEvents;
        readonly List<(double money, double seconds)> offlineEvents = new List<(double, double)>();

        [SetUp]
        public void SetUp()
        {
            TestSupport.InitServicesWithMocks();
            db = ScriptableObject.CreateInstance<GameDatabase>();
            db.config = ScriptableObject.CreateInstance<GameConfig>();
            Services.UseDatabase(db);
            Services.State.money = 0;
            Services.State.upgrades.Clear();
            Services.State.famePerks.Clear();
            Services.State.crew.Clear();

            economy = new EconomyService();
            Services.Register((IEconomyService)economy);

            moneyEvents.Clear();
            upgradeEvents.Clear();
            sideJobEvents = 0;
            offlineEvents.Clear();
            GameEvents.MoneyChanged += (current, delta) => moneyEvents.Add((current, delta));
            GameEvents.UpgradePurchased += (upgrade, level) => upgradeEvents.Add((upgrade, level));
            GameEvents.SideJobsChanged += () => sideJobEvents++;
            GameEvents.OfflineEarnings += (money, seconds) => offlineEvents.Add((money, seconds));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var upgrade in db.upgrades)
                if (upgrade != null) Object.DestroyImmediate(upgrade);
            foreach (var job in db.sideJobs)
                if (job != null) Object.DestroyImmediate(job);
            foreach (var perk in db.famePerks)
                if (perk != null) Object.DestroyImmediate(perk);
            Object.DestroyImmediate(db.config);
            Object.DestroyImmediate(db);
            TestSupport.Teardown();
        }

        UpgradeDef Upgrade(string id, UpgradeEffect effect, double baseCost = 100, float growth = 2f, int maxLevel = 3, float valuePerLevel = 5f)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDef>();
            upgrade.id = id;
            upgrade.effect = effect;
            upgrade.baseCost = baseCost;
            upgrade.costGrowth = growth;
            upgrade.maxLevel = maxLevel;
            upgrade.valuePerLevel = valuePerLevel;
            db.upgrades.Add(upgrade);
            return upgrade;
        }

        SideJobDef SideJob(string id, double income = 3, ThiefType preferred = ThiefType.ConArtist, float match = 2f)
        {
            var job = ScriptableObject.CreateInstance<SideJobDef>();
            job.id = id;
            job.requiresThief = true;
            job.hasPreferredType = true;
            job.preferredType = preferred;
            job.incomePerSecond = income;
            job.matchMultiplier = match;
            db.sideJobs.Add(job);
            return job;
        }

        ThiefState Thief(string id, ThiefType type, int level = 1, ThiefStatus status = ThiefStatus.Available)
        {
            var thief = new ThiefState { id = id, firstName = id, nickname = type.ToString(), type = type, level = level, status = status };
            Services.State.crew.Add(thief);
            return thief;
        }

        FamePerkDef FamePerk(string id, FamePerkEffect effect, float valuePerLevel)
        {
            var perk = ScriptableObject.CreateInstance<FamePerkDef>();
            perk.id = id;
            perk.effect = effect;
            perk.valuePerLevel = valuePerLevel;
            db.famePerks.Add(perk);
            return perk;
        }

        // ---------------- Cr$ ----------------

        [Test]
        public void Add_IncreasesMoney_AndRaisesMoneyChanged()
        {
            economy.Add(150, "teste");

            Assert.AreEqual(150.0, economy.Money);
            Assert.AreEqual(1, moneyEvents.Count);
            Assert.AreEqual((150.0, 150.0), moneyEvents[0]);
        }

        [Test]
        public void Add_IgnoresZeroNegativeAndInvalidAmounts()
        {
            economy.Add(0, "teste");
            economy.Add(-50, "teste");
            economy.Add(double.NaN, "teste");
            economy.Add(double.PositiveInfinity, "teste");

            Assert.AreEqual(0.0, economy.Money);
            Assert.IsEmpty(moneyEvents, "Valor inválido não pode avisar a UI.");
        }

        [Test]
        public void TrySpend_WithEnoughMoney_SpendsAndRaisesNegativeDelta()
        {
            economy.Add(100, "teste");
            moneyEvents.Clear();

            Assert.IsTrue(economy.TrySpend(40, "teste"));
            Assert.AreEqual(60.0, economy.Money);
            Assert.AreEqual((60.0, -40.0), moneyEvents[0]);
        }

        [Test]
        public void TrySpend_WithoutEnoughMoney_FailsAndKeepsMoney()
        {
            economy.Add(30, "teste");
            moneyEvents.Clear();

            Assert.IsFalse(economy.TrySpend(31, "teste"));
            Assert.AreEqual(30.0, economy.Money);
            Assert.IsEmpty(moneyEvents);
        }

        [Test]
        public void TrySpend_NegativeOrInvalidAmount_NeverCreatesMoney()
        {
            economy.Add(30, "teste");

            Assert.IsFalse(economy.TrySpend(-10, "teste"));
            Assert.IsFalse(economy.TrySpend(double.NaN, "teste"));
            Assert.AreEqual(30.0, economy.Money);
        }

        [Test]
        public void CanAfford_IsTrueOnlyUpToTheMoneyInHand()
        {
            economy.Add(50, "teste");

            Assert.IsTrue(economy.CanAfford(50));
            Assert.IsFalse(economy.CanAfford(50.01));
            Assert.IsFalse(economy.CanAfford(-1));
        }

        // ---------------- Upgrades ----------------

        [Test]
        public void GetUpgradeCost_GrowsByTheDefGrowthEachLevel()
        {
            var upgrade = Upgrade("van", UpgradeEffect.HeistSlots, baseCost: 100, growth: 2f);

            Assert.AreEqual(100.0, economy.GetUpgradeCost(upgrade));
            GameState.SetLevel(Services.State.upgrades, "van", 2);
            Assert.AreEqual(400.0, economy.GetUpgradeCost(upgrade)); // 100 × 2²
        }

        [Test]
        public void CanBuyUpgrade_WithoutMoney_SaysHowMuchIsMissing()
        {
            var upgrade = Upgrade("van", UpgradeEffect.HeistSlots, baseCost: 100);
            economy.Add(30, "teste");

            Assert.IsFalse(economy.CanBuyUpgrade(upgrade, out var reason));
            Assert.AreEqual("Falta Cr$ 70.", reason);
        }

        [Test]
        public void BuyUpgrade_SpendsRaisesLevelAndRaisesUpgradePurchased()
        {
            var upgrade = Upgrade("van", UpgradeEffect.HeistSlots, baseCost: 100, growth: 2f);
            economy.Add(1000, "teste");

            Assert.IsTrue(economy.BuyUpgrade(upgrade));
            Assert.IsTrue(economy.BuyUpgrade(upgrade));

            Assert.AreEqual(2, economy.GetUpgradeLevel(upgrade));
            Assert.AreEqual(700.0, economy.Money); // 1000 − 100 − 200
            Assert.AreEqual(2, upgradeEvents.Count);
            Assert.AreEqual((upgrade, 2), upgradeEvents[1]);
        }

        [Test]
        public void BuyUpgrade_AtMaxLevel_FailsWithReasonAndKeepsMoney()
        {
            var upgrade = Upgrade("van", UpgradeEffect.HeistSlots, maxLevel: 1);
            economy.Add(10000, "teste");
            Assert.IsTrue(economy.BuyUpgrade(upgrade));
            double money = economy.Money;

            Assert.IsFalse(economy.CanBuyUpgrade(upgrade, out var reason));
            Assert.AreEqual("Nível máximo.", reason);
            Assert.IsFalse(economy.BuyUpgrade(upgrade));
            Assert.AreEqual(money, economy.Money);
            Assert.AreEqual(1, economy.GetUpgradeLevel(upgrade));
        }

        [Test]
        public void BuyUpgrade_WithoutMoney_ChangesNothing()
        {
            var upgrade = Upgrade("van", UpgradeEffect.HeistSlots, baseCost: 100);
            economy.Add(99, "teste");

            Assert.IsFalse(economy.BuyUpgrade(upgrade));
            Assert.AreEqual(0, economy.GetUpgradeLevel(upgrade));
            Assert.AreEqual(99.0, economy.Money);
            Assert.IsEmpty(upgradeEvents);
        }

        [Test]
        public void NullUpgrade_IsHandledWithoutErrors()
        {
            Assert.AreEqual(0, economy.GetUpgradeLevel(null));
            Assert.AreEqual(0.0, economy.GetUpgradeCost(null));
            Assert.IsFalse(economy.CanBuyUpgrade(null, out var reason));
            Assert.IsNotNull(reason);
            Assert.IsFalse(economy.BuyUpgrade(null));
        }

        [Test]
        public void GetUpgradeLevel_FromSaveAboveTheMax_IsClampedToTheMax()
        {
            var upgrade = Upgrade("van", UpgradeEffect.HeistSlots, maxLevel: 3);
            GameState.SetLevel(Services.State.upgrades, "van", 9); // save de quando o máximo era maior

            Assert.AreEqual(3, economy.GetUpgradeLevel(upgrade));
            Assert.IsFalse(economy.CanBuyUpgrade(upgrade, out _));
        }

        [Test]
        public void GetUpgradeTotal_SumsLevelTimesValueOfEveryUpgradeWithThatEffect()
        {
            Upgrade("ferramentas", UpgradeEffect.SkillPercent, valuePerLevel: 5f);
            Upgrade("ferramentas-extra", UpgradeEffect.SkillPercent, valuePerLevel: 2f);
            Upgrade("lavanderia", UpgradeEffect.LaundryPercent, valuePerLevel: 10f);
            GameState.SetLevel(Services.State.upgrades, "ferramentas", 3);
            GameState.SetLevel(Services.State.upgrades, "ferramentas-extra", 1);
            GameState.SetLevel(Services.State.upgrades, "lavanderia", 2);

            Assert.AreEqual(17f, economy.GetUpgradeTotal(UpgradeEffect.SkillPercent), 0.001f); // 3×5 + 1×2
            Assert.AreEqual(20f, economy.GetUpgradeTotal(UpgradeEffect.LaundryPercent), 0.001f);
            Assert.AreEqual(0f, economy.GetUpgradeTotal(UpgradeEffect.HeistSlots), 0.001f);
        }

        [Test]
        public void GetUpgradeTotal_IgnoresMissingUpgradesInTheDatabase()
        {
            db.upgrades.Add(null);
            Upgrade("ferramentas", UpgradeEffect.SkillPercent, valuePerLevel: 5f);
            GameState.SetLevel(Services.State.upgrades, "ferramentas", 1);

            Assert.AreEqual(5f, economy.GetUpgradeTotal(UpgradeEffect.SkillPercent), 0.001f);
        }

        // ---------------- Bicos ----------------

        [Test]
        public void IncomePerSecond_WithNobodyOnSideJobs_IsJustCatarLatinha()
        {
            db.config.scavengeIncomePerSecond = 1.5;
            Thief("ze", ThiefType.ConArtist);

            Assert.AreEqual(1.5, economy.IncomePerSecond, 1e-6);
        }

        [Test]
        public void GetSideJobIncome_GrowsTenPercentPerLevelAboveOne()
        {
            var job = SideJob("bilhete", income: 3, preferred: ThiefType.ConArtist);
            var driver = Thief("tiao", ThiefType.Driver, level: 3);

            Assert.AreEqual(3.6, economy.GetSideJobIncome(driver, job), 1e-6); // 3 × (1 + 0,1 × 2)
        }

        [Test]
        public void GetSideJobIncome_RightTypeEarnsTheMatchMultiplier()
        {
            var job = SideJob("bilhete", income: 3, preferred: ThiefType.ConArtist, match: 2f);

            Assert.AreEqual(6.0, economy.GetSideJobIncome(Thief("ze", ThiefType.ConArtist), job), 1e-6);
            Assert.AreEqual(3.0, economy.GetSideJobIncome(Thief("tiao", ThiefType.Driver), job), 1e-6);
        }

        [Test]
        public void GetSideJobIncome_AddsPontoDeBicoAndFamaRede()
        {
            var job = SideJob("bilhete", income: 3, preferred: ThiefType.ConArtist);
            Upgrade("ponto-de-bico", UpgradeEffect.SideJobPercent, valuePerLevel: 10f);
            GameState.SetLevel(Services.State.upgrades, "ponto-de-bico", 2);
            FamePerk("rede", FamePerkEffect.SideJobPercent, valuePerLevel: 5f);
            GameState.SetLevel(Services.State.famePerks, "rede", 2);

            // 3 × (1 + 20%) × (1 + 10%)
            Assert.AreEqual(3.96, economy.GetSideJobIncome(Thief("tiao", ThiefType.Driver), job), 1e-6);
        }

        [Test]
        public void AssignSideJob_PutsTheThiefOnTheJobAndAddsTheIncome()
        {
            db.config.scavengeIncomePerSecond = 1;
            var job = SideJob("bilhete", income: 3, preferred: ThiefType.ConArtist);
            var ze = Thief("ze", ThiefType.ConArtist);

            economy.AssignSideJob(ze, job);

            Assert.AreEqual(ThiefStatus.OnSideJob, ze.status);
            Assert.AreEqual("bilhete", ze.sideJobId);
            Assert.AreEqual(1, sideJobEvents);
            Assert.AreEqual(7.0, economy.IncomePerSecond, 1e-6); // latinha 1 + bilhete 6
        }

        [Test]
        public void AssignSideJob_FromAnotherJob_MovesTheThief()
        {
            var bilhete = SideJob("bilhete");
            var chaveiro = SideJob("chaveiro", preferred: ThiefType.Locksmith);
            var ze = Thief("ze", ThiefType.ConArtist);
            economy.AssignSideJob(ze, bilhete);

            Assert.IsTrue(economy.CanAssignSideJob(ze, chaveiro, out _));
            economy.AssignSideJob(ze, chaveiro);

            Assert.AreEqual("chaveiro", ze.sideJobId);
            Assert.AreEqual(ThiefStatus.OnSideJob, ze.status);
        }

        [Test]
        public void CanAssignSideJob_SaysWhyTheThiefCannotGo()
        {
            var job = SideJob("bilhete");
            var latinha = SideJob("latinha");
            latinha.requiresThief = false;
            var outsider = new ThiefState { id = "x", firstName = "Xis", status = ThiefStatus.Available };

            Assert.IsFalse(economy.CanAssignSideJob(Thief("ze", ThiefType.ConArtist, status: ThiefStatus.OnHeist), job, out var reason));
            Assert.AreEqual("ze está num golpe.", reason);
            Assert.IsFalse(economy.CanAssignSideJob(Thief("tiao", ThiefType.Driver, status: ThiefStatus.Jailed), job, out reason));
            Assert.AreEqual("tiao está em cana.", reason);
            Assert.IsFalse(economy.CanAssignSideJob(Thief("bia", ThiefType.Hacker), latinha, out reason));
            Assert.IsFalse(economy.CanAssignSideJob(outsider, job, out reason), "Candidato que não é da crew não vai pro bico.");
            Assert.IsFalse(economy.CanAssignSideJob(null, job, out reason));

            var onJob = Thief("lu", ThiefType.ConArtist);
            economy.AssignSideJob(onJob, job);
            Assert.IsFalse(economy.CanAssignSideJob(onJob, job, out reason));
            Assert.AreEqual("lu já está nesse bico.", reason);
        }

        [Test]
        public void AssignSideJob_WhenNotAllowed_ChangesNothing()
        {
            var job = SideJob("bilhete");
            var jailed = Thief("ze", ThiefType.ConArtist, status: ThiefStatus.Jailed);

            economy.AssignSideJob(jailed, job);

            Assert.AreEqual(ThiefStatus.Jailed, jailed.status);
            Assert.IsNull(jailed.sideJobId);
            Assert.AreEqual(0, sideJobEvents);
        }

        [Test]
        public void RemoveFromSideJob_MakesTheThiefAvailableAndStopsTheIncome()
        {
            db.config.scavengeIncomePerSecond = 1;
            var job = SideJob("bilhete");
            var ze = Thief("ze", ThiefType.ConArtist);
            economy.AssignSideJob(ze, job);

            economy.RemoveFromSideJob(ze);

            Assert.AreEqual(ThiefStatus.Available, ze.status);
            Assert.IsNull(ze.sideJobId);
            Assert.AreEqual(2, sideJobEvents);
            Assert.AreEqual(1.0, economy.IncomePerSecond, 1e-6);
        }

        [Test]
        public void GoingToTheHeist_TakesTheThiefOutOfTheSideJob()
        {
            db.config.scavengeIncomePerSecond = 1;
            var job = SideJob("bilhete");
            var ze = Thief("ze", ThiefType.ConArtist);
            economy.AssignSideJob(ze, job);

            Services.Crew.SetStatus(ze, ThiefStatus.OnHeist); // o que o Motor do Golpe faz ao começar

            Assert.IsNull(ze.sideJobId);
            Assert.AreEqual(1.0, economy.IncomePerSecond, 1e-6);
        }

        [Test]
        public void IncomePerSecond_IgnoresASideJobThatNoLongerExists()
        {
            db.config.scavengeIncomePerSecond = 1;
            Thief("ze", ThiefType.ConArtist, status: ThiefStatus.OnSideJob).sideJobId = "bico-que-saiu-do-jogo";

            Assert.AreEqual(1.0, economy.IncomePerSecond, 1e-6);
        }

        [Test]
        public void Tick_PaysWholeCruzeirosAndKeepsTheFraction()
        {
            db.config.scavengeIncomePerSecond = 1.5;

            economy.Tick(1f);
            Assert.AreEqual(1.0, economy.Money);
            economy.Tick(1f);
            Assert.AreEqual(3.0, economy.Money); // 1,5 + 1,5: a fração do primeiro não se perde
            Assert.AreEqual(2, moneyEvents.Count);
        }

        [Test]
        public void Tick_WithZeroOrNegativeTime_PaysNothing()
        {
            db.config.scavengeIncomePerSecond = 10;

            economy.Tick(0f);
            economy.Tick(-5f);

            Assert.AreEqual(0.0, economy.Money);
        }

        // ---------------- Offline ----------------

        /// <summary>Catar latinha 1/s + um 171 no Bilhete premiado (3 × 2) = 7/s.</summary>
        void SevenPerSecond()
        {
            db.config.scavengeIncomePerSecond = 1;
            economy.AssignSideJob(Thief("ze", ThiefType.ConArtist), SideJob("bilhete", income: 3, preferred: ThiefType.ConArtist));
        }

        [Test]
        public void ApplyOfflineEarnings_OneHour_PaysHalfOfTheSideJobs()
        {
            SevenPerSecond();

            Assert.AreEqual(12600.0, economy.ApplyOfflineEarnings(3600)); // 3.600 s × 7 × 50%
            Assert.AreEqual(12600.0, economy.Money);
            Assert.AreEqual((12600.0, 3600.0), offlineEvents[0]);
        }

        [Test]
        public void ApplyOfflineEarnings_TenHours_StopsAtFourHours()
        {
            SevenPerSecond();

            Assert.AreEqual(50400.0, economy.ApplyOfflineEarnings(36000)); // 4 h × 7 × 50%
            Assert.AreEqual((50400.0, 36000.0), offlineEvents[0], "O popup mostra o tempo fora de verdade.");
        }

        [Test]
        public void ApplyOfflineEarnings_ClockBackInTimeOrInvalid_PaysNothing()
        {
            SevenPerSecond();

            Assert.AreEqual(0.0, economy.ApplyOfflineEarnings(-3600));
            Assert.AreEqual(0.0, economy.ApplyOfflineEarnings(double.NaN));
            Assert.AreEqual(0.0, economy.Money);
            Assert.IsEmpty(offlineEvents);
        }

        [Test]
        public void ApplyOfflineEarnings_LessThanTheMinimum_PaysNothing()
        {
            SevenPerSecond();
            db.config.offlineMinSeconds = 60;

            Assert.AreEqual(0.0, economy.ApplyOfflineEarnings(59));
            Assert.IsEmpty(offlineEvents, "Recarregar a página não abre o popup.");
            Assert.AreEqual(210.0, economy.ApplyOfflineEarnings(60)); // 60 × 7 × 50%
        }

        [Test]
        public void ApplyOfflineEarnings_PaysWholeCruzeiros()
        {
            db.config.scavengeIncomePerSecond = 1.5;

            Assert.AreEqual(67.0, economy.ApplyOfflineEarnings(90)); // 90 × 1,5 × 50% = 67,5
        }

        [Test]
        public void GameLoaded_PaysTheTimeSinceTheLastSave()
        {
            db.config.scavengeIncomePerSecond = 1;
            Services.State.lastSaveUnix = 1000;
            GameClock.UnixNowProvider = () => 1000 + 3600;

            GameEvents.RaiseGameLoaded();

            Assert.AreEqual(1800.0, economy.Money); // 3.600 s × 1 × 50%
            Assert.AreEqual((1800.0, 3600.0), offlineEvents[0]);
        }

        [Test]
        public void GameLoaded_WithTheClockBeforeTheLastSave_PaysNothing()
        {
            db.config.scavengeIncomePerSecond = 1;
            Services.State.lastSaveUnix = 5000;
            GameClock.UnixNowProvider = () => 1000;

            GameEvents.RaiseGameLoaded();

            Assert.AreEqual(0.0, economy.Money);
            Assert.IsEmpty(offlineEvents);
        }
    }
}
