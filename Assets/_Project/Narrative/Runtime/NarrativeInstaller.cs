using AFirma.Core;
using UnityEngine;

namespace AFirma.Narrative
{
    /// <summary>
    /// Sobe os bips da Dona Cida junto com o jogo e solta a fila todo frame.
    /// Enquanto a UI do bip (Enzo) não existe, todo bip também sai no Console.
    /// </summary>
    public class NarrativeInstaller : MonoBehaviour
    {
        static DonaCidaBips bips;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var texts = Resources.Load<BipSetDef>(BipSetDef.ResourcePath);
            if (texts == null)
            {
                Debug.LogWarning("[A Firma] Bips da Dona Cida não encontrados em Resources/" + BipSetDef.ResourcePath + ".");
                return;
            }
            bips = new DonaCidaBips(texts);
            // O GameLoop também sobe no BeforeSceneLoad, sem ordem garantida: se o jogo já carregou, confere o começo agora.
            if (Services.IsInitialized) bips.OnGameStarted();
            GameEvents.Bip += LogBip;

            var go = new GameObject("[Narrative]");
            go.AddComponent<NarrativeInstaller>();
            DontDestroyOnLoad(go);
        }

        static void LogBip(string sender, string message) => Debug.Log($"[Bip] {sender}: {message}");

        void Update() => bips?.Flush();
    }
}
