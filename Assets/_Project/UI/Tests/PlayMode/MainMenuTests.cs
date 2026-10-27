using System.Collections;
using AFirma.Core;
using AFirma.UI;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static AFirma.Tests.UiTestSupport;

namespace AFirma.Tests
{
    public class MainMenuTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            UseMemorySave();
            yield return null;
        }

        [UnityTest]
        public IEnumerator NewGame_WithoutSave_GoesToHideout()
        {
            SceneManager.LoadScene(SceneFlow.MainMenu);
            yield return WaitForScene(SceneFlow.MainMenu);
            var root = Root<MainMenuScreen>();
            Assert.IsTrue(Find<Button>(root, "continue").ClassListContains("hidden"), "Sem save não tem Continuar.");
            yield return Print("menu_principal");

            Click(root, "new-game");
            yield return WaitForScene(SceneFlow.Hideout);
        }

        [UnityTest]
        public IEnumerator NewGame_WithSave_AsksBeforeWiping()
        {
            Services.StartNewGame();
            Services.State.money = 999;
            SceneManager.LoadScene(SceneFlow.MainMenu);
            yield return WaitForScene(SceneFlow.MainMenu);
            var root = Root<MainMenuScreen>();
            Assert.IsFalse(Find<Button>(root, "continue").ClassListContains("hidden"));

            Click(root, "new-game");
            yield return null;
            var dialog = root.Q(className: "dialog");
            Assert.IsNotNull(dialog, "Novo jogo com save precisa pedir confirmação.");
            yield return Print("menu_confirmar_novo_jogo");
            Assert.AreEqual(999, Services.State.money, "Não pode apagar antes de confirmar.");

            Click(dialog.Query<Button>().Last());
            yield return WaitForScene(SceneFlow.Hideout);
            Assert.AreEqual(Services.Config.startingMoney, Services.State.money);
        }

        [UnityTest]
        public IEnumerator Continue_GoesToHideout()
        {
            Services.StartNewGame();
            SceneManager.LoadScene(SceneFlow.MainMenu);
            yield return WaitForScene(SceneFlow.MainMenu);
            Click(Root<MainMenuScreen>(), "continue");
            yield return WaitForScene(SceneFlow.Hideout);
        }
    }
}
