using System.Linq;
using AFirma.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AFirma.HeistScene.EditorTools
{
    public static partial class HeistSceneBuilder
    {
        static void Step(string label)
        {
            if (Application.isBatchMode) Debug.Log("[Cena do Golpe] cena: " + label);
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        public const string HeistScenePath = "Assets/_Project/Scenes/Heist.unity";
        const string SceneRootName = "Cena do Golpe";
        const string FallbackHudName = "HUD provisória (apagar quando a HUD do Enzo entrar)";

        /// <summary>Monta a cena Heist: câmera Pixel Perfect seguindo a crew, fundo, clima do alerta, HeistView e HUD provisória.</summary>
        public static void BuildHeistScene()
        {
            var l = LoadLayout();
            var art = AssetDatabase.LoadAssetAtPath<HeistSceneArt>(Root + "/HeistSceneArt.asset");
            var scene = EditorSceneManager.OpenScene(HeistScenePath, OpenSceneMode.Single);

            Step("câmera");
            // câmera: 640x360 em pixel perfect, fundo noturno e pós-processamento (vinheta do alerta)
            var cam = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).First();
            cam.orthographic = true;
            cam.orthographicSize = 360f / 2f / Ppu;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.11f, 0.106f, 0.18f);
            var ppc = GetOrAdd<PixelPerfectCamera>(cam.gameObject);
            ppc.assetsPPU = 32;
            ppc.refResolutionX = 640;
            ppc.refResolutionY = 360;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.None;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var rig = GetOrAdd<HeistCameraRig>(cam.gameObject);
            rig.cam = cam;

            Step("luz da viatura");
            // luz de viatura entrando pela janela: presa na câmera, pisca vermelho/azul na sirene
            foreach (var old in new[] { "Luz da viatura (vermelha)", "Luz da viatura (azul)" })
            {
                var t = cam.transform.Find(old);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
            var red = PointLight(cam.transform, "Luz da viatura (vermelha)", new Vector3(-7f, 3f, 10f), new Color(1f, 0.15f, 0.2f), 12f, 1.2f);
            var blue = PointLight(cam.transform, "Luz da viatura (azul)", new Vector3(7f, 3f, 10f), new Color(0.2f, 0.4f, 1f), 12f, 1.2f);
            red.enabled = blue.enabled = false;

            Step("luz global");
            var global = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include).FirstOrDefault(x => x.lightType == Light2D.LightType.Global);

            Step("placeholder");
            // placeholder OnGUI do Core sai: a cena real chegou (a HUD provisória ocupa só uma faixa embaixo)
            foreach (var placeholder in Object.FindObjectsByType<ScenePlaceholder>(FindObjectsInactive.Include))
                Object.DestroyImmediate(placeholder.gameObject);

            var existing = GameObject.Find(SceneRootName);
            if (existing != null) Object.DestroyImmediate(existing);
            var root = new GameObject(SceneRootName);
            var separator = GameObject.Find("--- CENA DO GOLPE (Miguel) ---");
            if (separator != null) root.transform.SetSiblingIndex(separator.transform.GetSiblingIndex() + 1);

            Step("HeistView");
            var view = root.AddComponent<HeistView>();
            view.art = art;
            view.cameraRig = rig;
            view.vanPrefab = AssetDatabase.LoadAssetAtPath<VehicleView>(PrefabDir + "/Veiculos/Van.prefab");
            view.policePrefab = AssetDatabase.LoadAssetAtPath<VehicleView>(PrefabDir + "/Veiculos/Viatura.prefab");

            Step("fundo");
            var backdropGo = new GameObject("Fundo da cidade");
            backdropGo.transform.SetParent(root.transform, false);
            var backdrop = backdropGo.AddComponent<CityBackdrop>();
            backdrop.cam = cam;
            backdrop.sky = Sprite("Fundo/Ceu.png");
            backdrop.layers = l.backdrop.Select(b => new CityBackdrop.Layer
            {
                sprite = Sprite(b.sprite), parallax = b.parallax, yOffsetPx = b.y, order = b.order,
            }).ToArray();
            view.backdrop = backdrop;

            Step("clima");
            var climaGo = new GameObject("Clima do alerta");
            climaGo.transform.SetParent(root.transform, false);
            var volume = climaGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            var atmosphere = climaGo.AddComponent<AlertAtmosphere>();
            atmosphere.volume = volume;
            atmosphere.globalLight = global;
            atmosphere.policeRed = red;
            atmosphere.policeBlue = blue;
            view.atmosphere = atmosphere;

            Step("HUD");
            var hud = GameObject.Find(FallbackHudName);
            if (hud == null)
            {
                hud = new GameObject(FallbackHudName);
                var hudSeparator = GameObject.Find("--- HUD (prefab do Enzo) ---");
                if (hudSeparator != null) hud.transform.SetSiblingIndex(hudSeparator.transform.GetSiblingIndex() + 1);
            }
            if (hud.GetComponent<HeistSceneFallbackHud>() == null) hud.AddComponent<HeistSceneFallbackHud>();

            if (Object.FindObjectsByType<HeistSceneBootstrap>(FindObjectsInactive.Include).Length == 0)
                new GameObject("HeistSceneBootstrap").AddComponent<HeistSceneBootstrap>();

            Step("salvar");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Cena do Golpe] Cena Heist montada.");
        }
    }
}
