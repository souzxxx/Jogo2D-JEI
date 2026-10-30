using System;
using System.Collections.Generic;

namespace AFirma.Core
{
    /// <summary>
    /// A abertura (ESCOPO 5): o golpe roteirizado no Banco Central, com a firma antiga do Chefe (um de cada tipo).
    /// Implementa o IHeistService e dispara os mesmos GameEvents do golpe de verdade, na ordem de sempre, então a Cena do
    /// Golpe e a HUD tocam a abertura sem saber que é ela. O IntroDirector põe ele no lugar do motor só enquanto toca.
    ///
    /// Não passa pelo motor e não mexe no save: não paga, não muda status nem prende ninguém da sua crew (a firma antiga
    /// não é a sua) e os botões da HUD não fazem nada. A tabela de tempos é a do ESCOPO; Speed (o 2x da HUD) acelera.
    /// </summary>
    public class IntroHeistService : IHeistService, ITickable
    {
        // Tabela do ESCOPO 5, em segundos
        public const float CaptionEnd = 3f;        // letreiro na tela preta
        public const float SirenAt = 20f;          // alerta chega a 100
        public const float DoorAt = 20.5f;         // aparece a porta: METER O PÉ ou SÓ MAIS UMA
        public const float ShoutAt = 22f;          // a crew grita "só mais uma"
        public const float OneMoreAt = 24f;        // ... e vai
        public const float ArrestAt = 30f;         // a polícia chega: todo mundo algemado, vira o jornal
        public const float BipAt = 35f;            // bip da segunda chance (o IntroDirector manda)
        public const float Length = 40f;           // segue pro esconderijo
        public const int RoomsBeforeTheDoor = 5;   // salas caindo rápido até o cofre abrir

        /// <summary>Entrada e saída de cada sala antes da porta (a 6ª entra no OneMoreAt e não termina).</summary>
        static readonly (float enter, float clear)[] RoomTimes = { (3f, 10f), (10f, 12f), (12f, 14.5f), (14.5f, 17f), (17f, 19.5f) };
        const float LoopAt = 11.5f, LoopEnd = 13f, LabiaAt = 15.5f, LabiaDrop = 25f;
        static readonly float[] BarkTimes = { 4f, 5.5f, 7f, 8.5f };

        readonly IntroDef def;
        readonly List<ThiefState> crew = new List<ThiefState>();
        readonly HashSet<string> usedAbilities = new HashSet<string>();
        readonly List<(float at, Action act)> steps = new List<(float, Action)>();
        int nextStep;
        float alert;

        public IntroHeistService(IntroDef def, GameDatabase db, Random rng = null)
        {
            this.def = def;
            rng ??= new Random();
            foreach (ThiefType type in Enum.GetValues(typeof(ThiefType)))
            {
                var thief = ThiefFactory.Create(db, type, Math.Max(1, def.crewLevel), rng, allowTrait: false);
                thief.id = "abertura-" + (int)type;
                var name = Pick(def.crewNames, (int)type);
                if (!string.IsNullOrEmpty(name)) thief.firstName = name;
                thief.status = ThiefStatus.OnHeist;
                crew.Add(thief);
            }
        }

        /// <summary>A firma antiga: um de cada tipo, fora da sua crew (Services.State.crew nunca vê esses).</summary>
        public IReadOnlyList<ThiefState> Crew => crew;
        /// <summary>Segundos de abertura tocados (com o 2x conta em dobro).</summary>
        public float Time { get; private set; }
        public string Caption => Time < CaptionEnd ? def.caption : null;

        public HeistPhase Phase { get; private set; } = HeistPhase.Idle;
        public float Alert => alert;
        public bool SirenActive => Time >= SirenAt;
        public double Bag { get; private set; }
        public bool IsRunning => Phase != HeistPhase.Idle && Phase != HeistPhase.Finished;
        public HeistDef CurrentHeist { get; private set; }
        public HeistContext Context { get; private set; }
        public IReadOnlyList<ThiefState> CurrentCrew => crew;
        public int RoomIndex { get; private set; }
        public float RoomProgress01 { get; private set; }
        public float PoliceSecondsLeft => SirenActive ? Math.Max(0f, ArrestAt - Time) : 0f;
        public float Speed { get; set; } = 1f;
        public DoorDecision PendingDoor { get; private set; }
        public HeistEventInstance PendingEvent => null;
        public HeistResult LastResult { get; private set; }

        public RoomData CurrentRoom =>
            CurrentHeist != null && RoomIndex >= 0 && RoomIndex < CurrentHeist.rooms.Count ? CurrentHeist.rooms[RoomIndex] : null;

        // ---------------- Início ----------------

        public bool CanStart(HeistDef heist, IReadOnlyList<ThiefState> team, out string reason)
        {
            reason = heist == null || heist.rooms.Count <= RoomsBeforeTheDoor ? "A abertura precisa do Banco Central." : null;
            return reason == null;
        }

        /// <summary>Começa a abertura. A crew passada é ignorada: quem vai é sempre a firma antiga.</summary>
        public void StartHeist(HeistDef heist, IReadOnlyList<ThiefState> team)
        {
            if (!CanStart(heist, team, out _)) return;
            CurrentHeist = heist;
            Context = new HeistContext { heist = heist };
            Time = 0f; Bag = 0; alert = 0f; RoomIndex = 0; RoomProgress01 = 0f;
            PendingDoor = null; LastResult = null; usedAbilities.Clear();
            BuildSteps();
            nextStep = 0;
            Phase = HeistPhase.Working;

            GameEvents.RaiseHeistStarted(heist);
            GameEvents.RaiseAlertChanged(alert);
            GameEvents.RaiseBagChanged(Bag);
            GameEvents.RaiseIntroCaption(def.caption);
        }

        void BuildSteps()
        {
            steps.Clear();
            steps.Add((CaptionEnd, () =>
            {
                GameEvents.RaiseIntroCaption(null);
                GameEvents.RaiseRoomEntered(0, CurrentRoom);
            }));
            for (int i = 0; i < crew.Count; i++)
            {
                var thief = crew[i];
                var line = Pick(def.barks, (int)thief.type);
                steps.Add((BarkTimes[i % BarkTimes.Length], () => Bark(thief, line)));
            }
            for (int i = 0; i < RoomTimes.Length; i++)
            {
                int room = i;
                steps.Add((RoomTimes[i].clear, () => ClearRoom(room)));
            }
            steps.Add((LoopAt, () => UseAbility(ThiefType.Hacker)));
            steps.Add((LabiaAt, () => UseAbility(ThiefType.ConArtist)));
            steps.Add((SirenAt, () => GameEvents.RaiseSirenStarted(ArrestAt - SirenAt)));
            steps.Add((DoorAt, OpenTheDoor));
            steps.Add((ShoutAt, () => Bark(crew[(int)ThiefType.Locksmith], def.oneMoreShout)));
            steps.Add((OneMoreAt, OneMoreRoom));
            steps.Add((ArrestAt, Arrest));
            steps.Sort((a, b) => a.at.CompareTo(b.at));
        }

        // ---------------- Loop ----------------

        public void Tick(float deltaSeconds)
        {
            if (Phase == HeistPhase.Idle || GameClock.Paused) return;
            // Depois do jornal o relógio continua: o IntroDirector manda o bip e encerra pelo Time.
            Time += Math.Min(deltaSeconds, 0.5f) * Math.Max(0.1f, Speed);
            if (!IsRunning) return;

            while (nextStep < steps.Count && steps[nextStep].at <= Time && IsRunning)
                steps[nextStep++].act();
            if (!IsRunning) return;

            float newAlert = AlertAt(Time);
            if (Math.Abs(newAlert - alert) > 0.01f)
            {
                alert = newAlert;
                GameEvents.RaiseAlertChanged(alert);
            }
            if (SirenActive) GameEvents.RaisePoliceCountdownTick(PoliceSecondsLeft);
            if (Phase != HeistPhase.Working) return;

            RoomProgress01 = ProgressAt(Time);
            GameEvents.RaiseRoomProgress(RoomIndex, RoomProgress01);
        }

        /// <summary>Sobe até 45, congela no Loop, sobe até 60, cai 25 com a Lábia e dispara até a sirene.</summary>
        public static float AlertAt(float t)
        {
            if (t < CaptionEnd) return 0f;
            if (t < LoopAt) return 45f * (t - CaptionEnd) / (LoopAt - CaptionEnd);
            if (t < LoopEnd) return 45f;
            if (t < LabiaAt) return 45f + 15f * (t - LoopEnd) / (LabiaAt - LoopEnd);
            float afterLabia = 60f - LabiaDrop;
            if (t < SirenAt) return afterLabia + (100f - afterLabia) * (t - LabiaAt) / (SirenAt - LabiaAt);
            return 100f;
        }

        float ProgressAt(float t)
        {
            if (RoomIndex < RoomTimes.Length)
            {
                var (enter, clear) = RoomTimes[RoomIndex];
                return Math.Min(1f, Math.Max(0f, (t - enter) / (clear - enter)));
            }
            return Math.Min(0.9f, Math.Max(0f, (t - OneMoreAt) / (ArrestAt - OneMoreAt + 4f))); // não dá tempo
        }

        // ---------------- Passos ----------------

        void ClearRoom(int index)
        {
            var room = CurrentHeist.rooms[index];
            RoomIndex = index;
            RoomProgress01 = 1f;
            GameEvents.RaiseRoomProgress(index, 1f);
            Bag += room.loot;
            GameEvents.RaiseRoomCleared(index, room, room.loot);
            GameEvents.RaiseBagChanged(Bag);
            if (index + 1 >= RoomsBeforeTheDoor) return; // depois do cofre vem a porta
            RoomIndex = index + 1;
            RoomProgress01 = 0f;
            GameEvents.RaiseRoomEntered(RoomIndex, CurrentRoom);
        }

        void OpenTheDoor()
        {
            Phase = HeistPhase.AwaitingDoor;
            var next = CurrentHeist.rooms[RoomsBeforeTheDoor];
            PendingDoor = new DoorDecision
            {
                nextRoomIndex = RoomsBeforeTheDoor,
                nextRoom = next,
                hasSpecialist = true,
                estimatedLoot = next.loot,
                currentBag = Bag,
                sirenActive = true
            };
            GameEvents.RaiseDoorDecisionRequested(PendingDoor);
        }

        /// <summary>A crew escolhe "só mais uma" sozinha: é o erro que a abertura ensina.</summary>
        void OneMoreRoom()
        {
            PendingDoor = null;
            GameEvents.RaiseDoorDecisionMade(true);
            Phase = HeistPhase.Working;
            RoomIndex = RoomsBeforeTheDoor;
            RoomProgress01 = 0f;
            GameEvents.RaiseRoomEntered(RoomIndex, CurrentRoom);
        }

        /// <summary>A contagem zera: fuga forçada, todo mundo preso e a capa do jornal. Ninguém da sua crew é tocado.</summary>
        void Arrest()
        {
            var result = new HeistResult
            {
                heistId = CurrentHeist.id,
                outcome = HeistOutcome.ForcedEscape,
                roomsCleared = RoomsBeforeTheDoor,
                totalRooms = CurrentHeist.rooms.Count,
                sirenHappened = true,
                bag = Bag,
                moneyGained = 0,
                headline = def.headline
            };
            foreach (var thief in crew) result.arrestedThiefIds.Add(thief.id);
            PendingDoor = null;
            Phase = HeistPhase.Finished;
            LastResult = result;
            GameEvents.RaisePoliceCountdownTick(0f);
            GameEvents.RaiseHeistFinished(result);
        }

        void Bark(ThiefState thief, string line)
        {
            if (!string.IsNullOrEmpty(line)) GameEvents.RaiseThiefBark(thief, line);
        }

        void UseAbility(ThiefType type)
        {
            var thief = crew[(int)type];
            var ability = Services.Db.GetThiefType(type)?.ability;
            if (ability == null) return;
            usedAbilities.Add(thief.id);
            GameEvents.RaiseAbilityUsed(thief, ability);
            if (ability.sfx != null) Services.Audio.PlaySfx(ability.sfx);
        }

        static string Pick(string[] list, int index) => list != null && index >= 0 && index < list.Length ? list[index] : null;

        // ---------------- A HUD não manda na abertura ----------------

        public const string NotNow = "É só a abertura: assiste.";

        public void ChooseDoor(bool goToNextRoom) { }
        public void ChooseEventOption(int choiceIndex) { }
        public bool CanUseAbility(ThiefState thief, out string reason) { reason = NotNow; return false; }
        public void UseAbility(ThiefState thief) { }
        public bool HasUsedAbility(ThiefState thief) => thief != null && usedAbilities.Contains(thief.id);
        public void Flee() { }
        public HeistResult RecoverInterruptedHeist() => null;

        public void AddAlert(float delta) { }
        public void FreezeAlert(float seconds) { }
        public void CompleteCurrentRoom() { }
        public void PerfectEscape() { }
        public void AddBagPercent(float percent) { }
        public void AddDelay(float seconds) { }
        public void JailRandomThief() { }
    }
}
