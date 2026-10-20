using System.Collections;
using AFirma.Core;
using AFirma.Heist;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AFirma.Tests
{
    public class HeistInstallerTests
    {
        [UnityTest]
        public IEnumerator RealRunner_IsTheHeistServiceWhenTheGameRuns()
        {
            yield return null;
            Assert.IsInstanceOf<HeistRunner>(Services.Heist, "O jogo ainda está usando o MockHeistService.");
        }
    }
}
