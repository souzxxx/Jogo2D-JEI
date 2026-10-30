using System;

namespace AFirma.Core
{
    /// <summary>
    /// Canal único de eventos do jogo. As frentes conversam por aqui, sem se referenciar.
    /// Quem escuta: assina no OnEnable e cancela no OnDisable.
    /// Quem dispara: chama o Raise correspondente.
    /// </summary>
    public static class GameEvents
    {
        // ---------- Jogo ----------
        public static event Action GameLoaded;
        public static event Action NewGameStarted;
        public static event Action GameSaved;
        public static event Action<bool> PauseChanged;
        /// <summary>Aviso genérico pro jogador (ex.: "Save corrompido, carregamos o backup").</summary>
        public static event Action<string> Notice;

        // ---------- Economia (Nicolas) ----------
        public static event Action<double, double> MoneyChanged;          // (valor atual, variação)
        public static event Action<UpgradeDef, int> UpgradePurchased;     // (upgrade, novo nível)
        public static event Action SideJobsChanged;
        public static event Action<double, double> OfflineEarnings;       // (valor, segundos fora)

        // ---------- Progressão (Nicolas) ----------
        public static event Action<HeistDef> HeistUnlocked;
        public static event Action HeistModifiersRolled;
        public static event Action<int, int> Prestiged;                   // (fama ganha, nível de procurado)
        public static event Action<FamePerkDef, int> FamePerkPurchased;

        // ---------- Crew (Leonardo) ----------
        public static event Action CrewChanged;
        public static event Action CandidatesChanged;
        public static event Action<ThiefState> ThiefJailed;
        public static event Action<ThiefState> ThiefReleased;
        public static event Action<ThiefState> ThiefLeveledUp;

        // ---------- Golpe (Leonardo dispara; Miguel e Enzo escutam) ----------
        public static event Action<HeistDef> HeistStarted;
        public static event Action<int, RoomData> RoomEntered;            // (índice, sala)
        public static event Action<int, float> RoomProgress;              // (índice, 0..1)
        public static event Action<int, RoomData, double> RoomCleared;    // (índice, sala, loot ganho)
        public static event Action<float> AlertChanged;                   // 0..100
        public static event Action<double> BagChanged;
        public static event Action<DoorDecision> DoorDecisionRequested;
        public static event Action<bool> DoorDecisionMade;                // true = próxima sala
        public static event Action<HeistEventInstance> HeistEventTriggered;
        public static event Action<EventResolution> HeistEventResolved;
        public static event Action<ThiefState, ThiefAbilityDef> AbilityUsed;
        public static event Action<float> SirenStarted;                   // segundos da contagem
        public static event Action<float> PoliceCountdownTick;            // segundos restantes
        public static event Action<ThiefState, string> ThiefBark;         // balão de fala
        public static event Action<HeistResult> HeistFinished;

        // ---------- Golpes no automático (Leonardo dispara; Enzo mostra na faixa Golpes rolando) ----------
        public static event Action<AutoHeistRun> AutoHeistStarted;                // toda volta, inclusive as do Repetir
        public static event Action<AutoHeistRun> AutoHeistProgress;               // sala nova, sala limpa, sirene
        public static event Action<AutoHeistRun, HeistResult> AutoHeistFinished;  // fim de cada volta (mini manchete, +Cr$)
        public static event Action<AutoHeistRun, string> AutoHeistStopped;        // saiu da faixa (motivo, ou null se acabou normal)

        // ---------- Abertura (IntroDirector dispara; Enzo mostra o letreiro e o Pular) ----------
        public static event Action<bool> IntroStarted;                    // true = "Ver abertura" do menu
        public static event Action<string> IntroCaption;                  // letreiro na tela preta (null = some)
        public static event Action IntroFinished;

        // ---------- Narrativa (Nicolas dispara; Enzo mostra) ----------
        public static event Action<string, string> Bip;                   // (remetente, mensagem)
        public static event Action<string> TipRequested;                  // id da dica de primeira vez

        public static void RaiseGameLoaded() => GameLoaded?.Invoke();
        public static void RaiseNewGameStarted() => NewGameStarted?.Invoke();
        public static void RaiseGameSaved() => GameSaved?.Invoke();
        public static void RaisePauseChanged(bool paused) => PauseChanged?.Invoke(paused);
        public static void RaiseNotice(string message) => Notice?.Invoke(message);

        public static void RaiseMoneyChanged(double current, double delta) => MoneyChanged?.Invoke(current, delta);
        public static void RaiseUpgradePurchased(UpgradeDef upgrade, int level) => UpgradePurchased?.Invoke(upgrade, level);
        public static void RaiseSideJobsChanged() => SideJobsChanged?.Invoke();
        public static void RaiseOfflineEarnings(double amount, double secondsAway) => OfflineEarnings?.Invoke(amount, secondsAway);

        public static void RaiseHeistUnlocked(HeistDef heist) => HeistUnlocked?.Invoke(heist);
        public static void RaiseHeistModifiersRolled() => HeistModifiersRolled?.Invoke();
        public static void RaisePrestiged(int fameGained, int wantedLevel) => Prestiged?.Invoke(fameGained, wantedLevel);
        public static void RaiseFamePerkPurchased(FamePerkDef perk, int level) => FamePerkPurchased?.Invoke(perk, level);

        public static void RaiseCrewChanged() => CrewChanged?.Invoke();
        public static void RaiseCandidatesChanged() => CandidatesChanged?.Invoke();
        public static void RaiseThiefJailed(ThiefState thief) => ThiefJailed?.Invoke(thief);
        public static void RaiseThiefReleased(ThiefState thief) => ThiefReleased?.Invoke(thief);
        public static void RaiseThiefLeveledUp(ThiefState thief) => ThiefLeveledUp?.Invoke(thief);

        public static void RaiseHeistStarted(HeistDef heist) => HeistStarted?.Invoke(heist);
        public static void RaiseRoomEntered(int index, RoomData room) => RoomEntered?.Invoke(index, room);
        public static void RaiseRoomProgress(int index, float progress01) => RoomProgress?.Invoke(index, progress01);
        public static void RaiseRoomCleared(int index, RoomData room, double loot) => RoomCleared?.Invoke(index, room, loot);
        public static void RaiseAlertChanged(float alert) => AlertChanged?.Invoke(alert);
        public static void RaiseBagChanged(double bag) => BagChanged?.Invoke(bag);
        public static void RaiseDoorDecisionRequested(DoorDecision decision) => DoorDecisionRequested?.Invoke(decision);
        public static void RaiseDoorDecisionMade(bool goToNextRoom) => DoorDecisionMade?.Invoke(goToNextRoom);
        public static void RaiseHeistEventTriggered(HeistEventInstance instance) => HeistEventTriggered?.Invoke(instance);
        public static void RaiseHeistEventResolved(EventResolution resolution) => HeistEventResolved?.Invoke(resolution);
        public static void RaiseAbilityUsed(ThiefState thief, ThiefAbilityDef ability) => AbilityUsed?.Invoke(thief, ability);
        public static void RaiseSirenStarted(float countdownSeconds) => SirenStarted?.Invoke(countdownSeconds);
        public static void RaisePoliceCountdownTick(float secondsLeft) => PoliceCountdownTick?.Invoke(secondsLeft);
        public static void RaiseThiefBark(ThiefState thief, string line) => ThiefBark?.Invoke(thief, line);
        public static void RaiseHeistFinished(HeistResult result) => HeistFinished?.Invoke(result);

        public static void RaiseAutoHeistStarted(AutoHeistRun run) => AutoHeistStarted?.Invoke(run);
        public static void RaiseAutoHeistProgress(AutoHeistRun run) => AutoHeistProgress?.Invoke(run);
        public static void RaiseAutoHeistFinished(AutoHeistRun run, HeistResult result) => AutoHeistFinished?.Invoke(run, result);
        public static void RaiseAutoHeistStopped(AutoHeistRun run, string reason) => AutoHeistStopped?.Invoke(run, reason);

        public static void RaiseIntroStarted(bool replay) => IntroStarted?.Invoke(replay);
        public static void RaiseIntroCaption(string text) => IntroCaption?.Invoke(text);
        public static void RaiseIntroFinished() => IntroFinished?.Invoke();

        public static void RaiseBip(string sender, string message) => Bip?.Invoke(sender, message);
        public static void RaiseTipRequested(string tipId) => TipRequested?.Invoke(tipId);

        /// <summary>Limpa todos os assinantes (ao entrar em Play, por segurança com Domain Reload desligado).</summary>
        public static void ClearAll()
        {
            GameLoaded = null; NewGameStarted = null; GameSaved = null; PauseChanged = null; Notice = null;
            MoneyChanged = null; UpgradePurchased = null; SideJobsChanged = null; OfflineEarnings = null;
            HeistUnlocked = null; HeistModifiersRolled = null; Prestiged = null; FamePerkPurchased = null;
            CrewChanged = null; CandidatesChanged = null; ThiefJailed = null; ThiefReleased = null; ThiefLeveledUp = null;
            HeistStarted = null; RoomEntered = null; RoomProgress = null; RoomCleared = null; AlertChanged = null;
            BagChanged = null; DoorDecisionRequested = null; DoorDecisionMade = null; HeistEventTriggered = null;
            HeistEventResolved = null; AbilityUsed = null; SirenStarted = null; PoliceCountdownTick = null;
            ThiefBark = null; HeistFinished = null; Bip = null; TipRequested = null;
            AutoHeistStarted = null; AutoHeistProgress = null; AutoHeistFinished = null; AutoHeistStopped = null;
            IntroStarted = null; IntroCaption = null; IntroFinished = null;
        }
    }
}
