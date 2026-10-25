using System.Collections.Generic;
using AFirma.Core;
using AFirma.Economy;
using NUnit.Framework;
using UnityEngine;

namespace AFirma.Tests
{
    /// <summary>Economia: Cr$ e upgrades do esconderijo (ESCOPO 6.8). Upgrades de teste, pra não depender dos números da planilha.</summary>
    public class EconomyServiceTests
    {
        EconomyService economy;
        GameDatabase db;
        readonly List<(double current, double delta)> moneyEvents = new List<(double, double)>();
        readonly List<(UpgradeDef upgrade, int level)> upgradeEvents = new List<(UpgradeDef, int)>();

        [SetUp]
        public void SetUp()
        {
            TestSupport.InitServicesWithMocks();
            db = ScriptableObject.CreateInstance<GameDatabase>();
            db.config = ScriptableObject.CreateInstance<GameConfig>();
            Services.UseDatabase(db);
            Services.State.money = 0;
            Services.State.upgrades.Clear();

            economy = new EconomyService();
            Services.Register((IEconomyService)economy);

            moneyEvents.Clear();
            upgradeEvents.Clear();
            GameEvents.MoneyChanged += (current, delta) => moneyEvents.Add((current, delta));
            GameEvents.UpgradePurchased += (upgrade, level) => upgradeEvents.Add((upgrade, level));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var upgrade in db.upgrades)
                if (upgrade != null) Object.DestroyImmediate(upgrade);
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
    }
}
