using System.Collections;
using System.IO;
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
            yield return SkipIntroToHideout();
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
            Assert.GreaterOrEqual(Services.State.money, 999, "Não pode apagar antes de confirmar (a latinha rende uns trocados).");

            Click(dialog.Query<Button>().Last());
            yield return SkipIntroToHideout();
            Assert.Less(Services.State.money, 999, "O save antigo foi pro lixo (os bicos já podem ter rendido uns trocados).");
        }

        /// <summary>Novo jogo toca a abertura na cena Heist; pula e cai no esconderijo.</summary>
        static IEnumerator SkipIntroToHideout()
        {
            yield return WaitForScene(SceneFlow.Heist);
            Assert.IsTrue(IntroDirector.IsPlaying, "Novo jogo tem que tocar a abertura.");
            IntroDirector.Skip();
            yield return WaitForScene(SceneFlow.Hideout);
        }

        [UnityTest]
        public IEnumerator ReplayIntro_CaptionNewspaperAndBackToTheMenu()
        {
            SceneManager.LoadScene(SceneFlow.MainMenu);
            yield return WaitForScene(SceneFlow.MainMenu);
            Click(Root<MainMenuScreen>(), "replay-intro");
            yield return WaitForScene(SceneFlow.Heist);
            yield return new WaitForSecondsRealtime(1f);
            var root = Root<HeistHud>();
            Assert.IsFalse(Find<VisualElement>(root, "intro-caption").ClassListContains("hidden"), "Letreiro na tela preta.");
            StringAssert.StartsWith("Nova", Find<Label>(root, "intro-text").text, "O letreiro vai batendo à máquina.");
            Assert.IsFalse(Find<Button>(root, "intro-skip").ClassListContains("hidden"), "Pular na tela.");
            Assert.IsFalse(IntroDirector.ShowPlaceholderOverlay, "A HUD desliga o letreiro provisório.");
            yield return Print("abertura_letreiro");

            Services.Heist.Speed = 10f;
            for (float t = 0f; t < 10f && root.Q("news") == null; t += Time.unscaledDeltaTime) yield return null;
            Services.Heist.Speed = 1f;
            var news = root.Q("news");
            Assert.IsNotNull(news, "A abertura acaba na capa do jornal.");
            Assert.IsNotNull(news.Q(className: "news__photo"), "Capa com a foto do Bastos.");
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Print("abertura_jornal");

            Click(root, "back");
            yield return WaitForScene(SceneFlow.MainMenu);
            Assert.IsFalse(IntroDirector.IsPlaying, "Ver abertura volta pro menu.");
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

        [UnityTest]
        public IEnumerator Options_VolumeAndWipeSaveAskTwice()
        {
            Services.StartNewGame();
            Services.State.money = 99999;
            SceneManager.LoadScene(SceneFlow.MainMenu);
            yield return WaitForScene(SceneFlow.MainMenu);
            var root = Root<MainMenuScreen>();

            Click(root, "options");
            yield return null;
            float music = Services.Audio.GetVolume("music");
            Find<Slider>(root, "volume-music").value = 0.3f;
            Assert.AreEqual(0.3f, Services.Audio.GetVolume("music"), 0.001f, "O slider muda o volume da música.");
            yield return Print("opcoes");
            Services.Audio.SetVolume("music", music);

            Click(root, "delete-save");
            yield return null;
            Click(root.Query(className: "dialog").Last().Query<Button>().Last());
            yield return null;
            Assert.Greater(Services.State.money, 99000, "Uma confirmação só não apaga.");
            Click(root.Query(className: "dialog").Last().Query<Button>().Last());
            Assert.AreEqual(Services.Config.startingMoney, Services.State.money, "Na segunda confirmação o jogo volta do zero.");
            yield return null;
        }

        [Test]
        public void UiSounds_EveryClipIsSet()
        {
            foreach (var field in typeof(UiSounds).GetFields().Where(f => f.FieldType == typeof(AudioClip)))
                Assert.IsNotNull(field.GetValue(UiSounds.Get), $"UiSons sem o som \"{field.Name}\" (Audio/Source~/gerar_sfx.py).");
        }

        [UnityTest]
        public IEnumerator Credits_HaveEveryNameFromTheReadme()
        {
            SceneManager.LoadScene(SceneFlow.MainMenu);
            yield return WaitForScene(SceneFlow.MainMenu);
            var root = Root<MainMenuScreen>();
            Click(root, "credits");
            yield return null;
            var shown = string.Join("\n", root.Q(className: "credits").Query<Label>().ToList().Select(l => l.text));
            yield return Print("creditos");

            var lines = File.ReadAllLines(Path.Combine(Application.dataPath, "../README.md")).Select(l => l.Trim())
                .SkipWhile(l => l != "## Créditos").ToList();
            var team = lines[lines.IndexOf("### Equipe") + 1].Split('·').Select(n => n.Trim());
            var assets = lines.SkipWhile(l => l != "### Assets de terceiros").TakeWhile(l => !l.StartsWith("### Ferramentas"))
                .Where(l => l.StartsWith("| ") && !l.StartsWith("| Asset"))
                .Select(l => l.Split('|')[1].Trim())
                .Select(n => n.Contains(" (") ? n.Substring(0, n.IndexOf(" (")) : n);
            foreach (var name in team.Concat(assets))
                StringAssert.Contains(name, shown, $"\"{name}\" está no README mas falta na tela de Créditos (UI/Resources/Creditos.txt).");
        }
    }
}
