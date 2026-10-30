using AFirma.Core;
using AFirma.HeistScene;
using AFirma.HeistScene.EditorTools;
using UnityEditor;
using UnityEngine;

namespace AFirma.Thieves.ConArtist.EditorTools
{
    /// <summary>
    /// 171: preenche a habilidade (ícone, som e VFX da Lábia), o som de ação do ThiefType e o ícone do bico.
    /// A arte vem de Art/Source~/gerar_icones_171.py e os sons de Audio/Source~/gerar_sons_171.py; o sprite, o retrato
    /// e o Animator continuam vindo do gerar_171.py. Mesmo padrão do Disquete (DisqueteBuilder).
    /// Linha de comando: Unity -batchmode -quit -projectPath . -executeMethod AFirma.Thieves.ConArtist.EditorTools.ConArtistBuilder.Build
    /// </summary>
    public static class ConArtistBuilder
    {
        const string Dir = "Assets/_Project/Thieves/171";

        [MenuItem("A Firma/171/Gerar (ícones, sons e VFX da Lábia)")]
        public static void Build()
        {
            AssetDatabase.Refresh();

            var type = AssetDatabase.LoadAssetAtPath<ThiefTypeDef>(Dir + "/ThiefType_171.asset");
            type.actionSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(Dir + "/Audio/SFX_171_Conversa.wav");
            EditorUtility.SetDirty(type);

            var ability = AssetDatabase.LoadAssetAtPath<ThiefAbilityDef>(Dir + "/Ability_171.asset");
            ability.icon = AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "/Icons/Icon_Labia.png");
            ability.sfx = AssetDatabase.LoadAssetAtPath<AudioClip>(Dir + "/Audio/SFX_Labia.wav");
            ability.vfxPrefab = BuildLabiaVfx();
            EditorUtility.SetDirty(ability);

            var job = AssetDatabase.LoadAssetAtPath<SideJobDef>(Dir + "/SideJob_171.asset");
            job.icon = AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "/Icons/Icon_BilhetePremiado.png");
            EditorUtility.SetDirty(job);

            AssetDatabase.SaveAssets();
            Debug.Log("[171] Ícones, sons e VFX da Lábia prontos.");
        }

        /// <summary>VFX da Lábia: o balão "BLÁ BLÁ" sobe da sala e solta os coraçõezinhos da conversa mole, flutuando pra cima.</summary>
        static GameObject BuildLabiaVfx()
        {
            var root = new GameObject("VFX_Labia");
            var balloon = new GameObject("Balao");
            balloon.transform.SetParent(root.transform, false);
            balloon.transform.localPosition = HeistSceneBuilder.Px(-22, 50); // acima do balão "Lábia!" da cena, sem tampar o ícone da sala
            var sr = balloon.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "/VFX/Labia.png");
            sr.sortingOrder = SortOrder.Bubble; // é fala: fica na frente do anel e do ícone da sala, como os bordões
            var pop = balloon.AddComponent<PopIcon>();
            pop.life = 1.4f;
            pop.rise = 0.35f;

            HeistSceneBuilder.Burst(root.transform, "Charme", HeistSceneBuilder.Sprite("Icones/Faisca.png"), 14, 0.6f, 1.6f, 0.5f, 1f, -0.2f,
                                    new Color(0.77f, 0.27f, 0.56f), new Color(0.97f, 0.83f, 0.41f, 0f));

            HeistSceneBuilder.EnsureFolder(Dir + "/VFX");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Dir + "/VFX/VFX_Labia.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
