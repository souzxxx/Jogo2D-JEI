using System.Collections.Generic;
using AFirma.Core;
using UnityEngine;

namespace AFirma.Audio
{
    /// <summary>
    /// Áudio real: música com crossfade, camadas de tensão no golpe e volumes salvos (PlayerPrefs afirma.volume.*).
    /// Cada faixa é Resources/Musica/&lt;MusicTrack&gt;; as camadas são &lt;MusicTrack&gt;_1 e _2, do mesmo tamanho,
    /// e entram conforme o alerta sobe. Sem AudioMixer: o WebGL não tem mixer, então o volume de cada canal
    /// (master, music, sfx) vai direto nas fontes.
    /// Escuta o golpe sozinho: começou → música do golpe, sirene → música da sirene, acabou → esconderijo.
    /// </summary>
    public class AudioService : IAudioService, ITickable
    {
        const string Prefix = "afirma.volume.";
        const float FadeSpeed = 1f / 1.5f;                       // crossfade de 1,5 s
        static readonly float[] LayerStart = { 0.35f, 0.7f };   // alerta (0..1) em que cada camada começa a entrar

        class Deck
        {
            public AudioSource[] sources;   // base + camadas
            public float level;             // 0..1 do crossfade
        }

        readonly Deck[] decks = { new Deck(), new Deck() };
        readonly Dictionary<string, float> volumes = new Dictionary<string, float>();
        int active;
        MusicTrack current;
        float intensity, smoothIntensity;
        AudioSource sfx;

        public AudioService()
        {
            GameEvents.HeistStarted += _ => PlayMusic(MusicTrack.Heist);
            GameEvents.SirenStarted += _ => PlayMusic(MusicTrack.Siren);
            GameEvents.HeistFinished += _ => PlayMusic(MusicTrack.Hideout);
            GameEvents.AlertChanged += alert => SetHeistIntensity(alert / 100f);
        }

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip == null || !Application.isPlaying) return;
            EnsureSources();
            sfx.PlayOneShot(clip, volume * GetVolume("sfx") * GetVolume("master"));
        }

        public void PlayMusic(MusicTrack track)
        {
            if (track == current || !Application.isPlaying) return;
            current = track;
            EnsureSources();
            active = 1 - active;
            var deck = decks[active];
            for (int i = 0; i < deck.sources.Length; i++)
            {
                var source = deck.sources[i];
                source.clip = track == MusicTrack.None ? null : Resources.Load<AudioClip>(i == 0 ? $"Musica/{track}" : $"Musica/{track}_{i}");
                source.volume = 0f;
                if (source.clip != null) source.Play();
                else source.Stop();
            }
            if (track != MusicTrack.Heist) intensity = smoothIntensity = 0f;
        }

        public void SetHeistIntensity(float intensity01) => intensity = Mathf.Clamp01(intensity01);

        public float GetVolume(string channel)
        {
            if (!volumes.TryGetValue(channel, out var volume))
                volumes[channel] = volume = PlayerPrefs.GetFloat(Prefix + channel, 1f);
            return volume;
        }

        public void SetVolume(string channel, float volume01)
        {
            volumes[channel] = Mathf.Clamp01(volume01);
            PlayerPrefs.SetFloat(Prefix + channel, volumes[channel]);
            PlayerPrefs.Save();
            Apply();
        }

        public void Tick(float deltaSeconds)
        {
            if (sfx == null) return;
            smoothIntensity = Mathf.MoveTowards(smoothIntensity, intensity, deltaSeconds * 0.5f);
            for (int d = 0; d < decks.Length; d++)
            {
                decks[d].level = Mathf.MoveTowards(decks[d].level, d == active ? 1f : 0f, deltaSeconds * FadeSpeed);
                if (d != active && decks[d].level == 0f)
                    foreach (var source in decks[d].sources) source.Stop();
            }
            Apply();
        }

        void Apply()
        {
            if (sfx == null) return;
            float music = GetVolume("music") * GetVolume("master");
            foreach (var deck in decks)
                for (int i = 0; i < deck.sources.Length; i++)
                {
                    float layer = i == 0 ? 1f : Mathf.Clamp01((smoothIntensity - LayerStart[i - 1]) / 0.15f);
                    deck.sources[i].volume = music * deck.level * layer;
                }
        }

        /// <summary>As fontes moram num objeto que sobrevive à troca de cena, criado no primeiro som.</summary>
        void EnsureSources()
        {
            if (sfx != null) return;
            var host = new GameObject("[Áudio]");
            Object.DontDestroyOnLoad(host);
            sfx = host.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            foreach (var deck in decks)
            {
                deck.level = 0f;
                deck.sources = new AudioSource[1 + LayerStart.Length];
                for (int i = 0; i < deck.sources.Length; i++)
                {
                    deck.sources[i] = host.AddComponent<AudioSource>();
                    deck.sources[i].playOnAwake = false;
                    deck.sources[i].loop = true;
                }
            }
        }
    }
}
