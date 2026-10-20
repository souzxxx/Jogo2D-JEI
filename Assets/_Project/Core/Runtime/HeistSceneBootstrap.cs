using System.Collections.Generic;
using UnityEngine;

namespace AFirma.Core
{
    /// <summary>
    /// Fica na cena Heist. No Start (depois que todo mundo assinou os eventos no OnEnable) começa o golpe
    /// pedido pelo Planejamento. Se a cena for aberta direto no editor, começa um golpe de teste.
    /// </summary>
    public class HeistSceneBootstrap : MonoBehaviour
    {
        [Tooltip("Golpe usado quando a cena é aberta direto no editor. Vazio = o primeiro da lista.")]
        public HeistDef testHeist;

        void Start()
        {
            if (Services.Heist.IsRunning) return;

            if (!SceneFlow.TryConsumePendingHeist(out var heist, out var crew))
            {
                heist = testHeist != null ? testHeist : (Services.Db.heists.Count > 0 ? Services.Db.heists[0] : null);
                crew = new List<ThiefState>();
                foreach (var thief in Services.Crew.GetAvailable())
                    if (crew.Count < Services.Crew.HeistSlots) crew.Add(thief);
            }

            if (heist == null)
            {
                Debug.LogError("[A Firma] Nenhum golpe pra começar.");
                return;
            }

            if (Services.Heist.CanStart(heist, crew, out var reason))
                Services.Heist.StartHeist(heist, crew);
            else
                Debug.LogWarning($"[A Firma] Não deu pra começar {heist.displayName}: {reason}");
        }
    }
}
