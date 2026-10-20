using System.Collections.Generic;

namespace AFirma.Core
{
    /// <summary>Crew: ladrões, recrutamento, prisão. Dono: Leonardo. Mock: MockCrewService.</summary>
    public interface ICrewService
    {
        IReadOnlyList<ThiefState> Crew { get; }
        int MaxCrewSize { get; }
        int HeistSlots { get; }
        IReadOnlyList<RecruitCandidate> Candidates { get; }
        float SecondsToCandidatesRefresh { get; }
        double PaidRefreshCost { get; }

        IReadOnlyList<ThiefState> GetAvailable();

        // Atributos efetivos (já com manha e upgrades)
        float GetSkill(ThiefState thief);
        float GetStealth(ThiefState thief);
        float GetNerves(ThiefState thief);
        int XpToNextLevel(ThiefState thief);

        bool CanRecruit(RecruitCandidate candidate, out string reason);
        bool Recruit(RecruitCandidate candidate);
        bool CanFire(ThiefState thief, out string reason);
        void Fire(ThiefState thief);
        bool RefreshCandidates(bool paid);

        void SetStatus(ThiefState thief, ThiefStatus status);
        /// <summary>Soma XP e sobe de nível se der. Retorna true se subiu.</summary>
        bool AddXp(ThiefState thief, int xp);

        void Jail(ThiefState thief, float seconds);
        float SecondsLeftInJail(ThiefState thief);
        double GetBailCost(ThiefState thief);
        bool CanPayBail(ThiefState thief, out string reason);
        bool PayBail(ThiefState thief);

        /// <summary>Dona Cida manda um novato de graça (anti dead-end).</summary>
        ThiefState GrantFreeRookie(ThiefType type);
    }
}
