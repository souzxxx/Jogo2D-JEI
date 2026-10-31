using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace AFirma.HeistScene.EditorTools
{
    /// <summary>
    /// Monta a Cena do Golpe a partir da arte gerada por script (HeistScene/Art/Source~/gerar_cena.py e
    /// Thieves/Gazua/Art/Source~/gerar_gazua.py) e do layout.json:
    ///   1. importa e fatia os PNG (pixel art, PPU 32, pivô de cada tipo)
    ///   2. Gazua: animações, Animator, retrato, sons, ícones e VFX da habilidade
    ///   3. prefabs: salas modulares (uma por RoomKind), personagens, veículos e os 5 prédios (ligados nos HeistDef)
    ///   4. cena Heist: câmera Pixel Perfect, fundo da cidade, clima do alerta e HeistView (a HUD é o prefab do Enzo)
    /// Menu: A Firma > Cena do Golpe. Rodar de novo refaz tudo sem quebrar referência (GUIDs e nomes ficam).
    /// Linha de comando: Unity -batchmode -quit -projectPath . -executeMethod AFirma.HeistScene.EditorTools.HeistSceneBuilder.BuildAll
    /// </summary>
    public static partial class HeistSceneBuilder
    {
        public const string Root = "Assets/_Project/HeistScene";
        public const string ArtDir = Root + "/Art";
        public const string PrefabDir = Root + "/Prefabs";
        public const string GazuaDir = "Assets/_Project/Thieves/Gazua";
        const string LayoutFile = ArtDir + "/Source~/layout.json";
        const float Ppu = 32f;

        [MenuItem("A Firma/Cena do Golpe/Gerar tudo (arte, Gazua, prefabs e cena)")]
        public static void BuildAll()
        {
            ImportArt();
            BuildGazua();
            BuildPrefabs();
            BuildHeistScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[Cena do Golpe] Tudo gerado.");
        }

        [MenuItem("A Firma/Cena do Golpe/1. Importar e fatiar a arte")]
        public static void ImportArtMenu() => ImportArt();

        [MenuItem("A Firma/Cena do Golpe/2. Gazua (animações, dados e VFX)")]
        public static void BuildGazuaMenu()
        {
            BuildGazua();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("A Firma/Cena do Golpe/3. Prefabs das salas e prédios")]
        public static void BuildPrefabsMenu()
        {
            BuildPrefabs();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("A Firma/Cena do Golpe/4. Montar a cena Heist")]
        public static void BuildSceneMenu() => BuildHeistScene();

        // ---------------------------------------------------------------- layout.json

#pragma warning disable 0649
        [Serializable] public class Col { public float r, g, b, a; public Color Color => new Color(r, g, b, a <= 0 ? 1f : a); }
        [Serializable] public class Pt { public float x, y; }
        [Serializable] public class AnimInfo { public string name; public int frames; }
        [Serializable] public class StreetProp { public string sprite; public float x, y; public bool light; public int order; }
        [Serializable] public class GuardInfo { public string variant; public float minX, maxX; }
        [Serializable] public class CamInfo { public float x, y; public int facing; }
        [Serializable] public class LightInfo { public float x, y; public int[] color; public float radius, intensity; }
        [Serializable]
        public class RoomInfo
        {
            public int index, kind, obstacle;
            public string name, background, prop, door;
            public float x, propX, propY, workX, entryX, exitX, iconX, iconY, alarmX, alarmY;
            public bool backWall, dust;
            public GuardInfo[] guards;
            public CamInfo[] cameras;
            public LightInfo[] lights;
        }
        [Serializable]
        public class BuildingInfo
        {
            public string heistId, name, shell;
            public int tier;
            public Col ambient;
            public float ambientIntensity, shellX, shellY, width, streetY, cameraY;
            public bool underground;
            public Pt[] entryPath;
            public Pt van, police;
            public StreetProp[] props;
            public RoomInfo[] rooms;
        }
        [Serializable] public class DefaultRoom { public int kind; public string background, door; public Col lightColor; }
        [Serializable] public class NpcInfo { public string name, sheet; public AnimInfo[] anims; }
        [Serializable] public class PropInfo { public string name, closed, open; }
        [Serializable] public class DoorInfo { public string style, closed, open; }
        [Serializable] public class GlyphInfo { public string c; public int x, y, w; }
        [Serializable] public class GenericInfo { public string @base, cloth; public AnimInfo[] anims; }
        [Serializable] public class BackdropInfo { public string sprite; public float parallax, y; public int order; }
        [Serializable]
        public class Layout
        {
            public int ppu, roomW, roomH, wall, cellW, feet, fontCellH;
            public BuildingInfo[] buildings;
            public DefaultRoom[] defaults;
            public NpcInfo[] npcs;
            public PropInfo[] props;
            public DoorInfo[] doors;
            public GlyphInfo[] font;
            public GenericInfo genericThief;
            public BackdropInfo[] backdrop;
        }
#pragma warning restore 0649

        static Layout layout;

        public static Layout LoadLayout()
        {
            if (layout != null) return layout;
            var path = Path.Combine(Directory.GetCurrentDirectory(), LayoutFile);
            if (!File.Exists(path)) throw new FileNotFoundException("Rode o gerar_cena.py antes (falta o layout.json).", path);
            layout = JsonUtility.FromJson<Layout>(File.ReadAllText(path));
            return layout;
        }

        // ---------------------------------------------------------------- 1. Importação

        enum Kind { Single, Grid, Ring, Font }

        struct ImportRule
        {
            public Kind kind;
            public Vector2 pivot;
            public Vector4 border;
            public string[] anims;
            public int[] counts;
        }

        public static void ImportArt()
        {
            layout = null;
            var l = LoadLayout();
            AssetDatabase.Refresh();
            var paths = new List<string>();
            paths.AddRange(Directory.GetFiles(ArtDir, "*.png", SearchOption.AllDirectories));
            paths.AddRange(Directory.GetFiles(GazuaDir + "/Art", "*.png", SearchOption.TopDirectoryOnly));
            paths.AddRange(Directory.GetFiles(GazuaDir + "/Icons", "*.png", SearchOption.TopDirectoryOnly));
            paths.AddRange(Directory.GetFiles(GazuaDir + "/VFX", "*.png", SearchOption.TopDirectoryOnly));
            int count = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var raw in paths)
                {
                    var path = raw.Replace('\\', '/');
                    if (path.Contains("~/")) continue;
                    Configure(path, RuleFor(path, l), l);
                    count++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();
            Debug.Log($"[Cena do Golpe] {count} imagens importadas.");
        }

        static readonly string[] ThiefAnims = { "idle", "andar", "trabalhar", "comemorar" };
        static readonly string[] NpcAnimNames = { "idle", "andar", "alerta", "distraido" };
        static readonly string[] BastosAnims = { "idle", "andar", "apontar", "algemar" };
        static readonly int[] BastosCounts = { 2, 4, 2, 2 };

        static ImportRule RuleFor(string path, Layout l)
        {
            string file = Path.GetFileNameWithoutExtension(path);
            string dir = Path.GetFileName(Path.GetDirectoryName(path));
            var bottomCenter = new Vector2(0.5f, 0f);
            var bottomLeft = Vector2.zero;
            var center = new Vector2(0.5f, 0.5f);

            if (file == "Gazua_sheet")
                return new ImportRule { kind = Kind.Grid, pivot = bottomCenter, anims = ThiefAnims, counts = new[] { 4, 6, 6, 4 } };
            if (file.StartsWith("NPC_"))
            {
                var npc = l.npcs.First(n => "NPC_" + n.name == file);
                return new ImportRule { kind = Kind.Grid, pivot = bottomCenter, anims = npc.anims.Select(a => a.name).ToArray(), counts = npc.anims.Select(a => a.frames).ToArray() };
            }
            if (file.StartsWith("Ladrao_generico"))
                return new ImportRule { kind = Kind.Grid, pivot = bottomCenter, anims = l.genericThief.anims.Select(a => a.name).ToArray(), counts = l.genericThief.anims.Select(a => a.frames).ToArray() };
            if (file == "Bastos")
                return new ImportRule { kind = Kind.Grid, pivot = bottomCenter, anims = BastosAnims, counts = BastosCounts };
            if (file.StartsWith("Placa_")) return new ImportRule { kind = Kind.Single, pivot = new Vector2(0.5f, 1f) };
            if (file == "Anel") return new ImportRule { kind = Kind.Ring, pivot = center };
            if (file == "Fonte_pixel") return new ImportRule { kind = Kind.Font, pivot = bottomLeft };
            if (file == "Balao") return new ImportRule { kind = Kind.Single, pivot = center, border = new Vector4(4, 4, 4, 4) };
            if (file == "Balao_ponta") return new ImportRule { kind = Kind.Single, pivot = bottomCenter };
            if (file == "Cone") return new ImportRule { kind = Kind.Single, pivot = new Vector2(0f, 0.5f) };
            if (file == "Branco" || file == "Porta_destaque") return new ImportRule { kind = Kind.Single, pivot = bottomLeft };
            if (file == "Ceu") return new ImportRule { kind = Kind.Single, pivot = center };
            switch (dir)
            {
                case "Salas":
                case "Predios":
                case "Fundo":
                    return new ImportRule { kind = Kind.Single, pivot = bottomLeft };
                case "Aderecos":
                case "Rua":
                case "Personagens":
                    return new ImportRule { kind = Kind.Single, pivot = bottomCenter };
                case "Art" when file == "Gazua_retrato":
                    return new ImportRule { kind = Kind.Single, pivot = center };
                default:
                    return new ImportRule { kind = Kind.Single, pivot = center };
            }
        }

        static void Configure(string path, ImportRule rule, Layout l)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = Ppu;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = rule.pivot;
            settings.spriteBorder = rule.border;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            if (rule.kind == Kind.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
                return;
            }

            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.GetSourceTextureWidthAndHeight(out int texW, out int texH);
            var rects = new List<(string name, Rect rect, Vector2 pivot)>();
            string baseName = Path.GetFileNameWithoutExtension(path);
            if (rule.kind == Kind.Grid)
            {
                for (int row = 0; row < rule.anims.Length; row++)
                    for (int i = 0; i < rule.counts[row]; i++)
                        rects.Add(($"{baseName}_{rule.anims[row]}_{i}", new Rect(i * 32, texH - (row + 1) * 32, 32, 32), rule.pivot));
            }
            else if (rule.kind == Kind.Ring)
            {
                int size = texH;
                for (int i = 0; i * size < texW; i++)
                    rects.Add(($"Anel_{i:00}", new Rect(i * size, 0, size, size), rule.pivot));
            }
            else if (rule.kind == Kind.Font)
            {
                foreach (var g in l.font)
                {
                    // o atlas foi desenhado com y pra cima: (x, y) é o canto de baixo da célula
                    int w = Mathf.Max(1, g.w);
                    rects.Add(($"glifo_{(int)g.c[0]:X4}", new Rect(g.x, g.y, w, l.fontCellH), Vector2.zero));
                }
            }
            SetSpriteRects(importer, rects);
            importer.SaveAndReimport();
        }

        /// <summary>Define os sprites de um sheet mantendo o ID de quem já existia (as animações não perdem a referência).</summary>
        static void SetSpriteRects(TextureImporter importer, List<(string name, Rect rect, Vector2 pivot)> wanted)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existing = provider.GetSpriteRects().ToDictionary(r => r.name, r => r);
            var result = new List<SpriteRect>();
            foreach (var (name, rect, pivot) in wanted)
            {
                var sr = existing.TryGetValue(name, out var old) ? old : new SpriteRect { name = name, spriteID = GUID.Generate() };
                sr.rect = rect;
                sr.alignment = SpriteAlignment.Custom;
                sr.pivot = pivot;
                sr.border = Vector4.zero;
                result.Add(sr);
            }
            provider.SetSpriteRects(result.ToArray());
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names != null)
                names.SetNameFileIdPairs(result.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
        }

        // ---------------------------------------------------------------- utilidades

        public static Sprite Sprite(string artRelative)
        {
            if (string.IsNullOrEmpty(artRelative)) return null;
            var path = artRelative.StartsWith("Assets/") ? artRelative : ArtDir + "/" + artRelative;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning("[Cena do Golpe] Sprite não encontrado: " + path);
            return sprite;
        }

        public static Dictionary<string, Sprite> SheetSprites(string path)
        {
            var full = path.StartsWith("Assets/") ? path : ArtDir + "/" + path;
            return AssetDatabase.LoadAllAssetsAtPath(full).OfType<Sprite>().ToDictionary(s => s.name, s => s);
        }

        public static Sprite[] Frames(Dictionary<string, Sprite> sheet, string baseName, string anim, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                sheet.TryGetValue($"{baseName}_{anim}_{i}", out frames[i]);
            return frames;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static Vector3 Px(float x, float y) => new Vector3(x / Ppu, y / Ppu, 0f);

        public static Color Rgb(int[] c) => c == null || c.Length < 3 ? Color.white : new Color(c[0] / 255f, c[1] / 255f, c[2] / 255f);
    }
}
