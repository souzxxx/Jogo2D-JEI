using System.Linq;
using AFirma.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AFirma.HeistScene.EditorTools
{
    public static partial class HeistSceneBuilder
    {
        const string HideoutScenePath = "Assets/_Project/Scenes/Hideout.unity";
        const string HideoutLifeName = "Esconderijo vivo (Miguel)";

        [MenuItem("A Firma/Cena do Golpe/5. Esconderijo vivo")]
        public static void BuildHideoutLifeMenu() => BuildHideoutLife();

        /// <summary>
        /// Esconderijo vivo: liga a arte (Art/Esconderijo, gerada por gerar_esconderijo.py) e os quadros de cada ladrão
        /// (tirados do Animator do tipo) no HideoutLifeArt e põe o HideoutLife na cena Hideout.
        /// </summary>
        public static void BuildHideoutLife()
        {
            const string d = "Esconderijo/";
            var art = LoadOrCreate<HideoutLifeArt>(Root + "/HideoutLifeArt.asset");
            art.monitorOn = Sprite(d + "Monitor_ligado.png");
            art.monitorOff = Sprite(d + "Monitor_desligado.png");
            art.shelf = Sprite(d + "Prateleira.png");
            art.neonOn = Sprite(d + "Neon_aceso.png");
            art.neonOff = Sprite(d + "Neon_apagado.png");
            art.kombi = new[] { Sprite(d + "Kombi_1.png"), Sprite(d + "Kombi_2.png") };
            art.sleepingBags = Enumerable.Range(0, 5).Select(i => Sprite($"{d}Colchonete_{i}.png")).ToArray();
            art.shoeRacks = Enumerable.Range(1, 5).Select(i => Sprite($"{d}Sapateira_{i}.png")).ToArray();
            art.pegboard = Sprite(d + "Painel_ferramentas.png");
            art.tools = Enumerable.Range(0, 10).Select(i => Sprite($"{d}Ferramenta_{i}.png")).ToArray();
            art.diplomas = Enumerable.Range(1, 5).Select(i => Sprite($"{d}Diploma_{i}.png")).ToArray();
            art.phone = Sprite(d + "Telefone.png");
            art.rolodex = Enumerable.Range(1, 5).Select(i => Sprite($"{d}Agenda_{i}.png")).ToArray();
            art.washer = Enumerable.Range(0, 4).Select(i => Sprite($"{d}Maquina_{i}.png")).ToArray();
            art.radio = Enumerable.Range(0, 10).Select(i => Sprite($"{d}Radio_{i / 2 + 1}_{i % 2}.png")).ToArray();
            art.orelhao = Sprite(d + "Orelhao.png");
            art.orelhaoRinging = Sprite(d + "Orelhao_tocando.png");
            art.emCanaStamp = Sprite(d + "Carimbo_em_cana.png");
            art.note = Sprite("Icones/Cedula.png");

            art.crew = AssetDatabase.FindAssets("t:ThiefTypeDef")
                .Select(g => AssetDatabase.LoadAssetAtPath<ThiefTypeDef>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(t => t != null && t.animator != null)
                .Select(t => new HideoutLifeArt.CrewLook
                {
                    type = t.type,
                    idle = ClipFrames(t.animator, "idle"),
                    celebrate = ClipFrames(t.animator, "comemorar"),
                })
                .Where(c => c.idle.Length > 0)
                .ToArray();
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.OpenScene(HideoutScenePath, OpenSceneMode.Single);
            var old = GameObject.Find(HideoutLifeName);
            if (old != null) Object.DestroyImmediate(old);
            var go = new GameObject(HideoutLifeName);
            go.AddComponent<HideoutLife>().art = art;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Cena do Golpe] Esconderijo vivo pronto ({art.crew.Length} tipos de ladrão).");
        }

        /// <summary>Os sprites de um clipe do Animator do ladrão (o que termina em "_idle", "_comemorar"...).</summary>
        static Sprite[] ClipFrames(RuntimeAnimatorController controller, string anim)
        {
            var clip = controller.animationClips.FirstOrDefault(c => c != null && c.name.EndsWith("_" + anim));
            if (clip == null) return new Sprite[0];
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                if (binding.propertyName != "m_Sprite") continue;
                return AnimationUtility.GetObjectReferenceCurve(clip, binding)
                    .Select(k => k.value as Sprite).Where(s => s != null).ToArray();
            }
            return new Sprite[0];
        }
    }
}
