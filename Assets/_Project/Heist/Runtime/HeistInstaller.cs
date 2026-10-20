using AFirma.Core;
using UnityEngine;

namespace AFirma.Heist
{
    /// <summary>Registra o HeistRunner no lugar do MockHeistService antes da primeira cena.</summary>
    static class HeistInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Install() => Services.Register((IHeistService)new HeistRunner());
    }
}
