using UnityEngine;

namespace AFirma.Core
{
    /// <summary>Áudio. Dono: Enzo. Mock: MockAudioService.</summary>
    public interface IAudioService
    {
        void PlaySfx(AudioClip clip, float volume = 1f);
        void PlayMusic(MusicTrack track);
        /// <summary>0..1: intensidade da música do golpe (camadas por faixa de alerta).</summary>
        void SetHeistIntensity(float intensity01);
        float GetVolume(string channel);
        void SetVolume(string channel, float volume01);
    }

    /// <summary>Save. Implementação real no Core: PlayerPrefsSaveService.</summary>
    public interface ISaveService
    {
        bool HasSave { get; }
        /// <summary>Carrega o save. Se o principal estiver corrompido tenta o backup. notice explica o que aconteceu (ou null).</summary>
        GameState Load(out string notice);
        void Save(GameState state);
        void DeleteSave();
    }

    /// <summary>Serviços que precisam rodar todo frame implementam isto. O GameLoop chama.</summary>
    public interface ITickable
    {
        void Tick(float deltaSeconds);
    }
}
