using System.Collections;
using AFirma.Core;
using AFirma.Crew;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AFirma.Tests
{
    public class CrewInstallerTests
    {
        [UnityTest]
        public IEnumerator RealCrew_IsTheCrewServiceWhenTheGameRuns()
        {
            yield return null;
            Assert.IsInstanceOf<CrewService>(Services.Crew, "O jogo ainda está usando o MockCrewService.");
        }
    }
}
