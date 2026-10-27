using System;
using System.Collections;
using System.Collections.Generic;
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
    /// Cena do Golpe com a cena Heist real e os serviços reais: os 5 prédios montam, a crew entra,
    /// as salas acendem e abrem, a viatura chega na sirene, o Pé-de-cabra solta o VFX e a crew volta pra van.
    /// </summary>
    public class HeistScenePlayTests
    {
        class InMemorySave : ISaveService
        {
            string json;
            public bool HasSave => json != null;
            public GameState Load(out string notice) { notice = null; return json == null ? null : JsonUtility.FromJson<GameState>(json); }
            public void Save(GameState state) => json = JsonUtility.ToJson(state);
            public void DeleteSave() => json = null;
        }

        static readonly string[] Heists = { "loterica", "joalheria", "cambio", "cassino", "banco-central" };

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
            if (Services.Heist.IsRunning) Services.Heist.Flee(); // golpe que sobrou do teste anterior
            Services.StartNewGame();
            GameClock.SetPaused(false);
            yield return null;
        }

        /// <summary>Crew completa (um de cada tipo, nível 10) e van com 4 vagas, pra todo golpe ir até o cofre.</summary>
        static List<ThiefState> FullCrew()
        {
            var s = Services.State;
            foreach (var h in Services.Db.heists)
                if (!s.unlockedHeists.Contains(h.id)) s.unlockedHeists.Add(h.id);
            var van = Services.Db.GetUpgrade(UpgradeEffect.HeistSlots);
            s.upgrades.Add(new IdLevel { id = van.id, level = 2 });
            s.crew.Clear();
            var rng = new System.Random(7);
            foreach (ThiefType type in Enum.GetValues(typeof(ThiefType)))
                s.crew.Add(ThiefFactory.Create(Services.Db, type, 10, rng, false));
            return new List<ThiefState>(s.crew);
        }

        static IEnumerator StartHeist(string id, List<ThiefState> crew)
        {
            if (Services.Heist.IsRunning) Services.Heist.Flee();
            SceneFlow.GoToHeist(Services.Db.GetHeist(id), crew);
            // espera a cena (re)carregar e o HeistSceneBootstrap começar este golpe
            float timeout = 10f;
            while (timeout > 0f && !(Services.Heist.IsRunning && Services.Heist.CurrentHeist.id == id))
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;
        }

        static void AutoDecide()
        {
            var h = Services.Heist;
            if (h.Phase == HeistPhase.AwaitingDoor) h.ChooseDoor(true);
            else if (h.Phase == HeistPhase.AwaitingEvent)
            {
                var choices = h.PendingEvent.choices;
                for (int i = 0; i < choices.Count; i++)
                    if (choices[i].available) { h.ChooseEventOption(i); break; }
            }
        }

        [UnityTest]
        public IEnumerator EveryHeist_IsToldByTheScene([ValueSource(nameof(Heists))] string id)
        {
            var crew = FullCrew();
            HeistResult result = null;
            Action<HeistResult> onFinish = r => result = r;
            GameEvents.HeistFinished += onFinish;
            try
            {
                yield return StartHeist(id, crew);
                var view = Object.FindAnyObjectByType<HeistView>();
                Assert.IsNotNull(view, "A cena Heist não tem HeistView.");
                Assert.IsNotNull(view.Building, "O prédio não foi montado.");
                StringAssert.StartsWith("Predio_" + id, view.Building.name, "Montou o prédio genérico em vez do prefab do golpe.");
                var heist = Services.Heist.CurrentHeist;
                Assert.AreEqual(heist.rooms.Count, view.Building.rooms.Count);
                Assert.AreEqual(crew.Count, view.Actors.Count, "Um ator por ladrão da crew.");

                Services.Heist.Speed = 12f;
                float timeout = 60f;
                while (Services.Heist.IsRunning && timeout > 0f)
                {
                    var room = view.Building.rooms[Services.Heist.RoomIndex];
                    Assert.AreNotEqual(RoomView.State.Hidden, room.Current, "A sala atual tem que estar acesa.");
                    AutoDecide();
                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }
                Assert.IsNotNull(result, "O golpe não terminou.");
                if (result.completed)
                    foreach (var room in view.Building.rooms)
                        Assert.AreEqual(RoomView.State.Cleared, room.Current, room.name + " não ficou limpa.");

                // depois do golpe a crew corre pra van e a cena segue sem erro
                yield return new WaitForSecondsRealtime(1f);
            }
            finally
            {
                GameEvents.HeistFinished -= onFinish;
            }
        }

        [UnityTest]
        public IEnumerator Siren_BringsThePoliceCar()
        {
            yield return StartHeist("loterica", FullCrew());
            Services.Heist.AddAlert(100f);
            yield return null;
            Assert.IsTrue(Services.Heist.SirenActive);
            Assert.AreEqual(2, Object.FindObjectsByType<VehicleView>(FindObjectsInactive.Exclude).Length, "Van e viatura.");
            var alarms = Object.FindObjectsByType<AlarmLight>(FindObjectsInactive.Exclude);
            Assert.IsNotEmpty(alarms);
            Assert.IsTrue(alarms[0].beam.enabled, "O giroflex liga na sirene.");
        }

        [UnityTest]
        public IEnumerator PeDeCabra_OpensTheLockRoomWithItsVfx()
        {
            var crew = FullCrew();
            yield return StartHeist("loterica", crew);
            var gazua = crew.Find(t => t.type == ThiefType.Locksmith);
            Services.Heist.Speed = 12f;
            float timeout = 30f;
            while (timeout > 0f && Services.Heist.IsRunning &&
                   !(Services.Heist.Phase == HeistPhase.Working && Services.Heist.CurrentRoom.obstacle == ObstacleType.Lock))
            {
                AutoDecide();
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(Services.Heist.IsRunning, "Não chegou na sala de Fechadura.");
            var view = Object.FindAnyObjectByType<HeistView>();
            var lockRoom = view.Building.rooms[Services.Heist.RoomIndex];

            Services.Heist.UseAbility(gazua);
            yield return null;

            Assert.IsNotNull(GameObject.Find("VFX_PeDeCabra(Clone)"), "O VFX do Pé-de-cabra não apareceu.");
            Assert.AreEqual(RoomView.State.Cleared, lockRoom.Current);
            Assert.AreEqual(lockRoom.Prop.open, lockRoom.Prop.sr.sprite, "O cofre tem que ficar aberto.");
        }

        [UnityTest]
        public IEnumerator CavaloDePau_ShowsItsVfxAndEndsPerfect()
        {
            var crew = FullCrew();
            yield return StartHeist("loterica", crew);
            var driver = crew.Find(t => t.type == ThiefType.Driver);
            float timeout = 10f;
            while (timeout > 0f && Services.Heist.Phase != HeistPhase.Working)  // resolve o evento do tutorial
            {
                AutoDecide();
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(Services.Heist.CanUseAbility(driver, out var why), why);

            Services.Heist.UseAbility(driver);
            yield return null;

            Assert.IsNotNull(GameObject.Find("VFX_CavaloDePau(Clone)"), "O VFX do Cavalo de pau não apareceu.");
            Assert.AreEqual(HeistOutcome.PerfectEscape, Services.Heist.LastResult.outcome);
        }

        // ---------------------------------------------------------------- fotos (só quando pedido)

        /// <summary>
        /// Tira as fotos e os quadros de GIF da cena pra página do itch e pro GDD. Não roda no Run All:
        /// Test Runner > PlayMode > HeistScenePlayTests > Fotos > Run Selected. Saída na pasta da variável de ambiente AFIRMA_FOTOS (ex.: Builds/Fotos).
        /// </summary>
        [UnityTest, Explicit("Gera fotos e quadros de GIF; rode pelo nome.")]
        public IEnumerator Fotos()
        {
            string dir = Environment.GetEnvironmentVariable("AFIRMA_FOTOS");
            if (string.IsNullOrEmpty(dir)) Assert.Ignore("Defina a variável AFIRMA_FOTOS com a pasta de saída pra gerar as fotos.");
            Directory.CreateDirectory(dir);

            foreach (var id in Heists)
            {
                var crew = FullCrew();
                yield return StartHeist(id, crew);
                Services.Heist.Speed = 1f;
                yield return new WaitForSecondsRealtime(id == "cassino" || id == "banco-central" ? 2.2f : 1.4f);
                Capture(Path.Combine(dir, $"{id}_1_chegada.png"));
                Services.Heist.Speed = 2f;
                yield return WaitUntil(() => Services.Heist.Phase == HeistPhase.Working && Services.Heist.RoomProgress01 > 0.45f, 20f, true);
                Capture(Path.Combine(dir, $"{id}_2_trabalhando.png"));
                yield return WaitUntil(() => Services.Heist.Phase == HeistPhase.AwaitingDoor, 30f, false);
                yield return new WaitForSecondsRealtime(0.9f);
                Capture(Path.Combine(dir, $"{id}_3_porta.png"));
                if (id == "cassino" || id == "joalheria")
                {
                    Services.Heist.ChooseDoor(true);
                    yield return new WaitForSecondsRealtime(1.5f);
                    Services.Heist.AddAlert(100f);
                    yield return new WaitForSecondsRealtime(1.2f);
                    Capture(Path.Combine(dir, $"{id}_4_sirene.png"));
                }
                Services.Heist.Flee();
                yield return new WaitForSecondsRealtime(1.0f);
                Capture(Path.Combine(dir, $"{id}_5_fuga.png"));
            }

            // quadros pro GIF: Lotérica do começo até a primeira porta, e a sirene no cassino
            yield return Frames(dir, "gif_loterica", "loterica", 1.5f, 9f, false);
            yield return Frames(dir, "gif_sirene", "cassino", 1.5f, 6f, true);
        }

        IEnumerator Frames(string dir, string prefix, string id, float speed, float seconds, bool siren)
        {
            var crew = FullCrew();
            yield return StartHeist(id, crew);
            Services.Heist.Speed = speed;
            if (siren)
            {
                yield return new WaitForSecondsRealtime(2.5f);
                Services.Heist.AddAlert(100f);
            }
            int frame = 0;
            float t = 0f;
            while (t < seconds)
            {
                AutoDecide();
                Capture(Path.Combine(dir, $"{prefix}_{frame++:000}.png"), 640, 360);
                yield return new WaitForSecondsRealtime(0.1f);
                t += 0.1f;
            }
        }

        static IEnumerator WaitUntil(Func<bool> condition, float timeout, bool decide)
        {
            while (!condition() && timeout > 0f && Services.Heist.IsRunning)
            {
                if (decide && Services.Heist.Phase == HeistPhase.AwaitingEvent) AutoDecide();
                timeout -= Time.unscaledDeltaTime;
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
