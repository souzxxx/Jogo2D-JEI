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
    }
}
