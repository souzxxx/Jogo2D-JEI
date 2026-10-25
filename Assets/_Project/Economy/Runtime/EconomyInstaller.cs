using AFirma.Core;
using UnityEngine;

namespace AFirma.Economy
{
    /// <summary>Troca o MockEconomyService pela economia real antes do jogo inicializar os serviços.</summary>
    static class EconomyInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Install() => Services.Register((IEconomyService)new EconomyService());
    }
}
