using System.Collections.Generic;
using System.Linq;
using AFirma.Core;
using AFirma.HeistScene;
using AFirma.HeistScene.EditorTools;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace AFirma.Thieves.PeNaTabua.EditorTools
{
    /// <summary>
    /// Pé-na-Tábua: fatia o sheet, cria clipes e Animator, preenche o ThiefType com retrato e Animator e monta o VFX do
    /// Cavalo de pau. A arte vem de Art/Source~/gerar_penatabua.py. Mesmo padrão do Disquete (DisqueteBuilder) e do Gazua.
    /// Linha de comando: Unity -batchmode -quit -projectPath . -executeMethod AFirma.Thieves.PeNaTabua.EditorTools.PeNaTabuaBuilder.Build
    /// </summary>
    public static class PeNaTabuaBuilder
    {
        const string Dir = "Assets/_Project/Thieves/PeNaTabua";
        const string Sheet = Dir + "/Art/PeNaTabua_sheet.png";

        // (estado, frames, fps, repete) no mesmo padrão do 171, do Gazua e do Disquete
        static readonly (string name, int count, float fps, bool loop)[] Clips =
        {
            ("idle", 4, 4f, true), ("andar", 6, 10f, true), ("trabalhar", 6, 8f, true), ("comemorar", 4, 8f, false),
        };

        [MenuItem("A Firma/Pé-na-Tábua/Gerar (Animator, retrato e VFX)")]
        public static void Build()
        {
            SliceSheet();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>().ToDictionary(s => s.name);

            var controllerPath = Dir + "/Art/PeNaTabua.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)
                             ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            for (int k = 0; k < Clips.Length; k++)
            {
                var (name, count, fps, loop) = Clips[k];
                var frames = Enumerable.Range(0, count).Select(i => sprites[$"PeNaTabua_sheet_{name}_{i}"]).ToArray();
                var clip = SpriteClip($"{Dir}/Art/PeNaTabua_{name}.anim", frames, fps, loop);
                var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name)
                            ?? machine.AddState(name, new Vector3(260f, 60f * k, 0f));
                state.motion = clip;
                if (name == "idle") machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);

            var type = AssetDatabase.LoadAssetAtPath<ThiefTypeDef>(Dir + "/ThiefType_PeNaTabua.asset");
            type.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "/Art/PeNaTabua_retrato.png");
            type.animator = controller;
            EditorUtility.SetDirty(type);

            var ability = AssetDatabase.LoadAssetAtPath<ThiefAbilityDef>(Dir + "/Ability_PeNaTabua.asset");
            ability.vfxPrefab = BuildCavaloDePauVfx();
            EditorUtility.SetDirty(ability);

            AssetDatabase.SaveAssets();
            Debug.Log("[Pé-na-Tábua] Animator, retrato e VFX prontos.");
        }

        /// <summary>VFX do Cavalo de pau: um "VRUUM!" de gibi, fumaça de pneu cantando e poeira.</summary>
        static GameObject BuildCavaloDePauVfx()
        {
            var root = new GameObject("VFX_CavaloDePau");
            var vruum = new GameObject("Vruum");
            vruum.transform.SetParent(root.transform, false);
            vruum.transform.localPosition = HeistSceneBuilder.Px(0, 18);
            var sr = vruum.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "/VFX/Vruum.png");
            sr.sortingOrder = SortOrder.Fx + 8;
            var pop = vruum.AddComponent<PopIcon>();
            pop.life = 1.1f;
            pop.rise = 0.35f;

            HeistSceneBuilder.Burst(root.transform, "Fumaça", HeistSceneBuilder.Sprite("Icones/Fumaca.png"), 14, 1f, 2.5f, 0.7f, 1.3f, -0.3f,
                                    new Color(0.9f, 0.9f, 0.93f, 0.85f), new Color(0.6f, 0.6f, 0.65f, 0f));
            HeistSceneBuilder.Burst(root.transform, "Poeira", HeistSceneBuilder.Sprite("Icones/Poeira.png"), 8, 0.5f, 1.5f, 0.4f, 0.8f, 1f,
                                    new Color(0.63f, 0.43f, 0.26f), new Color(0.44f, 0.29f, 0.18f, 0f));

            HeistSceneBuilder.EnsureFolder(Dir + "/VFX");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Dir + "/VFX/VFX_CavaloDePau.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Uma linha por animação, 32x32, pé no chão (pivô embaixo no meio). Mantém o ID de quem já existia.</summary>
        static void SliceSheet()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Sheet);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.GetSourceTextureWidthAndHeight(out _, out int height);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existing = provider.GetSpriteRects().ToDictionary(r => r.name);
            var rects = new List<SpriteRect>();
            for (int row = 0; row < Clips.Length; row++)
                for (int i = 0; i < Clips[row].count; i++)
                {
                    var name = $"PeNaTabua_sheet_{Clips[row].name}_{i}";
                    var rect = existing.TryGetValue(name, out var old) ? old : new SpriteRect { name = name, spriteID = GUID.Generate() };
                    rect.rect = new Rect(i * 32, height - (row + 1) * 32, 32, 32);
                    rect.alignment = SpriteAlignment.Custom;
                    rect.pivot = new Vector2(0.5f, 0f);
                    rects.Add(rect);
                }
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                ?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        /// <summary>Clipe que troca o sprite do SpriteRenderer.</summary>
        static AnimationClip SpriteClip(string path, Sprite[] frames, float fps, bool loop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool isNew = clip == null;
            if (isNew) clip = new AnimationClip();
            clip.frameRate = fps;
            var keys = frames.Select((f, i) => new ObjectReferenceKeyframe { time = i / fps, value = f }).ToList();
            // repetir um quadro no fim garante a duração certa do último frame
            keys.Add(new ObjectReferenceKeyframe { time = frames.Length / fps, value = loop ? frames[0] : frames[^1] });
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            if (isNew) AssetDatabase.CreateAsset(clip, path);
            else EditorUtility.SetDirty(clip);
            return clip;
        }
    }
}
