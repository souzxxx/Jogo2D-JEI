using System.Collections;
using System.Collections.Generic;
using AFirma.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AFirma.Tests
{
    /// <summary>
    /// Fumaça do fluxo inteiro com as cenas reais: Boot → Menu → Golpe → Esconderijo.
    /// Se este teste quebrar, o jogo não abre pra ninguém.
    /// </summary>
    public class SceneFlowSmokeTests
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
            // O GameLoop já inicializou com o save real; troca por um save em memória pra não sujar o PlayerPrefs.
            // Mantém os serviços que cada frente registrou: o teste tem que rodar com os reais, não com os mocks.
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
            yield return null;
        }

        [UnityTest]
        public IEnumerator Boot_GoesToMainMenu()
        {
            SceneManager.LoadScene(SceneFlow.Boot);
            yield return WaitForScene(SceneFlow.MainMenu);
            Assert.AreEqual(SceneFlow.MainMenu, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator Heist_FromPlanningToHideout()
        {
            Services.StartNewGame();
            var heist = Services.Db.GetHeist("loterica");
            var crew = new List<ThiefState>(Services.Crew.GetAvailable());
            HeistResult result = null;
            GameEvents.HeistFinished += r => result = r;

            SceneFlow.GoToHeist(heist, crew);
            yield return WaitForScene(SceneFlow.Heist);
            yield return null; // Start do HeistSceneBootstrap

            Assert.IsTrue(Services.Heist.IsRunning, "O golpe não começou ao carregar a cena Heist.");
            Services.Heist.Speed = 20f;

            float timeout = 30f;
            while (Services.Heist.IsRunning && timeout > 0f)
            {
                if (Services.Heist.Phase == HeistPhase.AwaitingDoor) Services.Heist.ChooseDoor(true);
                else if (Services.Heist.Phase == HeistPhase.AwaitingEvent) Services.Heist.ChooseEventOption(FirstAvailable());
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsNotNull(result, "O golpe não terminou.");
            Assert.Greater(Services.Economy.Money, 0);

            SceneFlow.GoToHideout();
            yield return WaitForScene(SceneFlow.Hideout);
            Assert.AreEqual(SceneFlow.Hideout, SceneManager.GetActiveScene().name);
        }

        static int FirstAvailable()
        {
            var choices = Services.Heist.PendingEvent.choices;
            for (int i = 0; i < choices.Count; i++)
                if (choices[i].available) return i;
            return 0;
        }

        static IEnumerator WaitForScene(string name, float timeout = 10f)
        {
            while (SceneManager.GetActiveScene().name != name && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
