using System;
using System.Collections.Generic;

namespace AFirma.Core
{
    /// <summary>
    /// Tudo que vai pro save. Só tipos que o JsonUtility serializa (sem Dictionary, sem null em classes).
    /// Mudou o formato? Sobe o CurrentVersion e trata a migração no PlayerPrefsSaveService.
    /// </summary>
    [Serializable]
    public class GameState
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public long createdAtUnix;
        public long lastSaveUnix;

        // Economia
        public double money;
        public double cycleLoot;     // loot total do ciclo atual (base da Fama)
        public double lifetimeLoot;
        public List<IdLevel> upgrades = new List<IdLevel>();

        // Progressão
        public List<string> unlockedHeists = new List<string>();
        public List<string> completedHeists = new List<string>();
        public List<HeistModifierRoll> modifierRolls = new List<HeistModifierRoll>();
        public int fame;
        public int wantedLevel;
        public int prestigeCount;
        public List<IdLevel> famePerks = new List<IdLevel>();

        // Crew
        public List<ThiefState> crew = new List<ThiefState>();
        public List<RecruitCandidate> candidates = new List<RecruitCandidate>();
        public long candidatesRefreshAtUnix;

        // Golpe em andamento (pra recuperar se o jogo fechar no meio)
        public bool hasActiveHeist;
        public ActiveHeistRecord activeHeist = new ActiveHeistRecord();

        // Golpes no automático (ESCOPO 6.11). O do meio vira "meteu o pé" ao carregar.
        public List<AutoHeistRecord> autoHeists = new List<AutoHeistRecord>();

        // Tutorial e dicas
        public bool tutorialDone;
        public List<string> seenTips = new List<string>();

        public ThiefState FindThief(string thiefId) => crew.Find(t => t.id == thiefId);

        public static int GetLevel(List<IdLevel> list, string id)
        {
            var entry = list.Find(e => e.id == id);
            return entry != null ? entry.level : 0;
        }

        public static void SetLevel(List<IdLevel> list, string id, int level)
        {
            var entry = list.Find(e => e.id == id);
            if (entry == null) list.Add(new IdLevel { id = id, level = level });
            else entry.level = level;
        }

        /// <summary>Jogo novo: Dona Cida passa a firma com a crew inicial da config.</summary>
        public static GameState CreateNew(GameDatabase db, long nowUnix, Random rng)
        {
            var state = new GameState
            {
                createdAtUnix = nowUnix,
                lastSaveUnix = nowUnix,
                money = db.config != null ? db.config.startingMoney : 0
            };

            foreach (var heist in db.heists)
                if (heist != null && heist.unlockedByDefault)
                    state.unlockedHeists.Add(heist.id);

            if (db.config != null)
                foreach (var type in db.config.startingCrew)
                    state.crew.Add(ThiefFactory.Create(db, type, 1, rng, allowTrait: false));

            return state;
        }
    }

    [Serializable]
    public class ThiefState
    {
        public string id;
        public string firstName;
        public string nickname;
        public ThiefType type;
        public int level = 1;
        public int xp;
        public float skill;
        public float stealth;
        public float nerves;
        public string traitId;
        public ThiefStatus status;
        public string sideJobId;
        public long jailReleaseUnix;

        public string DisplayName => $"{firstName} {nickname}";
    }

    [Serializable]
    public class RecruitCandidate
    {
        public ThiefState thief = new ThiefState();
        public double cost;
    }

    [Serializable]
    public class IdLevel
    {
        public string id;
        public int level;
    }

    [Serializable]
    public class HeistModifierRoll
    {
        public string heistId;
        public string modifierId;
    }

    [Serializable]
    public class ActiveHeistRecord
    {
        public string heistId;
        public List<string> crewIds = new List<string>();
        public double bag;
    }

    [Serializable]
    public class AutoHeistRecord
    {
        public string heistId;
        public List<string> crewIds = new List<string>();
        public AutoHeistOrder order;
        public bool repeat;
        public double bag; // sacola do golpe em andamento (já com o multiplicador do automático)
    }
}
