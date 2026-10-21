using System;
using System.Collections.Generic;
using AFirma.Core;

namespace AFirma.Crew
{
    /// <summary>
    /// Crew real (Leonardo). Substitui o MockCrewService.
    ///
    /// Regras próprias: atributos efetivos (manha + Ferramentas/Tênis), XP e nível até o máximo, estados, novato grátis,
    /// recrutamento (Contatos, Fama Lenda e Reputação), demissão e vagas.
    /// Prisão e fiança em tempo real, e os estados e erros do save (ESCOPO 10).
    /// </summary>
    public class CrewService : ICrewService, ITickable
    {
        readonly Random rng;
        float releaseCheckTimer;

        static GameState S => Services.State;
        static GameDatabase Db => Services.Db;
        static GameConfig Cfg => Services.Config;

        public CrewService(Random rng = null)
        {
            this.rng = rng ?? new Random();
            GameEvents.GameLoaded += OnGameLoaded;
        }

        // ---------------- Estados e erros (ESCOPO 10) ----------------

        /// <summary>Arruma o que o save trouxe fora do lugar: pena cumprida com o jogo fechado, golpe ou bico que não existe mais.</summary>
        void OnGameLoaded()
        {
            if (S == null) return;
            ReleaseDue();
            foreach (var thief in S.crew)
            {
                bool inInterruptedHeist = S.hasActiveHeist && S.activeHeist.crewIds.Contains(thief.id); // o RecoverInterruptedHeist cuida desses
                if (thief.status == ThiefStatus.OnHeist && !inInterruptedHeist && !Services.Heist.IsRunning)
                    SetStatus(thief, ThiefStatus.Available);
                else if (thief.status == ThiefStatus.OnSideJob && !IsValidSideJob(thief.sideJobId))
                    SetStatus(thief, ThiefStatus.Available);
            }
        }

        static bool IsValidSideJob(string id)
        {
            var job = Db.GetSideJob(id);
            return job != null && job.requiresThief;
        }

        /// <summary>
        /// Anti dead-end: ninguém fora da cadeia e sem como recrutar (sem Cr$ ou sem vaga) → Dona Cida manda um novato.
        /// Roda no Tick, não no load, pra a tela já estar ouvindo o bip.
        /// </summary>
        void EnsureSomeoneCanWork()
        {
            if (S.crew.Exists(t => t.status != ThiefStatus.Jailed)) return;
            if (Services.Heist != null && Services.Heist.IsRunning) return;
            if (CanRecruitSomeone()) return;

            var type = Cfg.startingCrew.Count > 0 ? Cfg.startingCrew[0] : ThiefType.ConArtist;
            var rookie = GrantFreeRookie(type);
            GameEvents.RaiseBip(Cfg.mentorName, Cfg.freeRookieBip.Replace("{nome}", rookie.DisplayName));
        }

        bool CanRecruitSomeone()
        {
            if (S.crew.Count >= MaxCrewSize) return false;
            foreach (var candidate in Candidates)
                if (Services.Economy.CanAfford(candidate.cost)) return true;
            return false;
        }

        public IReadOnlyList<ThiefState> Crew => S.crew;

        public int MaxCrewSize =>
            Cfg.startingCrewSlots + (int)Services.Economy.GetUpgradeTotal(UpgradeEffect.CrewSlots);

        public int HeistSlots =>
            Cfg.startingHeistSlots + (int)Services.Economy.GetUpgradeTotal(UpgradeEffect.HeistSlots);

        // ---------------- Atributos e XP ----------------

        /// <summary>Só quem está Disponível. Quem está no bico também pode ir pro golpe (filtre a Crew por status).</summary>
        public IReadOnlyList<ThiefState> GetAvailable() => S.crew.FindAll(t => t.status == ThiefStatus.Available);

        /// <summary>Perícia × manha × (1 + Ferramentas%).</summary>
        public float GetSkill(ThiefState thief) =>
            thief.skill * (Mods(thief)?.skillMult ?? 1f) * (1f + Services.Economy.GetUpgradeTotal(UpgradeEffect.SkillPercent) / 100f);

        /// <summary>Discrição × manha × (1 + Tênis de molinha%).</summary>
        public float GetStealth(ThiefState thief) =>
            thief.stealth * (Mods(thief)?.stealthMult ?? 1f) * (1f + Services.Economy.GetUpgradeTotal(UpgradeEffect.StealthPercent) / 100f);

        /// <summary>Nervos × manha. Nenhum upgrade mexe em Nervos.</summary>
        public float GetNerves(ThiefState thief) => thief.nerves * (Mods(thief)?.nervesMult ?? 1f);

        /// <summary>XP que falta juntar pro próximo nível (fator × nível). 0 no nível máximo.</summary>
        public int XpToNextLevel(ThiefState thief) =>
            thief.level >= Cfg.maxLevel ? 0 : Formulas.XpToNextLevel(thief.level, Cfg.xpPerLevelFactor);

        /// <summary>Soma XP e sobe quantos níveis der, com o ganho por nível do tipo. Um ThiefLeveledUp por chamada.</summary>
        public bool AddXp(ThiefState thief, int xp)
        {
            if (xp <= 0 || thief.level >= Cfg.maxLevel) return false;

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
            if (thief.level >= Cfg.maxLevel) thief.xp = 0; // no máximo não guarda XP
            if (leveled) GameEvents.RaiseThiefLeveledUp(thief);
            return leveled;
        }

        public void SetStatus(ThiefState thief, ThiefStatus status)
        {
            thief.status = status;
            if (status != ThiefStatus.OnSideJob) thief.sideJobId = null;
            GameEvents.RaiseCrewChanged();
        }

        /// <summary>Dona Cida manda um novato nível 1 de graça (anti dead-end: a firma nunca fica sem ladrão).</summary>
        public ThiefState GrantFreeRookie(ThiefType type)
        {
            var rookie = ThiefFactory.Create(Db, type, 1, rng);
            S.crew.Add(rookie);
            GameEvents.RaiseCrewChanged();
            return rookie;
        }

        StatModifiers Mods(ThiefState thief)
        {
            var trait = Db.GetTrait(thief.traitId);
            return trait != null ? trait.modifiers : null;
        }

        // ---------------- Recrutamento ----------------

        /// <summary>Candidatos do painel Contatos. Trocam sozinhos a cada recruitRefreshSeconds de tempo real.</summary>
        public IReadOnlyList<RecruitCandidate> Candidates
        {
            get
            {
                if (CandidatesExpired) GenerateCandidates();
                return S.candidates;
            }
        }

        // Recrutar os 3 não adianta a troca: a lista nova só vem no horário.
        bool CandidatesExpired => S.candidatesRefreshAtUnix == 0 || GameClock.NowUnix >= S.candidatesRefreshAtUnix;

        public float SecondsToCandidatesRefresh => Math.Max(0, S.candidatesRefreshAtUnix - GameClock.NowUnix);

        public double PaidRefreshCost => CrewRules.PaidRefreshCost(Services.Economy.GetUpgradeTotal(UpgradeEffect.ContactsLevel), Cfg);

        public bool CanRecruit(RecruitCandidate candidate, out string reason)
        {
            if (candidate == null || !S.candidates.Contains(candidate)) { reason = "Esse candidato já foi embora."; return false; }
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

        /// <summary>Nível sorteado de 1 a 1 + Contatos, mais os níveis da Fama Lenda (de graça). Preço pela Reputação.</summary>
        void GenerateCandidates()
        {
            S.candidates.Clear();
            int maxRolled = 1 + (int)Services.Economy.GetUpgradeTotal(UpgradeEffect.ContactsLevel);
            int bonusLevels = (int)Services.Progression.GetFamePerkTotal(FamePerkEffect.RecruitBonusLevels);
            float discount = Services.Progression.GetFamePerkTotal(FamePerkEffect.RecruitDiscountPercent);
            for (int i = 0; i < Cfg.recruitCandidates; i++)
            {
                var type = (ThiefType)rng.Next(4);
                int rolled = rng.Next(1, maxRolled + 1);
                S.candidates.Add(new RecruitCandidate
                {
                    thief = ThiefFactory.Create(Db, type, Math.Min(Cfg.maxLevel, rolled + bonusLevels), rng),
                    cost = CrewRules.RecruitCost(rolled, discount, Cfg)
                });
            }
            S.candidatesRefreshAtUnix = GameClock.NowUnix + (long)Cfg.recruitRefreshSeconds;
            GameEvents.RaiseCandidatesChanged();
        }

        // ---------------- Prisão e fiança ----------------

        static float LawyerPercent => Services.Economy.GetUpgradeTotal(UpgradeEffect.LawyerPercent);

        /// <summary>Prende pelo tempo real (corre com o jogo fechado). Já preso, fica com a pena mais longa.</summary>
        public void Jail(ThiefState thief, float seconds)
        {
            long release = GameClock.NowUnix + CrewRules.JailSeconds(seconds, LawyerPercent);
            if (thief.status == ThiefStatus.Jailed) release = Math.Max(release, thief.jailReleaseUnix);
            thief.status = ThiefStatus.Jailed;
            thief.sideJobId = null;
            thief.jailReleaseUnix = release;
            GameEvents.RaiseThiefJailed(thief);
            GameEvents.RaiseCrewChanged();
        }

        public float SecondsLeftInJail(ThiefState thief) =>
            thief.status == ThiefStatus.Jailed ? Math.Max(0, thief.jailReleaseUnix - GameClock.NowUnix) : 0;

        public double GetBailCost(ThiefState thief) =>
            CrewRules.BailCost(SecondsLeftInJail(thief), Mods(thief)?.bailMult ?? 1f, LawyerPercent, Cfg);

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

        /// <summary>Solta quem já cumpriu a pena, inclusive o tempo que passou com o jogo fechado.</summary>
        void ReleaseDue()
        {
            foreach (var thief in S.crew.ToArray())
                if (thief.status == ThiefStatus.Jailed && GameClock.NowUnix >= thief.jailReleaseUnix)
                    Release(thief);
        }

        public void Tick(float deltaSeconds)
        {
            releaseCheckTimer += deltaSeconds;
            if (releaseCheckTimer < 1f) return;
            releaseCheckTimer = 0f;
            if (CandidatesExpired) GenerateCandidates();
            ReleaseDue();
            EnsureSomeoneCanWork();
        }
    }
}
