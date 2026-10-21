using AFirma.Core;
using UnityEngine;

namespace AFirma.Crew
{
    /// <summary>Troca o MockCrewService pela crew real antes do jogo inicializar os serviços.</summary>
    static class CrewInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Install() => Services.Register((ICrewService)new CrewService());
    }
}
