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
    public class EconomyPanelTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            UseMemorySave();
            Services.StartNewGame();
            Services.Economy.Add(100000, "teste");
            SceneManager.LoadScene(SceneFlow.Hideout);
            yield return WaitForScene(SceneFlow.Hideout);
        }

        [UnityTest]
        public IEnumerator Jobs_SendAndTakeBack()
        {
            var root = Root<HideoutScreen>();
            var job = Services.Db.sideJobs.First(j => j != null && j.requiresThief);
            var thief = Services.Crew.GetAvailable()[0];
            Click(root, "jobs");
            yield return null;
            yield return Print("bicos");

            Click(root, $"job-{job.id}-{thief.id}");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(job.id, thief.sideJobId);
            Click(root, "leave-" + thief.id);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(ThiefStatus.Available, thief.status);
        }

        [UnityTest]
        public IEnumerator Upgrades_BuyRaisesTheLevel()
        {
            var root = Root<HideoutScreen>();
            var van = Services.Db.GetUpgrade(UpgradeEffect.HeistSlots);
            Click(root, "upgrades");
            yield return null;
            Click(root, "buy-" + van.id);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(1, Services.Economy.GetUpgradeLevel(van));
            yield return Print("upgrades");
        }
    }
}
