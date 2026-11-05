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
        public IEnumerator Tutorial_GlowsTheMapUntilSkipped()
        {
            var root = Root<HideoutScreen>();
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.IsTrue(Find<Button>(root, "map").ClassListContains("glow"), "Primeiro passo do tutorial: o Mapa brilha.");
            yield return Print("tutorial");

            Click(root, "jobs");
            Assert.Contains("dica:bicos", Services.State.seenTips, "Primeira vez nos Bicos vira dica.");
            Click(root, "skip-tutorial");
            yield return null;
            Assert.IsTrue(Services.State.tutorialDone);
            Assert.IsNull(root.Q(className: "glow"), "Pulou: some o brilho.");
            Assert.IsTrue(Find<Button>(root, "skip-tutorial").ClassListContains("hidden"));
        }

        [UnityTest]
        public IEnumerator AutoHeist_SendFromPlanningShowsInTheStrip()
        {
            Services.State.tutorialDone = true;
            Services.Progression.MarkCompleted(Services.Db.GetHeist("loterica"));
            var root = Root<HideoutScreen>();
            Click(root, "map");
            yield return null;
            Click(root, "loterica");
            Click(root, "plan");
            yield return null;
            Assert.IsFalse(Find<Button>(root, "auto-order").ClassListContains("hidden"), "Cofre limpo na mão: aparece a ordem.");
            Click(root, "auto-order");
            Click(root, "auto-repeat");
            yield return Print("planejamento_automatico");

            Click(root, "auto-send");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(1, Services.AutoHeist.Running.Count, "Mandou pro automático.");
            var run = Services.AutoHeist.Running[0];
            Assert.AreEqual(AutoHeistOrder.ToTheVault, run.order);
            Assert.IsTrue(run.repeat);
            var card = Find<Button>(root, "auto-loterica");
            Click(root, "crew");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(root.Query<Label>().ToList().Any(l => l.text.StartsWith("Em golpe (automático)")), "A ficha diz que tá no automático.");
            yield return Print("golpes_rolando");

            Click(card);
            yield return null;
            Click(root, "auto-repeat-toggle");
            Assert.IsFalse(run.repeat, "Desliga o Repetir pelo cartão.");
            Click(root, "auto-recall");
            Assert.AreEqual(0, Services.AutoHeist.Running.Count, "Chamar de volta tira da faixa.");
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Print("golpes_rolando_fim");
            Assert.IsNotNull(root.Q(className: "auto__news"), "A volta acabou: mini manchete ao lado do cartão.");
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
