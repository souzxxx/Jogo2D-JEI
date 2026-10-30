using System;
using System.Collections;
using System.Collections.Generic;
using AFirma.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AFirma.Tests
{
    /// <summary>
    /// Golpes extras (ESCOPO 6.9) de ponta a ponta na cena Heist, ainda com o prédio provisório montado pelos RoomKind:
    /// dá pra jogar até o cofre, o cofre limpo vale como concluído e o extra nunca libera o prestígio.
    /// </summary>
    public class ExtraHeistsPlayTests
    {
        class InMemorySave : ISaveService
        {
            string json;
            public bool HasSave => json != null;
            public GameState Load(out string notice) { notice = null; return json == null ? null : JsonUtility.FromJson<GameState>(json); }
            public void Save(GameState state) => json = JsonUtility.ToJson(state);
            public void DeleteSave() => json = null;
        }

        static readonly string[] Extras = { "banca-do-bicho", "carro-forte", "museu" };

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // save em memória pra não sujar o PlayerPrefs; os serviços reais de cada frente continuam
            var economy = Services.Economy;
            var progression = Services.Progression;
            var crew = Services.Crew;
            var heist = Services.Heist;
            var audio = Services.Audio;
            Services.ResetAll();
            Services.Register(new InMemorySave());
            Services.Register(economy);
            Services.Register(progression);
            Services.Register(crew);
            Services.Register(heist);
            Services.Register(audio);
            Services.Initialize();
            if (Services.Heist.IsRunning) Services.Heist.Flee();
            Services.StartNewGame();
            Services.State.tutorialDone = true;
            GameClock.SetPaused(false);
            yield return null;
        }

        /// <summary>Um de cada tipo no nível 10 e a van cheia: a crew que limpa qualquer golpe.</summary>
        static List<ThiefState> FullCrew()
        {
            var s = Services.State;
            var van = Services.Db.GetUpgrade(UpgradeEffect.HeistSlots);
            s.upgrades.Add(new IdLevel { id = van.id, level = van.maxLevel });
            s.crew.Clear();
            var rng = new System.Random(7);
            foreach (ThiefType type in Enum.GetValues(typeof(ThiefType)))
                s.crew.Add(ThiefFactory.Create(Services.Db, type, 10, rng, false));
            return new List<ThiefState>(s.crew);
        }

        [UnityTest]
        public IEnumerator ExtraHeist_PlaysToTheVault_WithTheTemporaryBuilding([ValueSource(nameof(Extras))] string id)
        {
            var heist = Services.Db.GetHeist(id);
            Assert.IsNotNull(heist, id + " não está no GameDatabase.");
            Assert.IsTrue(heist.extra);
            Assert.IsFalse(Services.Progression.IsUnlocked(heist), "Extra começa bloqueado.");
            Services.Progression.MarkCompleted(heist.unlockedBy);
            Assert.IsTrue(Services.Progression.IsUnlocked(heist), "Extra libera junto com o golpe da escada que vem antes.");

            HeistResult result = null;
            Action<HeistResult> onFinish = r => result = r;
            GameEvents.HeistFinished += onFinish;
            try
            {
                SceneFlow.GoToHeist(heist, FullCrew());
                float timeout = 10f;
                while (timeout > 0f && !(Services.Heist.IsRunning && Services.Heist.CurrentHeist == heist))
                {
                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }
                Assert.IsTrue(Services.Heist.IsRunning, "O golpe não começou na cena Heist.");

                Services.Heist.Speed = 12f;
                timeout = 90f;
                while (Services.Heist.IsRunning && timeout > 0f)
                {
                    var h = Services.Heist;
                    if (h.Phase == HeistPhase.AwaitingDoor) h.ChooseDoor(true);
                    else if (h.Phase == HeistPhase.AwaitingEvent)
                    {
                        var choices = h.PendingEvent.choices;
                        for (int i = 0; i < choices.Count; i++)
                            if (choices[i].available) { h.ChooseEventOption(i); break; }
                    }
                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.IsNotNull(result, "O golpe não terminou.");
                Assert.AreEqual(heist.rooms.Count, result.totalRooms);
                if (result.completed)
                    Assert.IsTrue(Services.Progression.IsCompleted(heist));
                Assert.IsFalse(Services.Progression.CanPrestige(out _), "Golpe extra não libera o prestígio.");
                yield return new WaitForSecondsRealtime(0.5f); // a crew corre pra van e a cena segue sem erro
            }
            finally
            {
                GameEvents.HeistFinished -= onFinish;
            }
        }
    }
}
