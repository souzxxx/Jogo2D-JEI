using System.Collections.Generic;

namespace AFirma.Core
{
    /// <summary>Motor do Golpe. Dono: Leonardo. Mock: MockHeistService.</summary>
    public interface IHeistService : IHeistControl
    {
        bool IsRunning { get; }
        HeistDef CurrentHeist { get; }
        HeistContext Context { get; }
        IReadOnlyList<ThiefState> CurrentCrew { get; }
        int RoomIndex { get; }
        float RoomProgress01 { get; }
        float PoliceSecondsLeft { get; }
        /// <summary>1 = normal, GameConfig.speedFast = rápido.</summary>
        float Speed { get; set; }
        DoorDecision PendingDoor { get; }
        HeistEventInstance PendingEvent { get; }
        HeistResult LastResult { get; }

        bool CanStart(HeistDef heist, IReadOnlyList<ThiefState> crew, out string reason);
        void StartHeist(HeistDef heist, IReadOnlyList<ThiefState> crew);

        /// <summary>Resposta ao DoorDecisionRequested. true = próxima sala, false = meter o pé.</summary>
        void ChooseDoor(bool goToNextRoom);
        void ChooseEventOption(int choiceIndex);

        bool CanUseAbility(ThiefState thief, out string reason);
        void UseAbility(ThiefState thief);
        bool HasUsedAbility(ThiefState thief);

        /// <summary>Meter o pé agora (antes da sirene: limpo; durante a contagem: apressado).</summary>
        void Flee();

        /// <summary>Chamado ao carregar o jogo: resolve um golpe que ficou aberto quando o jogo fechou.</summary>
        HeistResult RecoverInterruptedHeist();
    }
}
