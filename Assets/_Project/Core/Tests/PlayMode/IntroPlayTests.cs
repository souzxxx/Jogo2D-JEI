using System.Collections;
using System.Collections.Generic;
using AFirma.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AFirma.Tests
{
    /// <summary>Fluxo da abertura (ESCOPO 5 e 10): Novo jogo → Abertura → Esconderijo, Pular, até o fim e "Ver abertura".</summary>
    public class IntroPlayTests
    {
        class InMemorySave : ISaveService
        {
            string json;
            public bool HasSave => json != null;
            public GameState Load(out string notice) { notice = null; return json == null ? null : JsonUtility.FromJson<GameState>(json); }
            public void Save(GameState state) => json = JsonUtility.ToJson(state);
            public void DeleteSave() => json = null;
        }

        readonly List<string> bips = new List<string>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Services.UseSave(new InMemorySave());
            if (Services.Heist.IsRunning) Services.Heist.Flee();
            GameClock.SetPaused(false);
            bips.Clear();
            GameEvents.Bip += OnBip;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameEvents.Bip -= OnBip;
            if (IntroDirector.IsPlaying) IntroDirector.Skip();
            // espera a troca de cena do Skip acabar, pra não atropelar o próximo teste
            for (int i = 0; i < 300 && SceneManager.GetActiveScene().name == SceneFlow.Heist; i++) yield return null;
            yield return null;
        }

        void OnBip(string sender, string message) => bips.Add(message);

        static IEnumerator WaitForScene(string name, float timeout = 15f)
        {
            while (SceneManager.GetActiveScene().name != name && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.AreEqual(name, SceneManager.GetActiveScene().name, $"A cena {name} não carregou.");
            yield return null;
        }

        /// <summary>O save sem o que muda sozinho (hora do save e a marca da abertura).</summary>
        static string Snapshot()
        {
            var copy = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(Services.State));
            copy.lastSaveUnix = 0;
            copy.introSeen = false;
            copy.money = 0; // Catar latinha rende sozinho enquanto a abertura toca
            copy.seenTips.RemoveAll(t => t.StartsWith("dica:")); // o tutorial do esconderijo começa assim que ele abre
            copy.modifierRolls.Clear();
            copy.candidates.Clear();
            copy.candidatesRefreshAtUnix = 0;
            return JsonUtility.ToJson(copy);
        }

        static bool AnyMilestoneBip() =>
            Services.State.seenTips.Exists(t => t == "bip:primeira-sirene" || t == "bip:primeiro-golpe" || t == "bip:primeiro-preso" || t == "bip:jogo-novo");

        [UnityTest]
        public IEnumerator NewGame_PlaysTheIntro_SkipGoesToHideout_WithTheSaveIntact()
        {
            IntroDirector.NewGameWithIntro();
            yield return WaitForScene(SceneFlow.Heist);
            Assert.IsTrue(IntroDirector.IsPlaying);
            Assert.IsInstanceOf<IntroHeistService>(Services.Heist);
            Assert.IsTrue(Services.Heist.IsRunning, "A cena Heist não começou a abertura.");
            var before = Snapshot();

            yield return new WaitForSecondsRealtime(1f);
            IntroDirector.Skip();
            yield return WaitForScene(SceneFlow.Hideout);

            Assert.IsFalse(IntroDirector.IsPlaying);
            Assert.IsNotInstanceOf<IntroHeistService>(Services.Heist, "O motor de verdade tem que voltar.");
            Assert.IsTrue(Services.State.introSeen);
            Assert.AreEqual(before, Snapshot(), "Pular a abertura não mexe no save.");
            Assert.AreEqual(1, bips.FindAll(b => b == Services.Db.intro.secondChanceBip).Count, "Pulou, mas o bip da segunda chance vem.");
            Assert.IsFalse(AnyMilestoneBip(), "A abertura não gasta bip de marco.");
        }

        [UnityTest]
        public IEnumerator Intro_PlaysToTheEnd_AndLandsInTheHideout()
        {
            int sirens = 0;
            HeistResult result = null;
            System.Action<float> onSiren = s => sirens++;
            System.Action<HeistResult> onFinish = r => result = r;
            GameEvents.SirenStarted += onSiren;
            GameEvents.HeistFinished += onFinish;
            try
            {
                IntroDirector.NewGameWithIntro();
                yield return WaitForScene(SceneFlow.Heist);
                var before = Snapshot();
                Services.Heist.Speed = 8f; // o 2x da HUD, só que mais
                yield return WaitForScene(SceneFlow.Hideout, 30f);

                Assert.AreEqual(1, sirens);
                Assert.IsNotNull(result, "A abertura não chegou no jornal.");
                Assert.AreEqual(4, result.arrestedThiefIds.Count);
                Assert.AreEqual(before, Snapshot(), "Nem a prisão da firma antiga toca na sua crew.");
                Assert.IsTrue(Services.State.introSeen);
                Assert.IsFalse(AnyMilestoneBip(), "Nem a sirene nem a prisão da abertura contam como marco.");
                Assert.AreEqual(1, bips.FindAll(b => b == Services.Db.intro.secondChanceBip).Count);
            }
            finally
            {
                GameEvents.SirenStarted -= onSiren;
                GameEvents.HeistFinished -= onFinish;
            }
        }

        [UnityTest]
        public IEnumerator ReplayFromTheMenu_TouchesNothing_AndGoesBackToTheMenu()
        {
            Services.StartNewGame();
            var before = Snapshot();

            IntroDirector.Replay();
            yield return WaitForScene(SceneFlow.Heist);
            Assert.IsTrue(IntroDirector.IsReplay);
            IntroDirector.Skip();
            yield return WaitForScene(SceneFlow.MainMenu);

            Assert.IsFalse(Services.State.introSeen, "Rever não conta como ter visto no save.");
            Assert.AreEqual(before, Snapshot());
            Assert.IsFalse(bips.Contains(Services.Db.intro.secondChanceBip), "Rever não manda o bip da segunda chance.");
        }
    }
}
