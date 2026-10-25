using System.Collections;
using AFirma.Core;
using AFirma.Progression;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AFirma.Tests
{
    public class ProgressionInstallerTests
    {
        [UnityTest]
        public IEnumerator RealProgression_IsTheProgressionServiceWhenTheGameRuns()
        {
            yield return null;
            Assert.IsInstanceOf<ProgressionService>(Services.Progression, "O jogo ainda está usando o MockProgressionService.");
        }
    }
}
