namespace AFirma.Core
{
    /// <summary>
    /// O que habilidades e eventos podem fazer com o golpe em andamento.
    /// Implementado pelo serviço de golpe (real e mock).
    /// </summary>
    public interface IHeistControl
    {
        HeistPhase Phase { get; }
        float Alert { get; }
        bool SirenActive { get; }
        RoomData CurrentRoom { get; }
        double Bag { get; }

        void AddAlert(float delta);
        void FreezeAlert(float seconds);
        void CompleteCurrentRoom();
        void PerfectEscape();
        void AddBagPercent(float percent);
        void AddDelay(float seconds);
        void JailRandomThief();
    }
}
