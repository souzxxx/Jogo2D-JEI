using System.Collections.Generic;
using System.IO;
using AFirma.Core;
using UnityEditor;
using UnityEngine;

namespace AFirma.EditorTools
{
    /// <summary>
    /// Liga a cena de cada bico (ESCOPO 6.7) pelos nomes dos arquivos, pra ninguém arrastar sprite na mão:
    /// ao lado do SideJob_X.asset, na pasta Bico/, os quadros &lt;id&gt;_0.png … &lt;id&gt;_7.png e a cena vazia &lt;id&gt;_vazio.png
    /// (ex.: Thieves/PeNaTabua/Bico/lotacao-pirata_0.png). Spec da arte no Thieves/LEIA-ME.md.
    /// Linha de comando: Unity -batchmode -quit -projectPath . -executeMethod AFirma.EditorTools.SideJobScenes.LinkAll
    /// </summary>
    public static class SideJobScenes
    {
        [MenuItem("A Firma/Bicos/Ligar as cenas dos bicos")]
        public static void LinkAll()
        {
            var db = Resources.Load<GameDatabase>(GameDatabase.ResourcePath);
            int linked = 0;
            foreach (var job in db.sideJobs)
                if (job != null && Link(job)) linked++;
            AssetDatabase.SaveAssets();
            Debug.Log($"[Bicos] {linked} cena(s) ligada(s).");
        }

        /// <summary>Preenche sceneFrames e sceneEmpty se achar os PNGs. Retorna se achou algum quadro.</summary>
        public static bool Link(SideJobDef job)
        {
            var folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(job)).Replace('\\', '/') + "/Bico";
            var frames = new List<Sprite>();
            for (int i = 0; i < SideJobDef.MaxSceneFrames; i++)
            {
                var frame = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{job.id}_{i}.png");
                if (frame == null) break;
                frames.Add(frame);
            }
            if (frames.Count == 0) return false;

            job.sceneFrames = frames.ToArray();
            job.sceneEmpty = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{job.id}_vazio.png");
            EditorUtility.SetDirty(job);
            return true;
        }
    }
}
