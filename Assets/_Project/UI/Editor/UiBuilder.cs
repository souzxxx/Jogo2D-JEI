using System;
using AFirma.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace AFirma.UI.EditorTools
{
    /// <summary>
    /// Monta a interface a partir dos arquivos de texto (UXML/USS) e da arte gerada por script:
    /// PanelSettings com o tema, sons da UI, telas nas cenas MainMenu e Hideout, prefab da HUD na cena Heist.
    /// Menu: A Firma > Interface. Rodar de novo refaz tudo sem quebrar referência.
    /// Linha de comando: Unity -batchmode -quit -projectPath . -executeMethod AFirma.UI.EditorTools.UiBuilder.BuildAll
    /// </summary>
    public static class UiBuilder
    {
        public const string Root = "Assets/_Project/UI";
        public const string PanelSettingsPath = Root + "/Settings/PanelSettings.asset";
        const string Scenes = "Assets/_Project/Scenes";

        [MenuItem("A Firma/Interface/Gerar tudo (tema, telas e HUD)")]
        public static void BuildAll()
        {
            var settings = BuildPanelSettings();
            LoadOrCreate<UiSounds>(Root + "/Resources/UiSons.asset");
            SetupScene(Scenes + "/MainMenu.unity", "Menu principal", "MainMenu", typeof(MainMenuScreen), settings);
            SetupScene(Scenes + "/Hideout.unity", "Esconderijo", "Hideout", typeof(HideoutScreen), settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[Interface] Tudo gerado.");
        }

        /// <summary>Tela de referência 640x360 (a mesma da câmera Pixel Perfect): em 1280x720 cada pixel vira 2.</summary>
        static PanelSettings BuildPanelSettings()
        {
            var settings = LoadOrCreate<PanelSettings>(PanelSettingsPath);
            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(Root + "/Settings/Tema.tss");
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(640, 360);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 1f;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        /// <summary>Tira o placeholder OnGUI do Core e põe a tela real (UIDocument + script da tela).</summary>
        static void SetupScene(string scenePath, string objectName, string screen, Type component, PanelSettings settings)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (var placeholder in Object.FindObjectsByType<ScenePlaceholder>(FindObjectsInactive.Include))
                Object.DestroyImmediate(placeholder.gameObject);

            var go = GameObject.Find(objectName) ?? new GameObject(objectName);
            var document = go.GetComponent<UIDocument>() ?? go.AddComponent<UIDocument>();
            document.panelSettings = settings;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{Root}/Screens/{screen}.uxml");
            if (go.GetComponent(component) == null) go.AddComponent(component);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
