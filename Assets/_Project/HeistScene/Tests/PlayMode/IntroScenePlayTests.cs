using System;
using System.Collections;
using System.IO;
using AFirma.Core;
using AFirma.HeistScene;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AFirma.Tests
{
    /// <summary>
    /// A abertura no Banco Central contada pela Cena do Golpe: chuva de dinheiro no cofre, as duas placas na porta,
    /// o Bastos chegando, todo mundo algemado e a cena congelada antes do jornal. Usa o "Ver abertura" do menu,
    /// que não mexe no save.
    /// </summary>
    public class IntroScenePlayTests
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
            if (Services.Heist.IsRunning) Services.Heist.Flee();
            GameClock.SetPaused(false);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (IntroDirector.IsPlaying) IntroDirector.Skip();
            for (int i = 0; i < 300 && SceneManager.GetActiveScene().name == SceneFlow.Heist; i++) yield return null;
            yield return null;
        }

        static IEnumerator StartIntro(float speed)
        {
            IntroDirector.Replay();
            float timeout = 15f;
            while (timeout > 0f && !(IntroDirector.Current != null && IntroDirector.Current.IsRunning && Object.FindAnyObjectByType<HeistView>()?.Building != null))
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(IntroDirector.Current, "A abertura não começou.");
            IntroDirector.Current.Speed = speed;
        }

        static IEnumerator WaitIntro(float time, float timeout = 40f)
        {
            while (IntroDirector.Current != null && IntroDirector.Current.Time < time && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Intro_RainsMoney_ShowsTheSigns_BringsBastos_AndFreezesBeforeTheNewspaper()
        {
            yield return StartIntro(3f);
            Assert.Greater(SceneClock.IntroBoost, 1f, "As salas deviam cair rápido até a sirene.");

            // a chuva é rápida no 3x: confere no frame seguinte ao cofre abrir
            bool vaultOpened = false;
            System.Action<int, RoomData, double> onCleared = (i, r, l) => vaultOpened |= i == IntroHeistService.RoomsBeforeTheDoor - 1;
            GameEvents.RoomCleared += onCleared;
            float timeout = 30f;
            while (!vaultOpened && timeout > 0f) { timeout -= Time.unscaledDeltaTime; yield return null; }
            GameEvents.RoomCleared -= onCleared;
            Assert.IsTrue(vaultOpened, "O cofre não abriu.");
            Assert.IsNotNull(GameObject.Find("Cédula caindo"), "O cofre abriu sem chuva de dinheiro.");

            yield return WaitIntro(IntroHeistService.DoorAt + 0.5f);
            Assert.AreEqual(1f, SceneClock.IntroBoost, "Depois da sirene a cena volta pra velocidade normal.");
            Assert.IsNotNull(GameObject.Find("Placa: METER O PÉ"));
            Assert.IsNotNull(GameObject.Find("Placa: SÓ MAIS UMA"));

            yield return WaitIntro(IntroHeistService.ArrestAt - 2.5f);
            Assert.IsNotNull(GameObject.Find("Delegado Bastos"), "O Bastos não chegou.");
            Assert.IsNotNull(GameObject.Find("Viatura do Bastos"));

            yield return WaitIntro(IntroHeistService.ArrestAt + 0.5f);
            Assert.IsTrue(SceneClock.Frozen, "A cena devia congelar no flash, antes do jornal.");
            var view = Object.FindAnyObjectByType<HeistView>();
            foreach (var actor in view.Actors)
                Assert.IsTrue(actor.marker != null && actor.marker.enabled && actor.marker.sprite == view.art.handcuffsIcon,
                    $"{actor.Thief.DisplayName} devia estar algemado.");

            IntroDirector.Skip();
            yield return null;
            Assert.IsFalse(SceneClock.Frozen, "Acabou a abertura: a cena descongela.");
            Assert.AreEqual(1f, SceneClock.IntroBoost);
        }

        /// <summary>
        /// Fotos e quadros do GIF da abertura pro itch e pro GDD (não é teste de verdade).
        /// Saída na pasta da variável de ambiente AFIRMA_FOTOS.
        /// </summary>
        [UnityTest]
        public IEnumerator Fotos()
        {
            string dir = Environment.GetEnvironmentVariable("AFIRMA_FOTOS");
            if (string.IsNullOrEmpty(dir)) Assert.Ignore("Defina a variável AFIRMA_FOTOS com a pasta de saída pra gerar as fotos.");
            Directory.CreateDirectory(dir);
            yield return StartIntro(1f);
            float[] shots = { 6f, 15f, 19.9f, 22.5f, 25f, 26.5f, 28.8f, 30.6f };
            int frame = 0;
            int shot = 0;
            float nextFrame = 3f;
            while (IntroDirector.Current != null && IntroDirector.Current.Time < 31.5f)
            {
                float t = IntroDirector.Current.Time;
                if (shot < shots.Length && t >= shots[shot])
                    Capture(Path.Combine(dir, $"abertura_{shot++}.png"));
                if (t >= nextFrame)
                {
                    Capture(Path.Combine(dir, $"gif_abertura_{frame++:000}.png"), 640, 360);
                    nextFrame += 0.12f;
                }
                yield return null;
            }
        }

        static void Capture(string path, int width = 1280, int height = 720)
        {
            var cam = Camera.main;
            var rt = RenderTexture.GetTemporary(width, height, 24);
            var previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = active;
            cam.targetTexture = previous;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.Destroy(tex);
        }
    }
}
