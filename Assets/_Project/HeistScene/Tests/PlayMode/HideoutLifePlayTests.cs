using System;
using System.Collections;
using System.IO;
using System.Linq;
using AFirma.Core;
using AFirma.HeistScene;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace AFirma.Tests
{
    /// <summary>
    /// Esconderijo vivo na cena Hideout de verdade: os bicos passam nos monitores e soltam "+Cr$", cada upgrade vira
    /// um objeto no porão, a crew disponível aparece, quem está preso vira foto no mural e comprar faz o objeto aparecer.
    /// </summary>
    public class HideoutLifePlayTests
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
            Services.StartNewGame();
            GameClock.SetPaused(false);
            yield return null;
        }

        /// <summary>Firma no meio do jogo: 3 de bobeira, 2 em bico, 1 preso e upgrades em vários níveis.</summary>
        static void MidGame()
        {
            var s = Services.State;
            s.crew.Clear();
            var rng = new System.Random(3);
            var types = new[] { ThiefType.Locksmith, ThiefType.ConArtist, ThiefType.Hacker, ThiefType.Driver, ThiefType.Locksmith, ThiefType.ConArtist };
            foreach (var type in types) s.crew.Add(ThiefFactory.Create(Services.Db, type, 4, rng, false));
            var jobs = Services.Db.sideJobs.Where(j => j.requiresThief).ToList();
            Services.Economy.AssignSideJob(s.crew[3], jobs[0]);
            Services.Economy.AssignSideJob(s.crew[4], jobs[1]);
            s.crew[5].status = ThiefStatus.Jailed;
            s.crew[5].jailReleaseUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 300;
            s.upgrades.Clear();
            foreach (var u in Services.Db.upgrades)
                if (u.effect != UpgradeEffect.ContactsLevel) // Contatos fica pra comprar no teste
                    s.upgrades.Add(new IdLevel { id = u.id, level = Mathf.Min(u.maxLevel, 3) });
            Services.Economy.Add(1_000_000, "teste");
        }

        static IEnumerator OpenHideout()
        {
            SceneFlow.GoToHideout();
            float timeout = 10f;
            while (timeout > 0f && (SceneManager.GetActiveScene().name != SceneFlow.Hideout || Object.FindAnyObjectByType<HideoutLife>()?.Layer == null))
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(Object.FindAnyObjectByType<HideoutLife>()?.Layer, "O Esconderijo vivo não montou a camada.");
            yield return new WaitForSecondsRealtime(0.6f);
        }

        [UnityTest]
        public IEnumerator Hideout_ShowsBicosUpgradesAndCrew_AndMoneyPopsFromTheMonitors()
        {
            MidGame();
            yield return OpenHideout();
            var life = Object.FindAnyObjectByType<HideoutLife>();
            var layer = life.Layer;

            Assert.AreEqual(Mathf.Min(5, Services.Db.sideJobs.Count), layer.Query<VisualElement>().ToList().Count(e => e.name != null && e.name.StartsWith("monitor: ")), "Um monitor por bico.");
            Assert.AreEqual(3, layer.Q("crew").childCount, "Só os 3 de bobeira ficam no porão.");
            Assert.IsNotNull(layer.Q("foto: " + Services.State.crew[5].DisplayName), "O preso vira foto no mural.");
            foreach (var u in Services.Db.upgrades.Where(u => u.effect != UpgradeEffect.ContactsLevel))
                Assert.Greater(layer.Q("upgrade: " + u.id).childCount, 0, u.displayName + " não apareceu no porão.");

            float timeout = 5f;
            while (life.FloatingCount == 0 && timeout > 0f) { timeout -= Time.unscaledDeltaTime; yield return null; }
            Assert.Greater(life.FloatingCount, 0, "Os bicos não soltaram \"+Cr$\".");
            yield return null;
            var pop = life.Floating.First();
            var bounds = pop.worldBound;
            Debug.Log($"[teste] +Cr$ '{pop.Q<Label>().text}' em {bounds}, opacidade {pop.resolvedStyle.opacity}, cor {pop.resolvedStyle.color}, camada {layer.worldBound}");
            Assert.Greater(bounds.width, 4f, "O \"+Cr$\" não tem tamanho.");
            Assert.IsTrue(layer.worldBound.Overlaps(bounds), "O \"+Cr$\" está fora da tela.");

            var contacts = Services.Db.upgrades.First(u => u.effect == UpgradeEffect.ContactsLevel);
            Assert.AreEqual(0, layer.Q("upgrade: " + contacts.id).childCount);
            Assert.IsTrue(Services.Economy.BuyUpgrade(contacts));
            yield return null;
            Assert.Greater(layer.Q("upgrade: " + contacts.id).childCount, 0, "Comprou e o telefone não apareceu.");
        }

        /// <summary>Foto do esconderijo vivo pro itch e pro GDD (não é teste de verdade). Saída em AFIRMA_FOTOS.</summary>
        [UnityTest]
        public IEnumerator Fotos()
        {
            string dir = Environment.GetEnvironmentVariable("AFIRMA_FOTOS");
            if (string.IsNullOrEmpty(dir)) Assert.Ignore("Defina a variável AFIRMA_FOTOS com a pasta de saída pra gerar as fotos.");
            Directory.CreateDirectory(dir);
            MidGame();
            yield return OpenHideout();
            var doc = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None).First(d => d.rootVisualElement.Q(className: "hideout") != null);
            var rt = new RenderTexture(1280, 720, 24);
            doc.panelSettings.targetTexture = rt;
            try
            {
                for (int i = 0; i < 12; i++)
                {
                    yield return new WaitForSecondsRealtime(i == 0 ? 1.2f : 0.25f);
                    yield return null;
                    var active = RenderTexture.active;
                    RenderTexture.active = rt;
                    var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                    tex.Apply();
                    RenderTexture.active = active;
                    File.WriteAllBytes(Path.Combine(dir, $"esconderijo_vivo_{i:00}.png"), tex.EncodeToPNG());
                    Object.Destroy(tex);
                }
            }
            finally
            {
                doc.panelSettings.targetTexture = null;
                rt.Release();
            }
        }
    }
}
