using AFirma.Core;
using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Tempo da Cena do Golpe: congela na pausa do jogo e acelera junto com o golpe (1x/2x).
    /// Tudo que anda, pisca ou gira na cena usa isto, nunca Time.deltaTime direto.
    /// </summary>
    public static class SceneClock
    {
        /// <summary>Teto do frame: aba do WebGL voltando de segundo plano não teleporta ninguém.</summary>
        const float MaxFrame = 0.1f;

        public static bool Paused => GameClock.Paused;

        public static float Speed
        {
            get
            {
                var heist = Services.IsInitialized ? Services.Heist : null;
                return heist != null && heist.IsRunning ? Mathf.Max(0.1f, heist.Speed) : 1f;
            }
        }

        public static float Delta => Paused ? 0f : Mathf.Min(Time.unscaledDeltaTime, MaxFrame) * Speed;

        /// <summary>Tempo de interface: anda na velocidade normal, mas para na pausa (balões, ícones).</summary>
        public static float UiDelta => Paused ? 0f : Mathf.Min(Time.unscaledDeltaTime, MaxFrame);
    }

    /// <summary>Ordem de desenho da cena (todas na sorting layer Default).</summary>
    public static class SortOrder
    {
        public const int Sky = -100;
        public const int Backdrop = -90;
        public const int Shell = -30;
        public const int Street = -25;
        public const int Room = -20;
        public const int RoomDecor = -10;
        public const int Prop = -5;
        public const int Npc = 0;
        public const int Fog = 5;
        public const int Thief = 10;
        public const int Vehicle = 12;
        public const int Fx = 20;
        public const int Badge = 30;
        public const int Bubble = 40;
    }
}
