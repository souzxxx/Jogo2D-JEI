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

        [UnityTest]
        public IEnumerator Pause_FreezesTheHeist()
        {
            var root = Root<HeistHud>();
            Click(root, "pause");
            Assert.IsTrue(GameClock.Paused);
            yield return Print("hud_pausa");
            Click(root, "resume");
            Assert.IsFalse(GameClock.Paused);
            Click(root, "flee");
            yield return null;
            Assert.IsFalse(Services.Heist.IsRunning, "Meter o pé termina o golpe.");
        }
    }
}
