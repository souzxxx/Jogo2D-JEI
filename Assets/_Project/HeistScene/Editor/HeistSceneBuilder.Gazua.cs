using System.Collections.Generic;
using System.Linq;
using AFirma.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AFirma.HeistScene.EditorTools
{
    public static partial class HeistSceneBuilder
    {
        // (estado, fps, repete) no mesmo padrão do 171
        static readonly (string name, float fps, bool loop)[] GazuaClips =
        {
            ("idle", 4f, true), ("andar", 10f, true), ("trabalhar", 8f, true), ("comemorar", 8f, false),
        };

        /// <summary>Gazua: clipes e Animator do sheet, retrato, som de ação, ícones e VFX do Pé-de-cabra.</summary>
        public static void BuildGazua()
        {
            string art = GazuaDir + "/Art";
            var sheet = SheetSprites(art + "/Gazua_sheet.png");
            int[] counts = { 4, 6, 6, 4 };

            var controllerPath = art + "/Gazua.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            if (controller.layers.Length == 0) controller.AddLayer("Base Layer");
            var machine = controller.layers[0].stateMachine;

            for (int k = 0; k < GazuaClips.Length; k++)
            {
                var (name, fps, loop) = GazuaClips[k];
                var frames = Frames(sheet, "Gazua_sheet", name, counts[k]);
                var clip = SpriteClip(art + $"/Gazua_{name}.anim", frames, fps, loop);
                var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name)
                            ?? machine.AddState(name, new Vector3(260f, 60f * k, 0f));
                state.motion = clip;
                if (name == "idle") machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);

            var type = AssetDatabase.LoadAssetAtPath<ThiefTypeDef>(GazuaDir + "/ThiefType_Gazua.asset");
            type.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(art + "/Gazua_retrato.png");
            type.animator = controller;
            type.actionSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(GazuaDir + "/Audio/SFX_Gazua_Fechadura.wav");
            EditorUtility.SetDirty(type);

            var ability = AssetDatabase.LoadAssetAtPath<ThiefAbilityDef>(GazuaDir + "/Ability_Gazua.asset");
            ability.icon = AssetDatabase.LoadAssetAtPath<Sprite>(GazuaDir + "/Icons/Icon_PeDeCabra.png");
            ability.sfx = AssetDatabase.LoadAssetAtPath<AudioClip>(GazuaDir + "/Audio/SFX_PeDeCabra.wav");
            ability.vfxPrefab = BuildPeDeCabraVfx();
            EditorUtility.SetDirty(ability);

            var job = AssetDatabase.LoadAssetAtPath<SideJobDef>(GazuaDir + "/SideJob_Gazua.asset");
            job.icon = AssetDatabase.LoadAssetAtPath<Sprite>(GazuaDir + "/Icons/Icon_Chaveiro24h.png");
            EditorUtility.SetDirty(job);
            Debug.Log("[Cena do Golpe] Gazua pronto: Animator, retrato, sons, ícones e VFX.");
        }

        /// <summary>Clipe que troca o sprite do SpriteRenderer (mesma estrutura dos clipes do 171).</summary>
        static AnimationClip SpriteClip(string path, Sprite[] frames, float fps, bool loop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool isNew = clip == null;
            if (isNew) clip = new AnimationClip();
            clip.frameRate = fps;
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new List<ObjectReferenceKeyframe>();
            for (int i = 0; i < frames.Length; i++)
                keys.Add(new ObjectReferenceKeyframe { time = i / fps, value = frames[i] });
            // repetir o último quadro no fim garante a duração certa do último frame
            keys.Add(new ObjectReferenceKeyframe { time = frames.Length / fps, value = loop ? frames[0] : frames[frames.Length - 1] });
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            if (isNew) AssetDatabase.CreateAsset(clip, path);
            else EditorUtility.SetDirty(clip);
            return clip;
        }

        /// <summary>VFX do Pé-de-cabra: faíscas, lascas de madeira e um "CREC!" de gibi.</summary>
        static GameObject BuildPeDeCabraVfx()
        {
            var root = new GameObject("VFX_PeDeCabra");
            var crec = new GameObject("Crec");
            crec.transform.SetParent(root.transform, false);
            crec.transform.localPosition = Px(0, 18);
            var sr = crec.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GazuaDir + "/VFX/Crec.png");
            sr.sortingOrder = SortOrder.Fx + 8;
            var pop = crec.AddComponent<PopIcon>();
            pop.life = 1.1f;
            pop.rise = 0.35f;

            Burst(root.transform, "Faíscas", Sprite("Icones/Faisca.png"), 26, 2.5f, 5f, 0.35f, 0.7f, 2f,
                  new Color(0.97f, 0.83f, 0.41f), new Color(0.95f, 0.45f, 0.31f));
            Burst(root.transform, "Lascas", Sprite("Icones/Lasca.png"), 10, 1.5f, 3.5f, 0.6f, 1.1f, 4f,
                  new Color(0.63f, 0.43f, 0.26f), new Color(0.44f, 0.29f, 0.18f));
            Burst(root.transform, "Poeira", Sprite("Icones/Fumaca.png"), 6, 0.3f, 0.9f, 0.6f, 1.0f, -0.2f,
                  new Color(0.8f, 0.8f, 0.85f, 0.7f), new Color(0.6f, 0.6f, 0.65f, 0f));

            EnsureFolder(GazuaDir + "/VFX");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, GazuaDir + "/VFX/VFX_PeDeCabra.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
