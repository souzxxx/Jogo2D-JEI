using AFirma.Core;
using UnityEngine;

namespace AFirma.Audio
{
    /// <summary>Troca o MockAudioService pelo áudio real antes do jogo inicializar os serviços.</summary>
    static class AudioInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Install() => Services.Register((IAudioService)new AudioService());
    }
}
