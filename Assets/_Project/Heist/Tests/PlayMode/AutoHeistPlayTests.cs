using System.Collections;
using System.Collections.Generic;
using AFirma.Core;
using AFirma.Heist;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AFirma.Tests
{
    /// <summary>Golpe na mão e golpe no automático rodando juntos no GameLoop de verdade (ESCOPO 6.11).</summary>
    public class AutoHeistPlayTests
    {
        class InMemorySave : ISaveService
        {
            string json;
            public bool HasSave => json != null;
            public GameState Load(out string notice) { notice = null; return json == null ? null : JsonUtility.FromJson<GameState>(json); }
            public void Save(GameState state) => json = JsonUtility.ToJson(state);
            public void DeleteSave() => json = null;
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Services.UseSave(new InMemorySave());
            if (Services.Heist.IsRunning) Services.Heist.Flee(); // golpe que sobrou do teste anterior
            Services.StartNewGame();
            Services.State.tutorialDone = true;
            GameClock.SetPaused(false);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var run in new List<AutoHeistRun>(Services.AutoHeist.Running)) Services.AutoHeist.Recall(run);
            if (Services.Heist.IsRunning) Services.Heist.Flee();
            yield return null;
        }

        [UnityTest]
        public IEnumerator HandHeist_RunsWhileAnAutoHeistRunsInTheBackground()
        {
            Assert.IsInstanceOf<AutoHeistService>(Services.AutoHeist);
            var loterica = Services.Db.GetHeist("loterica");
            Services.Progression.MarkCompleted(loterica);
            var crew = Services.State.crew;
            var autoCrew = new List<ThiefState> { crew[0] };
            var handCrew = new List<ThiefState> { crew[1] };

            int handProgress = 0;
            GameEvents.RoomProgress += (i, p) => handProgress++;
            Assert.IsTrue(Services.AutoHeist.CanSend(loterica, autoCrew, out var reason), reason);
            Services.AutoHeist.Send(loterica, autoCrew, AutoHeistOrder.Quiet, repeat: true);
            int progressWithOnlyAuto = handProgress;
            Services.Heist.StartHeist(loterica, handCrew);

            // Evento sorteado pode travar a sala uns segundos (nos dois golpes): espera os dois andarem, com prazo.
            // O da mão a gente joga como a HUD jogaria; o automático se vira sozinho.
            var run = Services.AutoHeist.Running[0];
            for (float t = 0f; t < 20f && (handProgress == 0 || run.roomProgress01 + run.roomIndex + run.runsDone <= 0f); t += Time.unscaledDeltaTime)
            {
                var hand = Services.Heist;
                if (hand.Phase == HeistPhase.AwaitingEvent) hand.ChooseEventOption(hand.PendingEvent.choices.FindIndex(c => c.available));
                else if (hand.Phase == HeistPhase.AwaitingDoor) hand.ChooseDoor(true);
                yield return null;
            }

            Assert.IsTrue(Services.Heist.IsRunning || Services.Heist.LastResult != null, "O golpe na mão não rodou.");
            Assert.AreEqual(1, Services.AutoHeist.Running.Count, "O automático parou.");
            Assert.AreEqual(0, progressWithOnlyAuto, "O automático disparou evento do golpe na mão.");
            Assert.Greater(handProgress, 0, $"O golpe na mão não andou (pausado: {GameClock.Paused}).");
            Assert.Greater(run.roomProgress01 + run.roomIndex + run.runsDone, 0f,
                $"O GameLoop não está tocando o automático (fase do golpe na mão: {Services.Heist.Phase}, alerta {run.alert}).");
            Assert.AreEqual(ThiefStatus.OnHeist, crew[0].status);

            if (Services.Heist.IsRunning) Services.Heist.Flee();
            Services.AutoHeist.Recall(run);
            Assert.AreEqual(0, Services.AutoHeist.Running.Count);
            Assert.AreEqual(ThiefStatus.Available, crew[0].status);
        }
    }
}
