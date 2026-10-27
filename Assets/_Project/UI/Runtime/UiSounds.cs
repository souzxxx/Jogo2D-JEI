using AFirma.Core;
using UnityEngine;

namespace AFirma.UI
{
    /// <summary>Sons da interface (Resources/UiSons). Tocam pelo Services.Audio, no canal de SFX.</summary>
    [CreateAssetMenu(menuName = "A Firma/UI/Sons da interface", fileName = "UiSons")]
    public class UiSounds : ScriptableObject
    {
        public AudioClip click;
        public AudioClip hover;
        public AudioClip buy;
        public AudioClip error;
        public AudioClip bip;
        public AudioClip typewriter;
        public AudioClip stamp;
        public AudioClip paper;

        static UiSounds instance;
        public static UiSounds Get => instance != null ? instance : instance = Resources.Load<UiSounds>("UiSons");

        public static void Click() => Play(Get?.click, 0.6f);
        public static void Hover() => Play(Get?.hover, 0.25f);
        public static void Buy() => Play(Get?.buy);
        public static void Error() => Play(Get?.error, 0.7f);
        public static void Paper() => Play(Get?.paper, 0.5f);
        public static void Stamp() => Play(Get?.stamp);

        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (clip != null && Services.IsInitialized) Services.Audio.PlaySfx(clip, volume);
        }
    }
}
