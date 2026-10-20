using System;

namespace AFirma.Core
{
    /// <summary>Pausa do jogo e relógio real (prisão, bicos offline e recrutas usam tempo real).</summary>
    public static class GameClock
    {
        public static bool Paused { get; private set; }

        /// <summary>Testes podem trocar a fonte de tempo.</summary>
        public static Func<long> UnixNowProvider = () => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static long NowUnix => UnixNowProvider();

        public static void SetPaused(bool paused)
        {
            if (Paused == paused) return;
            Paused = paused;
            GameEvents.RaisePauseChanged(paused);
        }

        public static void Reset()
        {
            Paused = false;
            UnixNowProvider = () => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
