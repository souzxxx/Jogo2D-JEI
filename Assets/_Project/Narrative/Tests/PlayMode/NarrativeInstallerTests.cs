using System.Collections;
using AFirma.Core;
using AFirma.Narrative;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AFirma.Tests
{
    public class NarrativeInstallerTests
    {
        [Test]
        public void BipTexts_AreInResources_AndFilled()
        {
            var texts = Resources.Load<BipSetDef>(BipSetDef.ResourcePath);
            Assert.IsNotNull(texts, "Falta o DonaCidaBips.asset em Narrative/Resources.");
            Assert.IsNotEmpty(texts.newGame);
            Assert.IsNotEmpty(texts.firstHeistCompleted);
            Assert.IsNotEmpty(texts.firstSiren);
            StringAssert.Contains("{nome}", texts.firstArrest);
            Assert.IsNotEmpty(texts.prestigeUnlocked);
            foreach (var heist in Services.Db.heists)
                if (heist != null && !heist.unlockedByDefault)
                    Assert.IsNotEmpty(texts.GetHeistUnlocked(heist), "Sem bip pro golpe " + heist.id);
        }

        [UnityTest]
        public IEnumerator SirenDuringPlay_ReachesTheBipEvent()
        {
            yield return null;
            var messages = new System.Collections.Generic.List<string>();
            GameEvents.Bip += (sender, text) => { if (sender == Services.Config.mentorName) messages.Add(text); };
            Services.State.seenTips.Remove(DonaCidaBips.SeenPrefix + "primeira-sirene");

            GameEvents.RaiseSirenStarted(10f);
            yield return null;

            CollectionAssert.Contains(messages, Resources.Load<BipSetDef>(BipSetDef.ResourcePath).firstSiren);
        }
    }
}
