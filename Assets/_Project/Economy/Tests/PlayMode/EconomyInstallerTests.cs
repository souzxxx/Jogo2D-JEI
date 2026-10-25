using System.Collections;
using AFirma.Core;
using AFirma.Economy;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AFirma.Tests
{
    public class EconomyInstallerTests
    {
        [UnityTest]
        public IEnumerator RealEconomy_IsTheEconomyServiceWhenTheGameRuns()
        {
            yield return null;
            Assert.IsInstanceOf<EconomyService>(Services.Economy, "O jogo ainda está usando o MockEconomyService.");
        }
    }
}
