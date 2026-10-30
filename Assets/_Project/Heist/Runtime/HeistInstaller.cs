using AFirma.Core;
using UnityEngine;

namespace AFirma.Heist
{
    /// <summary>Registra o HeistRunner e o AutoHeistService no lugar dos mocks antes da primeira cena.</summary>
    static class HeistInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Install()
        {
            Services.Register((IHeistService)new HeistRunner());
            Services.Register((IAutoHeistService)new AutoHeistService());
        }
    }
}
