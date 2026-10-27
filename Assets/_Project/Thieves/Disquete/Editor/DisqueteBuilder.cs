using System.Collections.Generic;
using System.Linq;
using AFirma.Core;
using AFirma.HeistScene;
using AFirma.HeistScene.EditorTools;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace AFirma.Thieves.Disquete.EditorTools
{
    /// <summary>
    /// Disquete: fatia o sheet, cria clipes e Animator, preenche ThiefType, habilidade e bico com retrato, ícones e sons
    /// e monta o VFX do Loop na câmera. A arte vem de Art/Source~/gerar_disquete.py e os sons de Audio/Source~/gerar_sfx.py.
    /// Mesmo padrão do Gazua (HeistSceneBuilder.Gazua) e do Pé-na-Tábua.
    /// Linha de comando: Unity -batchmode -quit -projectPath . -executeMethod AFirma.Thieves.Disquete.EditorTools.DisqueteBuilder.Build
    /// </summary>
    public static class DisqueteBuilder
    {
        const string Dir = "Assets/_Project/Thieves/Disquete";
        const string Sheet = Dir + "/Art/Disquete_sheet.png";

        // (estado, frames, fps, repete) no mesmo padrão do 171 e do Gazua
        static readonly (string name, int count, float fps, bool loop)[] Clips =
        {
            ("idle", 4, 4f, true), ("andar", 6, 10f, true), ("trabalhar", 6, 8f, true), ("comemorar", 4, 8f, false),
        };

        [MenuItem("A Firma/Disquete/Gerar (Animator, retrato, ícones, sons e VFX)")]
        public static void Build()
        {
            SliceSheet();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>().ToDictionary(s => s.name);

            var controllerPath = Dir + "/Art/Disquete.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)
                             ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            for (int k = 0; k < Clips.Length; k++)
            {
                var (name, count, fps, loop) = Clips[k];
                var frames = Enumerable.Range(0, count).Select(i => sprites[$"Disquete_sheet_{name}_{i}"]).ToArray();
                var clip = SpriteClip($"{Dir}/Art/Disquete_{name}.anim", frames, fps, loop);
                var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name)
                            ?? machine.AddState(name, new Vector3(260f, 60f * k, 0f));
                state.motion = clip;
                if (name == "idle") machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);

            var type = AssetDatabase.LoadAssetAtPath<ThiefTypeDef>(Dir + "/ThiefType_Disquete.asset");
            type.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "/Art/Disquete_retrato.png");
            type.animator = controller;
            type.actionSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(Dir + "/Audio/SFX_Disquete_Teclado.wav");
            EditorUtility.SetDirty(type);

            var ability = AssetDatabase.LoadAssetAtPath<ThiefAbilityDef>(Dir + "/Ability_Disquete.asset");
            ability.icon = AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "/Icons/Icon_LoopNaCamera.png");
            ability.sfx = AssetDatabase.LoadAssetAtPath<AudioClip>(Dir + "/Audio/SFX_LoopNaCamera.wav");
            ability.vfxPrefab = BuildLoopVfx();
            EditorUtility.SetDirty(ability);

            var job = AssetDatabase.LoadAssetAtPath<SideJobDef>(Dir + "/SideJob_Disquete.asset");
            job.icon = AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "/Icons/Icon_Gatonet.png");
            EditorUtility.SetDirty(job);

            AssetDatabase.SaveAssets();
            Debug.Log("[Disquete] Animator, retrato, ícones, sons e VFX prontos.");
        }

        /// <summary>VFX do Loop na câmera: a telinha "LOOP" sobe da sala e solta faíscas verdes de chiado de vídeo.</summary>
        static GameObject BuildLoopVfx()
        {
            var root = new GameObject("VFX_LoopNaCamera");
            var screen = new GameObject("Loop");
            screen.transform.SetParent(root.transform, false);
            screen.transform.localPosition = HeistSceneBuilder.Px(0, 20);
            var sr = screen.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "/VFX/Loop.png");
            sr.sortingOrder = SortOrder.Fx + 8;
            var pop = screen.AddComponent<PopIcon>();
            pop.life = 1.3f;
            pop.rise = 0.3f;

            HeistSceneBuilder.Burst(root.transform, "Chiado", HeistSceneBuilder.Sprite("Icones/Faisca.png"), 18, 1f, 2.5f, 0.4f, 0.8f, 0f,
                                    new Color(0.71f, 0.86f, 0.55f), new Color(0.35f, 0.66f, 0.39f, 0f));

            HeistSceneBuilder.EnsureFolder(Dir + "/VFX");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Dir + "/VFX/VFX_LoopNaCamera.prefab");
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
                    var name = $"Disquete_sheet_{Clips[row].name}_{i}";
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
