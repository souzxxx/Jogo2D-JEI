using System.Collections;
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
    public class HideoutTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            UseMemorySave();
            Services.StartNewGame();
            SceneManager.LoadScene(SceneFlow.Hideout);
            yield return WaitForScene(SceneFlow.Hideout);
        }

        [UnityTest]
        public IEnumerator Hub_MoneyRollsAndEveryTabOpens()
        {
            var root = Root<HideoutScreen>();
            Services.Economy.Add(1000, "teste");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(Formulas.Money(Services.Economy.Money), Find<Label>(root, "money").text, "O contador chega no valor do caixa.");
            yield return Print("esconderijo");

            var content = root.Q("content");
            foreach (var tab in new[] { "map", "crew", "contacts", "jobs", "upgrades" })
            {
                Click(root, tab);
                Assert.IsFalse(content.ClassListContains("hidden"), $"A aba {tab} abre o painel.");
                Assert.Greater(content.childCount, 0);
            }
            Click(root, "upgrades");
            Assert.IsTrue(content.ClassListContains("hidden"), "Clicar na aba aberta fecha o painel.");
        }

        [UnityTest]
        public IEnumerator Map_PlanningStartsTheHeist()
        {
            var root = Root<HideoutScreen>();
            Click(root, "map");
            yield return null;
            Assert.AreEqual(Services.Progression.AllHeists.Count, root.Query<Button>(className: "pin").ToList().Count, "Um pino por golpe.");
            yield return Print("mapa");

            Click(root, "plan");
            yield return null;
            var go = Find<Button>(root, "go");
            Assert.IsFalse(go.ClassListContains("btn--off"), go.text);
            yield return Print("planejamento");

            Click(go);
            yield return WaitForScene(SceneFlow.Heist);
            Assert.IsTrue(Services.Heist.IsRunning);
            Services.Heist.Flee();
            yield return null;
        }
    }
}
