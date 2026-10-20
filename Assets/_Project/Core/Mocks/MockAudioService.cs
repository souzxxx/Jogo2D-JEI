using System.Collections.Generic;
using UnityEngine;

namespace AFirma.Core
{
    /// <summary>Áudio de mentira: toca SFX simples e só loga a música. O serviço real (Enzo) substitui este.</summary>
    public class MockAudioService : IAudioService
    {
        readonly Dictionary<string, float> volumes = new Dictionary<string, float>();
        MusicTrack current;

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            var position = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(clip, position, volume * GetVolume("sfx") * GetVolume("master"));
        }

        public void PlayMusic(MusicTrack track)
        {
            if (track == current) return;
            current = track;
            Debug.Log($"[Áudio mock] música: {track}");
        }

        public void SetHeistIntensity(float intensity01) { }

        public float GetVolume(string channel) => volumes.TryGetValue(channel, out var v) ? v : 1f;

        public void SetVolume(string channel, float volume01) => volumes[channel] = Mathf.Clamp01(volume01);
    }
}
