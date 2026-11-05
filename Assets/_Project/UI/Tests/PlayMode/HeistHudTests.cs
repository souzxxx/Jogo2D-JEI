using System.Collections;
using System.Linq;
using AFirma.Core;
using AFirma.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static AFirma.Tests.UiTestSupport;

namespace AFirma.Tests
{
    public class HeistHudTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            UseMemorySave();
            Services.StartNewGame();
            var lottery = Services.Progression.AllHeists.First(h => Services.Progression.IsUnlocked(h));
            SceneFlow.GoToHeist(lottery, Services.Crew.GetAvailable().Take(Services.Crew.HeistSlots).ToList());
            yield return WaitForScene(SceneFlow.Heist);
            yield return null;
        }

        /// <summary>Golpe inteiro só pela HUD: evento, porta, cofre e o jornal de volta pro esconderijo.</summary>
        [UnityTest]
        public IEnumerator WholeHeist_PlaysThroughTheHud()
        {
            Assert.IsNull(GameObject.Find("HUD provisória (apagar quando a HUD do Enzo entrar)"), "A HUD provisória sai da cena.");
            var root = Root<HeistHud>();
            var h = Services.Heist;
            Assert.IsTrue(h.IsRunning);
            Assert.AreEqual(h.CurrentCrew.Count, root.Query(className: "hud-thief").ToList().Count, "Um cartão por ladrão.");
            yield return Print("hud");

            bool printedDoor = false, printedEvent = false;
            float timeout = 60f;
            while (h.IsRunning && timeout > 0f)
            {
                var popup = root.Q("popup");
                if (h.Phase == HeistPhase.AwaitingDoor)
                {
                    Assert.IsFalse(popup.ClassListContains("hidden"), "Porta abre o popup.");
                    if (!printedDoor) { printedDoor = true; yield return Print("hud_porta"); }
                    Click(root, "next-room");
                }
                else if (h.Phase == HeistPhase.AwaitingEvent)
                {
                    var choice = popup.Query<Button>().Where(b => !b.ClassListContains("btn--off")).First();
                    Assert.IsNotNull(choice, "Evento tem pelo menos uma escolha disponível.");
                    if (!printedEvent) { printedEvent = true; yield return Print("hud_evento"); }
                    Click(choice);
                }
                else if (h.Phase == HeistPhase.Working) h.CompleteCurrentRoom();
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsFalse(h.IsRunning, "O golpe terminou.");

            yield return new WaitForSecondsRealtime(1f);
            Find<VisualElement>(root, "news");
            yield return Print("jornal");
            Click(root, "back");
            yield return WaitForScene(SceneFlow.Hideout);
        }

        /// <summary>Usar a habilidade pela HUD avisa o que ela fez, com o número antes e depois.</summary>
        [UnityTest]
        public IEnumerator Ability_ToastSaysWhatItDid()
        {
            var root = Root<HeistHud>();
            var h = Services.Heist;
            float timeout = 10f;
            while (h.Phase != HeistPhase.Working && timeout > 0f) // evento do tutorial
            {
                var choice = root.Q("popup").Query<Button>().Where(b => !b.ClassListContains("btn--off")).First();
                if (choice != null) Click(choice);
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            h.AddAlert(20f - h.Alert);
            var labia = root.Q("crew").Query<Button>().Where(b => b.text == "Lábia").First();
            Assert.IsNotNull(labia, "A crew inicial tem o 171.");

            Click(labia);
            yield return null;

            var toasts = root.Q("toasts").Query<Label>().ToList().Select(l => l.text).ToList();
            Assert.That(toasts, Has.Some.Contains("Lábia").And.Some.Contains("alerta 20 → 0"), string.Join(" | ", toasts));
        }

        /// <summary>Porta com a sirene tocando: a janela "A polícia tá chegando!" abre embaixo do "POLÍCIA EM", sem tampar a contagem.</summary>
        [UnityTest]
        public IEnumerator SirenDoor_PopupDoesNotCoverTheCountdown()
        {
            var root = Root<HeistHud>();
            var h = Services.Heist;
            float timeout = 10f;
            while (h.Phase != HeistPhase.AwaitingDoor && timeout > 0f)
            {
                if (h.Phase == HeistPhase.AwaitingEvent)
                {
                    var choice = root.Q("popup").Query<Button>().Where(b => !b.ClassListContains("btn--off")).First();
                    if (choice != null) Click(choice);
                }
                else if (h.Phase == HeistPhase.Working)
                {
                    if (!h.SirenActive) h.AddAlert(100f);
                    h.CompleteCurrentRoom();
                }
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.AreEqual(HeistPhase.AwaitingDoor, h.Phase);
            Assert.IsTrue(h.SirenActive);
            yield return null;
            yield return null; // a contagem aparece, faz layout e a janela desce no frame seguinte

            var countdown = root.Q("countdown");
            var popup = root.Q("popup");
            Assert.IsFalse(popup.ClassListContains("hidden"));
            Assert.GreaterOrEqual(popup.worldBound.yMin, countdown.worldBound.yMax,
                $"A janela da porta ({popup.worldBound}) tampa a contagem da polícia ({countdown.worldBound}).");
        }

        /// <summary>
        /// Bip da Dona Cida com o jornal aberto (a prisão da abertura, um golpe com gente presa): o pager não cai em cima da
        /// capa; o bip espera o jornal fechar e aparece no esconderijo.
        /// </summary>
        [UnityTest]
        public IEnumerator News_HoldsTheBipUntilTheHideout()
        {
            var root = Root<HeistHud>();
            Services.Heist.Flee();
            float timeout = 5f;
            while (root.Q("news") == null && timeout > 0f) { timeout -= Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(root.Q("news"), "O jornal não abriu.");

            const string text = "Bip de teste com o jornal aberto.";
            GameEvents.RaiseBip("Dona Cida", text);
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsNull(root.Q("pager"), "O bip apareceu em cima do jornal.");

            Click(root, "back");
            yield return WaitForScene(SceneFlow.Hideout);
            var hideout = Root<HideoutScreen>();
            for (int i = 0; i < 20; i++) // os bips de antes (tutorial) vão sendo clicados
            {
                yield return new WaitForSecondsRealtime(0.3f);
                var pager = hideout.Q<Button>("pager");
                if (pager == null) continue;
                if (pager.Query<Label>().Last().text == text) yield break;
                Click(pager);
            }
            Assert.Fail("O bip segurado durante o jornal não apareceu no esconderijo.");
        }

        [UnityTest]
        public IEnumerator Revenge_SpecialCoverThenCredits()
        {
            var root = Root<HeistHud>();
            var banco = Services.Db.intro.heist;
            Services.Heist.Flee();
            yield return null;
            root.Q("news")?.parent.RemoveFromHierarchy(); // o jornal do golpe de verdade

            GameEvents.RaiseHeistFinished(new HeistResult
            {
                heistId = banco.id, completed = true, outcome = HeistOutcome.CleanEscape,
                roomsCleared = banco.rooms.Count, totalRooms = banco.rooms.Count
            });
            yield return null;
            var news = root.Q("news");
            Assert.IsTrue(news.ClassListContains("news--special"), "Primeiro Banco Central limpo: capa especial.");
            Assert.AreEqual(Services.Db.intro.revengeHeadline, news.Q<Label>(className: "news__headline").text);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Print("jornal_revanche");

            Click(root, "back");
            yield return WaitForScene(SceneFlow.Hideout);
            yield return null;
            Assert.IsTrue(Root<HideoutScreen>().Query<Label>(className: "h2").ToList().Any(l => l.text == "Créditos"), "Depois da revanche, os créditos.");
        }

        [UnityTest]
        public IEnumerator Pause_FreezesTheHeist()
        {
            var root = Root<HeistHud>();
            Click(root, "pause");
            Assert.IsTrue(GameClock.Paused);
            yield return Print("hud_pausa");
            Click(root, "options");
            yield return null;
            Assert.IsNotNull(root.Q<Slider>("volume-sfx"), "Opções abrem por cima da pausa.");
            Click(root, "resume");
            Assert.IsFalse(GameClock.Paused);
            Click(root, "pause");
            Click(root, "give-up");
            yield return null;
            Assert.IsFalse(GameClock.Paused);
            Assert.IsFalse(Services.Heist.IsRunning, "Meter o pé pela pausa termina o golpe.");
        }
    }
}
