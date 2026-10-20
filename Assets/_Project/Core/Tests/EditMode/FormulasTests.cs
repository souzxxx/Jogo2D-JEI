using AFirma.Core;
using NUnit.Framework;

namespace AFirma.Tests
{
    public class FormulasTests
    {
        [Test]
        public void UpgradeCost_GrowsPerLevel()
        {
            Assert.AreEqual(100, Formulas.UpgradeCost(100, 1.6f, 0));
            Assert.AreEqual(256, Formulas.UpgradeCost(100, 1.6f, 2));
        }

        [Test]
        public void FameForLoot_IsSqrtOfLootOverDivisor()
        {
            Assert.AreEqual(0, Formulas.FameForLoot(9999, 10000));
            Assert.AreEqual(2, Formulas.FameForLoot(40000, 10000));
            Assert.AreEqual(0, Formulas.FameForLoot(-5, 10000));
        }

        [Test]
        public void StatAtLevel_StartsAtBase()
        {
            Assert.AreEqual(30f, Formulas.StatAtLevel(30f, 7f, 1));
            Assert.AreEqual(44f, Formulas.StatAtLevel(30f, 7f, 3));
        }

        [Test]
        public void Money_UsesBrazilianThousandsSeparator()
        {
            Assert.AreEqual("Cr$ 1.234.567", Formulas.Money(1234567));
            Assert.AreEqual("Cr$ 0", Formulas.Money(0));
        }
    }
}
