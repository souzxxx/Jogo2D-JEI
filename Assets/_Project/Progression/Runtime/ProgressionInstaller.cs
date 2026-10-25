using AFirma.Core;
using UnityEngine;

namespace AFirma.Progression
{
    /// <summary>Troca o MockProgressionService pela progressão real antes do jogo inicializar os serviços.</summary>
    static class ProgressionInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Install() => Services.Register((IProgressionService)new ProgressionService());
    }
}
