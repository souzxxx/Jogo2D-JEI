using System;
using System.Collections.Generic;
using AFirma.Core;

namespace AFirma.Heist
{
    /// <summary>
    /// Motor do Golpe (Leonardo). Substitui o MockHeistService e dispara os mesmos eventos, na mesma ordem
    /// (ARQUITETURA › Ciclo de vida de um golpe).
    ///
    /// Regras reais (HeistRules): especialista, poder, tempo, loot com manhas, decisão de porta,
    /// alerta, sirene, contagem da polícia, as 4 saídas, prisão, pagamento e XP. Eventos: EventRules.
    /// Habilidades: cada ThiefAbilityDef mexe no golpe só pelo IHeistControl.
    ///
    /// Sem cena (Headless): o mesmo golpe pros golpes no automático (AutoHeistService). Não dispara GameEvents,
    /// não grava o activeHeist nem libera golpe; o loot sai multiplicado e, no offline, ninguém vai preso.
    /// </summary>
    public class HeistRunner : IHeistService, ITickable
    {
        readonly Random rng;
        readonly bool live = true;     // golpe na mão: dispara os GameEvents, grava o activeHeist e libera golpe
        readonly float lootMult = 1f;  // sem cena: autoHeistLootMult (e a taxa do offline)
        readonly bool arrests = true;  // sem cena no offline: ninguém vai preso
        readonly List<ThiefState> crew = new List<ThiefState>();
        readonly HashSet<string> usedAbilities = new HashSet<string>();
        readonly List<ThiefState> pendingArrests = new List<ThiefState>();
        readonly List<string> leveledUp = new List<string>();
        readonly Dictionary<string, float> xpEarned = new Dictionary<string, float>();
        readonly Dictionary<string, int> xpGiven = new Dictionary<string, int>();

        HeistPhase phase = HeistPhase.Idle;
        float alert;
        float roomProgress;
        float freezeTimer;
        float delayTimer;
        float policeLeft;
        float fameAlertReduction;
        bool siren;
        double bag;
        int roomIndex;
        int roomsCleared;

        static GameState S => Services.State;
        static GameConfig Cfg => Services.Config;

        /// <summary>O golpe nunca anda mais que isso de tempo real num frame (aba em segundo plano, travada).</summary>
        public const float MaxTickSeconds = 0.5f;

        public HeistRunner(Random rng = null)
        {
            this.rng = rng ?? new Random();
        }

        HeistRunner(Random rng, float lootMult, bool arrests) : this(rng)
        {
            live = false;
            this.lootMult = lootMult;
            this.arrests = arrests;
        }

        /// <summary>Golpe sem cena, pros golpes no automático. lootMult multiplica o loot de cada sala.</summary>
        public static HeistRunner Headless(Random rng, float lootMult, bool arrests = true) => new HeistRunner(rng, lootMult, arrests);

        public HeistPhase Phase => phase;
        public float Alert => alert;
        public bool SirenActive => siren;
        public double Bag => bag;
        public bool IsRunning => phase != HeistPhase.Idle && phase != HeistPhase.Finished;
        public HeistDef CurrentHeist { get; private set; }
        public HeistContext Context { get; private set; }
        public IReadOnlyList<ThiefState> CurrentCrew => crew;
        public int RoomIndex => roomIndex;
        public float RoomProgress01 => roomProgress;
        public float PoliceSecondsLeft => siren ? Math.Max(0f, policeLeft) : 0f;
        public float Speed { get; set; } = 1f;
        public DoorDecision PendingDoor { get; private set; }
        public HeistEventInstance PendingEvent { get; private set; }
        public HeistResult LastResult { get; private set; }

        public RoomData CurrentRoom =>
            CurrentHeist != null && roomIndex >= 0 && roomIndex < CurrentHeist.rooms.Count ? CurrentHeist.rooms[roomIndex] : null;

        // ---------------- Início ----------------

        public bool CanStart(HeistDef heist, IReadOnlyList<ThiefState> team, out string reason)
        {
            reason = null;
            if (IsRunning) reason = "Já tem um golpe rolando.";
            else if (heist == null || heist.rooms.Count == 0) reason = "Escolha um golpe.";
            else if (!Services.Progression.IsUnlocked(heist)) reason = "Golpe ainda bloqueado.";
            else if (team == null || team.Count == 0) reason = "Escolha pelo menos 1 ladrão.";
            else if (team.Count > Services.Crew.HeistSlots) reason = $"Só cabem {Services.Crew.HeistSlots} na van.";
            else
                foreach (var thief in team)
                    if (thief.status != ThiefStatus.Available && thief.status != ThiefStatus.OnSideJob)
                    {
                        reason = $"{thief.DisplayName} não está disponível.";
                        break;
                    }
            return reason == null;
        }

        public void StartHeist(HeistDef heist, IReadOnlyList<ThiefState> team)
        {
            if (!CanStart(heist, team, out var reason))
            {
                UnityEngine.Debug.LogWarning("[Golpe] " + reason);
                return;
            }

            crew.Clear();
            crew.AddRange(team);
            usedAbilities.Clear();
            pendingArrests.Clear();
            leveledUp.Clear();
            xpEarned.Clear();
            xpGiven.Clear();
            alert = 0f; freezeTimer = 0f; delayTimer = 0f; policeLeft = 0f; siren = false;
            bag = 0; roomsCleared = 0;
            Speed = 1f; // o 2x é escolha de cada golpe, não vaza pro próximo
            PendingDoor = null; PendingEvent = null; LastResult = null;
            CurrentHeist = heist;
            Context = Services.Progression.GetContext(heist);
            Context.lootMult *= lootMult;
            fameAlertReduction = Services.Progression.GetFamePerkTotal(FamePerkEffect.AlertReductionPercent);

            foreach (var thief in crew) Services.Crew.SetStatus(thief, ThiefStatus.OnHeist);

            phase = HeistPhase.Working;
            if (live)
            {
                S.hasActiveHeist = true;
                S.activeHeist = new ActiveHeistRecord { heistId = heist.id, bag = 0 };
                foreach (var thief in crew) S.activeHeist.crewIds.Add(thief.id);
                Services.SaveNow();

                GameEvents.RaiseHeistStarted(heist);
                GameEvents.RaiseAlertChanged(alert);
                GameEvents.RaiseBagChanged(bag);
            }
            EnterRoom(0);
        }

        void EnterRoom(int index)
        {
            roomIndex = index;
            roomProgress = 0f;
            phase = HeistPhase.Working;
            if (live) GameEvents.RaiseRoomEntered(index, CurrentRoom);

            if (live && crew.Count > 0 && rng.NextDouble() < 0.3)
            {
                var talker = crew[rng.Next(crew.Count)];
                var def = Services.Db.GetThiefType(talker.type);
                if (def != null && def.barks != null && def.barks.Length > 0)
                    GameEvents.RaiseThiefBark(talker, def.barks[rng.Next(def.barks.Length)]);
            }

            var evt = PickEvent();
            if (evt == null) return;
            var instance = EventRules.BuildInstance(evt, crew, Services.Crew, Services.Db);
            if (EventRules.HasAvailableChoice(instance)) TriggerEvent(instance);
        }

        /// <summary>Tutorial na 1ª sala da 1ª Lotérica; senão sorteia pela chance da sala, filtros e peso.</summary>
        HeistEventDef PickEvent()
        {
            var events = Services.Db.events;
            if (live && !S.tutorialDone && CurrentHeist.tier == 1 && roomIndex == 0)
            {
                var tutorial = events.Find(e => e != null && e.tutorialOnly);
                if (tutorial != null) return tutorial;
            }

            if (rng.NextDouble() >= EventRules.EventChance(crew, Services.Db)) return null;
            var pool = events.FindAll(e => EventRules.CanAppear(e, CurrentHeist, CurrentRoom));
            return pool.Count == 0 ? null : EventRules.PickWeighted(pool, rng.NextDouble());
        }

        // ---------------- Loop ----------------

        public void Tick(float deltaSeconds)
        {
            if (!IsRunning || GameClock.Paused) return;
            // Aba em segundo plano no WebGL volta com um frame de minutos: sem o teto a polícia chegaria de uma vez.
            float dt = Math.Min(deltaSeconds, MaxTickSeconds) * Math.Max(0.1f, Speed);

            if (siren && phase != HeistPhase.AwaitingEvent)
            {
                policeLeft -= dt;
                if (live) GameEvents.RaisePoliceCountdownTick(Math.Max(0f, policeLeft));
                if (policeLeft <= 0f)
                {
                    Finish(HeistOutcome.ForcedEscape, false);
                    return;
                }
            }

            if (phase != HeistPhase.Working) return;

            if (delayTimer > 0f)
            {
                delayTimer -= dt;
                return;
            }

            var room = CurrentRoom;
            float power = HeistRules.RoomPower(room.obstacle, crew, Services.Crew, Services.Db);
            float seconds = HeistRules.RoomSeconds(room, Context, power);
            roomProgress = Math.Min(1f, roomProgress + dt / seconds);
            if (live) GameEvents.RaiseRoomProgress(roomIndex, roomProgress);

            if (freezeTimer > 0f) freezeTimer -= dt;
            else AddAlert(HeistRules.AlertPerSecond(room, roomIndex, Context, crew, Services.Crew, Services.Db, fameAlertReduction) * dt);

            if (phase == HeistPhase.Working && roomProgress >= 1f) ClearRoom();
        }

        void ClearRoom()
        {
            var room = CurrentRoom;
            double loot = HeistRules.RoomLoot(room, Context, crew, Services.Db);
            bag += loot;
            roomsCleared++;
            if (live) S.activeHeist.bag = bag;

            foreach (var thief in crew) GiveRoomXp(thief);

            if (live)
            {
                GameEvents.RaiseRoomCleared(roomIndex, room, loot);
                GameEvents.RaiseBagChanged(bag);
            }

            if (roomIndex >= CurrentHeist.rooms.Count - 1)
            {
                Finish(siren ? HeistOutcome.HastyEscape : HeistOutcome.CleanEscape, true);
                return;
            }

            phase = HeistPhase.AwaitingDoor;
            var next = CurrentHeist.rooms[roomIndex + 1];
            PendingDoor = new DoorDecision
            {
                nextRoomIndex = roomIndex + 1,
                nextRoom = next,
                hasSpecialist = HeistRules.HasSpecialist(next.obstacle, crew, Services.Db),
                estimatedLoot = HeistRules.RoomLoot(next, Context, crew, Services.Db),
                currentBag = bag,
                sirenActive = siren
            };
            if (live) GameEvents.RaiseDoorDecisionRequested(PendingDoor);
        }

        /// <summary>Soma o XP fracionado da sala e entrega só a parte inteira. O resto fica pra próxima sala.</summary>
        void GiveRoomXp(ThiefState thief)
        {
            xpEarned.TryGetValue(thief.id, out var earned);
            earned += HeistRules.XpPerRoom(thief, Services.Db);
            xpEarned[thief.id] = earned;

            xpGiven.TryGetValue(thief.id, out var given);
            int xp = (int)Math.Floor(earned + 0.0001f) - given;
            if (xp <= 0) return;
            xpGiven[thief.id] = given + xp;
            if (Services.Crew.AddXp(thief, xp) && !leveledUp.Contains(thief.id)) leveledUp.Add(thief.id);
        }

        // ---------------- Decisões ----------------

        public void ChooseDoor(bool goToNextRoom)
        {
            if (phase != HeistPhase.AwaitingDoor) return;
            if (live) GameEvents.RaiseDoorDecisionMade(goToNextRoom);
            var next = PendingDoor != null ? PendingDoor.nextRoomIndex : roomIndex + 1;
            PendingDoor = null;
            if (goToNextRoom) EnterRoom(next);
            else Flee();
        }

        void TriggerEvent(HeistEventInstance instance)
        {
            PendingEvent = instance;
            phase = HeistPhase.AwaitingEvent;
            if (live) GameEvents.RaiseHeistEventTriggered(instance);
        }

        public void ChooseEventOption(int choiceIndex)
        {
            if (phase != HeistPhase.AwaitingEvent || PendingEvent == null) return;
            if (choiceIndex < 0 || choiceIndex >= PendingEvent.choices.Count) return;
            var view = PendingEvent.choices[choiceIndex];
            if (!view.available) return;

            bool success = rng.NextDouble() < view.successChance;
            var effect = success ? view.choice.success : view.choice.failure;
            var resolution = new EventResolution { def = PendingEvent.def, choiceIndex = choiceIndex, success = success, effect = effect };

            if (PendingEvent.def.tutorialOnly) S.tutorialDone = true;
            PendingEvent = null;
            phase = HeistPhase.Working;

            if (effect != null)
            {
                if (effect.bagPercentDelta != 0f) AddBagPercent(effect.bagPercentDelta);
                if (effect.delaySeconds > 0f) AddDelay(effect.delaySeconds);
                if (effect.jailRandomThief) JailRandomThief();
            }
            if (live) GameEvents.RaiseHeistEventResolved(resolution);
            if (effect != null && effect.alertDelta != 0f) AddAlert(effect.alertDelta);
        }

        // ---------------- Habilidades ----------------

        public bool HasUsedAbility(ThiefState thief) => usedAbilities.Contains(thief.id);

        public bool CanUseAbility(ThiefState thief, out string reason)
        {
            reason = null;
            if (!IsRunning) { reason = "Nenhum golpe rolando."; return false; }
            if (!crew.Contains(thief)) { reason = "Não está neste golpe."; return false; }
            if (HasUsedAbility(thief)) { reason = "Já usou neste golpe."; return false; }
            var def = Services.Db.GetThiefType(thief.type);
            if (def == null || def.ability == null) { reason = "Sem habilidade."; return false; }
            return def.ability.CanUse(this, out reason);
        }

        public void UseAbility(ThiefState thief)
        {
            if (!CanUseAbility(thief, out _)) return;
            var ability = Services.Db.GetThiefType(thief.type).ability;
            usedAbilities.Add(thief.id);
            if (live)
            {
                GameEvents.RaiseAbilityUsed(thief, ability);
                if (ability.sfx != null) Services.Audio.PlaySfx(ability.sfx);
            }
            ability.Use(this);
        }

        // ---------------- IHeistControl ----------------

        public void AddAlert(float delta)
        {
            if (!IsRunning || delta == 0f) return;
            alert = Math.Min(100f, Math.Max(0f, alert + delta));
            if (live) GameEvents.RaiseAlertChanged(alert);
            if (alert >= 100f && !siren) StartSiren();
        }

        void StartSiren()
        {
            siren = true;
            policeLeft = HeistRules.PoliceCountdown(Context, crew, Services.Db, Services.Economy.GetUpgradeTotal(UpgradeEffect.PoliceRadioSeconds));
            if (live) GameEvents.RaiseSirenStarted(policeLeft);
        }

        public void FreezeAlert(float seconds) => freezeTimer = Math.Max(freezeTimer, seconds);

        public void CompleteCurrentRoom()
        {
            if (phase != HeistPhase.Working) return;
            roomProgress = 1f;
            if (live) GameEvents.RaiseRoomProgress(roomIndex, roomProgress);
            ClearRoom();
        }

        public void PerfectEscape()
        {
            if (IsRunning) Finish(HeistOutcome.PerfectEscape, false);
        }

        public void AddBagPercent(float percent)
        {
            bag = Math.Max(0, Math.Round(bag * (1 + percent / 100.0)));
            if (!live) return;
            S.activeHeist.bag = bag;
            GameEvents.RaiseBagChanged(bag);
        }

        public void AddDelay(float seconds) => delayTimer += Math.Max(0f, seconds);

        public void JailRandomThief()
        {
            var free = crew.FindAll(t => !pendingArrests.Contains(t));
            if (free.Count > 0) pendingArrests.Add(free[rng.Next(free.Count)]);
        }

        public void Flee()
        {
            if (!IsRunning) return;
            Finish(siren ? HeistOutcome.HastyEscape : HeistOutcome.CleanEscape, false);
        }

        // ---------------- Fim ----------------

        void Finish(HeistOutcome outcome, bool completed)
        {
            float keep = HeistRules.LootKeep(outcome, crew, Services.Db);

            // Cavalo de pau salva todo mundo, até quem um evento ia prender. Fora isso, só a fuga forçada sorteia prisão.
            var arrested = new List<ThiefState>();
            if (outcome != HeistOutcome.PerfectEscape && arrests)
            {
                arrested.AddRange(pendingArrests);
                if (outcome == HeistOutcome.ForcedEscape)
                    foreach (var thief in crew)
                        if (!arrested.Contains(thief) && rng.NextDouble() < HeistRules.ArrestChance(thief, crew, Services.Crew, Services.Db))
                            arrested.Add(thief);
            }

            double money = Pay(bag, keep, (live ? "golpe " : "golpe no automático ") + CurrentHeist.id);

            var result = new HeistResult
            {
                heistId = CurrentHeist.id,
                outcome = outcome,
                roomsCleared = roomsCleared,
                totalRooms = CurrentHeist.rooms.Count,
                completed = completed,
                sirenHappened = siren,
                bag = bag,
                moneyGained = money,
                xpPerThief = roomsCleared * Cfg.xpPerRoom,
                auto = !live
            };
            result.leveledUpThiefIds.AddRange(leveledUp);

            foreach (var thief in crew)
            {
                xpGiven.TryGetValue(thief.id, out var xp);
                result.xpGains.Add(new ThiefXpGain { thiefId = thief.id, xp = xp, levelAfter = thief.level, leveledUp = leveledUp.Contains(thief.id) });

                if (arrested.Contains(thief))
                {
                    Services.Crew.Jail(thief, CurrentHeist.jailSeconds);
                    result.arrestedThiefIds.Add(thief.id);
                }
                else Services.Crew.SetStatus(thief, ThiefStatus.Available);
            }

            PendingDoor = null;
            PendingEvent = null;
            phase = HeistPhase.Finished;
            LastResult = result;
            if (!live) return; // o AutoHeistService cuida do save e avisa a faixa; cofre no automático não libera golpe

            if (completed) Services.Progression.MarkCompleted(CurrentHeist);
            S.hasActiveHeist = false;
            Services.SaveNow();
            GameEvents.RaiseHeistFinished(result);
        }

        public HeistResult RecoverInterruptedHeist()
        {
            if (!live || !S.hasActiveHeist) return null;
            var record = S.activeHeist;
            var heist = Services.Db.GetHeist(record.heistId);

            double money = Pay(record.bag, 1f, "golpe interrompido"); // conta como meter o pé antes da sirene
            foreach (var id in record.crewIds)
            {
                var thief = S.FindThief(id);
                if (thief != null && thief.status == ThiefStatus.OnHeist) Services.Crew.SetStatus(thief, ThiefStatus.Available);
            }

            var result = new HeistResult
            {
                heistId = record.heistId,
                outcome = HeistOutcome.Interrupted,
                totalRooms = heist != null ? heist.rooms.Count : 0,
                bag = record.bag,
                moneyGained = money
            };
            S.hasActiveHeist = false;
            S.activeHeist = new ActiveHeistRecord();
            LastResult = result;
            Services.SaveNow();
            GameEvents.RaiseNotice("O jogo fechou no meio do golpe. A crew meteu o pé e trouxe " + Formulas.Money(money) + ".");
            return result;
        }

        /// <summary>Único lugar onde o dinheiro do golpe (na mão ou no automático) entra no caixa.</summary>
        internal static double Pay(double amount, float keep, string reason)
        {
            double money = HeistRules.Payment(amount, keep, Services.Economy.GetUpgradeTotal(UpgradeEffect.LaundryPercent),
                                              Services.Progression.GetFamePerkTotal(FamePerkEffect.LootPercent));
            Services.Economy.Add(money, reason);
            S.cycleLoot += money;
            S.lifetimeLoot += money;
            return money;
        }
    }
}
