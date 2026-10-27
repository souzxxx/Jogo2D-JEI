using System.Collections;
using System.Linq;
using AFirma.Core;
using AFirma.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static AFirma.Tests.UiTestSupport;

namespace AFirma.Tests
{
    public class PrestigePanelTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            UseMemorySave();
            Services.StartNewGame();
            Services.State.fame = 10;
            SceneManager.LoadScene(SceneFlow.Hideout);
            yield return WaitForScene(SceneFlow.Hideout);
        }

        [UnityTest]
        public IEnumerator Prestige_TabShowsWithFameAndBuysPerks()
        {
            var root = Root<HideoutScreen>();
            Assert.IsFalse(Find<Button>(root, "prestige").ClassListContains("hidden"), "Com Fama a aba aparece.");
            var perk = Services.Db.famePerks.First(p => p != null);
            Click(root, "prestige");
            yield return null;
            Assert.AreEqual(Services.Progression.CanPrestige(out _), !Find<Button>(root, "vanish").ClassListContains("btn--off"));
            yield return Print("prestigio");

            Click(root, "perk-" + perk.id);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(1, Services.Progression.GetFamePerkLevel(perk));
        }
    }
}
