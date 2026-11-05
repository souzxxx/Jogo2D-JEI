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
        public IEnumerator Jobs_ShowcaseLivesWithSomeoneThere()
        {
            var root = Root<HideoutScreen>();
            var thief = Services.Crew.GetAvailable()[0];
            Assert.IsTrue(Services.Db.GetSideJob("gatonet").HasScene, "O Gatonet tem cena.");
            Click(root, "jobs");
            yield return null;
            Assert.IsTrue(Find<VisualElement>(root, "showcase-gatonet").ClassListContains("showcase--empty"), "Sem ninguém, a cena fica apagada.");

            Click(root, $"job-gatonet-{thief.id}");
            yield return new WaitForSecondsRealtime(3.5f);
            var scene = Find<VisualElement>(root, "showcase-gatonet");
            Assert.IsFalse(scene.ClassListContains("showcase--empty"), "Com gente, a cena anda.");
            StringAssert.Contains(thief.nickname, scene.Q<Label>(className: "showcase__name").text);
            Assert.IsNotNull(scene.Q(className: "pop"), "Solta o +Cr$ no ritmo da renda.");
            var latinha = Services.Db.sideJobs.FirstOrDefault(j => j != null && !j.requiresThief && j.HasScene);
            if (latinha != null) Assert.IsFalse(Find<VisualElement>(root, "showcase-" + latinha.id).ClassListContains("showcase--empty"), "Catar latinha sempre anda.");
            yield return Print("bicos_vitrine");
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
