using System;
using System.Collections.Generic;

namespace AFirma.Core
{
    /// <summary>
    /// Versão simples da crew. O serviço real (Leonardo) substitui este.
    /// Falta aqui: efeitos completos das manhas, bônus de Fama no recrutamento.
    /// </summary>
    public class MockCrewService : ICrewService, ITickable
    {
        readonly Random rng = new Random();
        float releaseCheckTimer;

        static GameState S => Services.State;
        static GameDatabase Db => Services.Db;
        static GameConfig Cfg => Services.Config;

        public IReadOnlyList<ThiefState> Crew => S.crew;

        public int MaxCrewSize =>
            Cfg.startingCrewSlots + (int)Services.Economy.GetUpgradeTotal(UpgradeEffect.CrewSlots);

        public int HeistSlots =>
            Cfg.startingHeistSlots + (int)Services.Economy.GetUpgradeTotal(UpgradeEffect.HeistSlots);

        public IReadOnlyList<RecruitCandidate> Candidates
        {
            get
            {
                if (S.candidates.Count == 0 || GameClock.NowUnix >= S.candidatesRefreshAtUnix) GenerateCandidates();
                return S.candidates;
            }
        }

        public float SecondsToCandidatesRefresh => Math.Max(0, S.candidatesRefreshAtUnix - GameClock.NowUnix);

        public double PaidRefreshCost => Cfg.recruitBaseCost * 0.5;

        public IReadOnlyList<ThiefState> GetAvailable() => S.crew.FindAll(t => t.status == ThiefStatus.Available);

        StatModifiers Mods(ThiefState thief)
        {
            var trait = Db.GetTrait(thief.traitId);
            return trait != null ? trait.modifiers : null;
        }

        public float GetSkill(ThiefState thief)
        {
            var mods = Mods(thief);
            return thief.skill * (mods?.skillMult ?? 1f) * (1f + Services.Economy.GetUpgradeTotal(UpgradeEffect.SkillPercent) / 100f);
        }

        public float GetStealth(ThiefState thief)
        {
            var mods = Mods(thief);
            return thief.stealth * (mods?.stealthMult ?? 1f) * (1f + Services.Economy.GetUpgradeTotal(UpgradeEffect.StealthPercent) / 100f);
        }

        public float GetNerves(ThiefState thief) => thief.nerves * (Mods(thief)?.nervesMult ?? 1f);

        public int XpToNextLevel(ThiefState thief) => Formulas.XpToNextLevel(thief.level, Cfg.xpPerLevelFactor);

        public bool CanRecruit(RecruitCandidate candidate, out string reason)
        {
            if (S.crew.Count >= MaxCrewSize) { reason = "Sem vaga no esconderijo. Compre Quartos."; return false; }
            if (!Services.Economy.CanAfford(candidate.cost)) { reason = "Falta " + Formulas.Money(candidate.cost - Services.Economy.Money) + "."; return false; }
            reason = null;
            return true;
        }

        public bool Recruit(RecruitCandidate candidate)
        {
            if (!CanRecruit(candidate, out _)) return false;
            if (!Services.Economy.TrySpend(candidate.cost, "recrutar")) return false;
            S.candidates.Remove(candidate);
            candidate.thief.status = ThiefStatus.Available;
            S.crew.Add(candidate.thief);
            GameEvents.RaiseCandidatesChanged();
            GameEvents.RaiseCrewChanged();
            return true;
        }

        public bool CanFire(ThiefState thief, out string reason)
        {
            if (S.crew.Count <= 1) { reason = "A firma precisa de pelo menos 1 ladrão."; return false; }
            if (thief.status == ThiefStatus.OnHeist) { reason = "Está no meio de um golpe."; return false; }
            reason = null;
            return true;
        }

        public void Fire(ThiefState thief)
        {
            if (!CanFire(thief, out _)) return;
            S.crew.Remove(thief);
            GameEvents.RaiseCrewChanged();
        }

        public bool RefreshCandidates(bool paid)
        {
            if (paid && !Services.Economy.TrySpend(PaidRefreshCost, "trocar candidatos")) return false;
            GenerateCandidates();
            return true;
        }

        void GenerateCandidates()
        {
            S.candidates.Clear();
            int maxLevel = 1 + (int)Services.Economy.GetUpgradeTotal(UpgradeEffect.ContactsLevel);
            for (int i = 0; i < Cfg.recruitCandidates; i++)
            {
                var type = (ThiefType)rng.Next(4);
                int level = rng.Next(1, maxLevel + 1);
                S.candidates.Add(new RecruitCandidate
                {
                    thief = ThiefFactory.Create(Db, type, level, rng),
                    cost = Math.Round(Cfg.recruitBaseCost * Math.Pow(Cfg.recruitCostGrowthPerLevel, level - 1))
                });
            }
            S.candidatesRefreshAtUnix = GameClock.NowUnix + (long)Cfg.recruitRefreshSeconds;
            GameEvents.RaiseCandidatesChanged();
        }

        public void SetStatus(ThiefState thief, ThiefStatus status)
        {
            thief.status = status;
            if (status != ThiefStatus.OnSideJob) thief.sideJobId = null;
            GameEvents.RaiseCrewChanged();
        }

        public bool AddXp(ThiefState thief, int xp)
        {
            var def = Db.GetThiefType(thief.type);
            bool leveled = false;
            thief.xp += xp;
            while (thief.level < Cfg.maxLevel && thief.xp >= XpToNextLevel(thief))
            {
                thief.xp -= XpToNextLevel(thief);
                thief.level++;
                if (def != null)
                {
                    thief.skill += def.skillPerLevel;
                    thief.stealth += def.stealthPerLevel;
                    thief.nerves += def.nervesPerLevel;
                }
                leveled = true;
            }
            if (leveled) GameEvents.RaiseThiefLeveledUp(thief);
            return leveled;
        }

        float LawyerFactor => 1f - Services.Economy.GetUpgradeTotal(UpgradeEffect.LawyerPercent) / 100f;

        public void Jail(ThiefState thief, float seconds)
        {
            thief.status = ThiefStatus.Jailed;
            thief.sideJobId = null;
            thief.jailReleaseUnix = GameClock.NowUnix + (long)Math.Ceiling(seconds * Math.Max(0.1f, LawyerFactor));
            GameEvents.RaiseThiefJailed(thief);
            GameEvents.RaiseCrewChanged();
        }

        public float SecondsLeftInJail(ThiefState thief) =>
            thief.status == ThiefStatus.Jailed ? Math.Max(0, thief.jailReleaseUnix - GameClock.NowUnix) : 0;

        public double GetBailCost(ThiefState thief) =>
            Math.Ceiling(SecondsLeftInJail(thief) * Cfg.bailCostPerJailSecond * (Mods(thief)?.bailMult ?? 1f) * Math.Max(0.1f, LawyerFactor));

        public bool CanPayBail(ThiefState thief, out string reason)
        {
            if (thief.status != ThiefStatus.Jailed) { reason = "Não está preso."; return false; }
            var cost = GetBailCost(thief);
            if (!Services.Economy.CanAfford(cost)) { reason = "Falta " + Formulas.Money(cost - Services.Economy.Money) + "."; return false; }
            reason = null;
            return true;
        }

        public bool PayBail(ThiefState thief)
        {
            if (!CanPayBail(thief, out _)) return false;
            if (!Services.Economy.TrySpend(GetBailCost(thief), "fiança")) return false;
            Release(thief);
            return true;
        }

        void Release(ThiefState thief)
        {
            thief.status = ThiefStatus.Available;
            thief.jailReleaseUnix = 0;
            GameEvents.RaiseThiefReleased(thief);
            GameEvents.RaiseCrewChanged();
        }

        public ThiefState GrantFreeRookie(ThiefType type)
        {
            var rookie = ThiefFactory.Create(Db, type, 1, rng);
            S.crew.Add(rookie);
            GameEvents.RaiseCrewChanged();
            return rookie;
        }

        public void Tick(float deltaSeconds)
        {
            releaseCheckTimer += deltaSeconds;
            if (releaseCheckTimer < 1f) return;
            releaseCheckTimer = 0f;
            foreach (var thief in S.crew.ToArray())
                if (thief.status == ThiefStatus.Jailed && GameClock.NowUnix >= thief.jailReleaseUnix)
                    Release(thief);
        }
    }
}
