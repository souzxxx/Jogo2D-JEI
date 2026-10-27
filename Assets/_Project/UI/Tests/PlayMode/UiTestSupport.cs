using System;
using System.Collections;
using System.IO;
using AFirma.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace AFirma.Tests
{
    /// <summary>Base dos testes de tela: save em memória, clique simulado e print opcional.</summary>
    public static class UiTestSupport
    {
        class InMemorySave : ISaveService
        {
            string json;
            public bool HasSave => json != null;
            public GameState Load(out string notice) { notice = null; return json == null ? null : JsonUtility.FromJson<GameState>(json); }
            public void Save(GameState state) => json = JsonUtility.ToJson(state);
            public void DeleteSave() => json = null;
        }

        /// <summary>Troca o save por um em memória mantendo os serviços reais de cada frente.</summary>
        public static void UseMemorySave()
        {
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
        }

        public static IEnumerator WaitForScene(string name, float timeout = 10f)
        {
            while (SceneManager.GetActiveScene().name != name && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.AreEqual(name, SceneManager.GetActiveScene().name, $"A cena {name} não carregou.");
            yield return null;
        }

        public static VisualElement Root<T>() where T : Component
        {
            var screen = UnityEngine.Object.FindAnyObjectByType<T>();
            Assert.IsNotNull(screen, typeof(T).Name + " não está na cena.");
            return screen.GetComponent<UIDocument>().rootVisualElement;
        }

        public static T Find<T>(VisualElement root, string name) where T : VisualElement
        {
            var element = root.Q<T>(name);
            Assert.IsNotNull(element, $"Não achei '{name}' na tela.");
            return element;
        }

        /// <summary>Clique como o do jogador (passa pelo Clickable do botão).</summary>
        public static void Click(Button button)
        {
            using var submit = NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
        }

        public static void Click(VisualElement root, string name) => Click(Find<Button>(root, name));

        /// <summary>
        /// Print da tela em 1280x720 (cena + UI) quando a variável AFIRMA_PRINTS aponta pra uma pasta.
        /// Usado pra conferir o visual rodando os testes em batchmode, onde não existe janela.
        /// </summary>
        public static IEnumerator Print(string name)
        {
            var dir = Environment.GetEnvironmentVariable("AFIRMA_PRINTS");
            if (string.IsNullOrEmpty(dir)) yield break;
            yield return new WaitForSecondsRealtime(0.3f); // deixa terminar o fade de entrada (Ui.FadeIn)

            const int w = 1280, h = 720;
            var documents = UnityEngine.Object.FindObjectsByType<UIDocument>();
            var camera = Camera.main;
            var sceneRt = new RenderTexture(w, h, 24);
            var uiRt = new RenderTexture(w, h, 24);
            var original = documents.Length > 0 ? documents[0].panelSettings : null;
            var offscreen = original != null ? UnityEngine.Object.Instantiate(original) : null;
            if (offscreen != null)
            {
                offscreen.targetTexture = uiRt;
                offscreen.clearColor = true;
                offscreen.colorClearValue = Color.clear;
                foreach (var d in documents) d.panelSettings = offscreen;
            }
            if (camera != null) camera.targetTexture = sceneRt;

            // WaitForEndOfFrame não dispara em batchmode; a câmera e o painel já desenham nas texturas a cada frame.
            for (int i = 0; i < 3; i++) yield return null;

            var scenePixels = Read(sceneRt, w, h);
            var uiPixels = Read(uiRt, w, h);
            for (int i = 0; i < scenePixels.Length; i++)
            {
                var ui = uiPixels[i];
                var bg = camera != null ? scenePixels[i] : Color.black;
                scenePixels[i] = new Color(ui.r + bg.r * (1 - ui.a), ui.g + bg.g * (1 - ui.a), ui.b + bg.b * (1 - ui.a), 1f);
            }
            var output = new Texture2D(w, h, TextureFormat.RGBA32, false);
            output.SetPixels(scenePixels);
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), output.EncodeToPNG());

            if (camera != null) camera.targetTexture = null;
            foreach (var d in documents) d.panelSettings = original;
            UnityEngine.Object.Destroy(offscreen);
            sceneRt.Release();
            uiRt.Release();
        }

        static Color[] Read(RenderTexture rt, int w, int h)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            RenderTexture.active = previous;
            return tex.GetPixels();
        }
    }
}
