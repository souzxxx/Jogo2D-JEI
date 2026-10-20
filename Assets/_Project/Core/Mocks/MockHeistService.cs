using System;
using System.Collections.Generic;

namespace AFirma.Core
{
    /// <summary>
    /// Golpe simplificado que dispara exatamente os mesmos eventos do golpe real.
    /// Serve pra Cena do Golpe (Miguel) e a HUD (Enzo) funcionarem antes do motor real (Leonardo).
    ///
    /// Simplificações (o motor real resolve): poder = maior Perícia da crew (sem regra de especialista),
    /// alerta não usa Discrição nem manhas, eventos sem peso por manha.
    /// </summary>
    public class MockHeistService : IHeistService, ITickable
    {
        readonly Random rng = new Random();
        readonly List<ThiefState> crew = new List<ThiefState>();
        readonly HashSet<string> usedAbilities = new HashSet<string>();
        readonly List<ThiefState> pendingArrests = new List<ThiefState>();

        HeistPhase phase = HeistPhase.Idle;
        float alert;
        float roomProgress;
        float freezeTimer;
        float delayTimer;
        float policeLeft;
        bool siren;
        double bag;
        int roomIndex;
        int roomsCleared;
        readonly List<string> leveledUp = new List<string>();

        static GameState S => Services.State;
        static GameConfig Cfg => Services.Config;

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
                UnityEngine.Debug.LogWarning("[Golpe mock] " + reason);
                return;
            }

            crew.Clear();
            crew.AddRange(team);
            usedAbilities.Clear();
            pendingArrests.Clear();
            leveledUp.Clear();
            alert = 0f; freezeTimer = 0f; delayTimer = 0f; policeLeft = 0f; siren = false;
            bag = 0; roomsCleared = 0;
            PendingDoor = null; PendingEvent = null; LastResult = null;
            CurrentHeist = heist;
            Context = Services.Progression.GetContext(heist);

            foreach (var thief in crew) Services.Crew.SetStatus(thief, ThiefStatus.OnHeist);

            S.hasActiveHeist = true;
            S.activeHeist = new ActiveHeistRecord { heistId = heist.id, bag = 0 };
            foreach (var thief in crew) S.activeHeist.crewIds.Add(thief.id);
            Services.SaveNow();

            phase = HeistPhase.Working;
            GameEvents.RaiseHeistStarted(heist);
            GameEvents.RaiseAlertChanged(alert);
            GameEvents.RaiseBagChanged(bag);
            EnterRoom(0);
        }

        void EnterRoom(int index)
        {
            roomIndex = index;
            roomProgress = 0f;
            phase = HeistPhase.Working;
            GameEvents.RaiseRoomEntered(index, CurrentRoom);

            if (crew.Count > 0 && rng.NextDouble() < 0.3)
            {
                var talker = crew[rng.Next(crew.Count)];
                var def = Services.Db.GetThiefType(talker.type);
                if (def != null && def.barks != null && def.barks.Length > 0)
                    GameEvents.RaiseThiefBark(talker, def.barks[rng.Next(def.barks.Length)]);
            }

            var evt = PickEvent();
            if (evt != null) TriggerEvent(evt);
        }

        HeistEventDef PickEvent()
        {
            var events = Services.Db.events;
            if (!S.tutorialDone && CurrentHeist.tier == 1 && roomIndex == 0)
            {
                var tutorial = events.Find(e => e != null && e.tutorialOnly);
                if (tutorial != null) return tutorial;
            }

            if (rng.NextDouble() >= Cfg.eventChancePerRoom) return null;

            var pool = events.FindAll(e => e != null && !e.tutorialOnly && e.minTier <= CurrentHeist.tier &&
                                           (e.anyRoom || e.roomObstacle == CurrentRoom.obstacle));
            if (pool.Count == 0) return null;

            float total = 0f;
            foreach (var e in pool) total += Math.Max(0.01f, e.weight);
            var roll = rng.NextDouble() * total;
            foreach (var e in pool)
            {
                roll -= Math.Max(0.01f, e.weight);
                if (roll <= 0) return e;
            }
            return pool[pool.Count - 1];
        }

        // ---------------- Loop ----------------

        public void Tick(float deltaSeconds)
        {
            if (!IsRunning) return;
            float dt = deltaSeconds * Math.Max(0.1f, Speed);

            if (siren && phase != HeistPhase.AwaitingEvent)
            {
                policeLeft -= dt;
                GameEvents.RaisePoliceCountdownTick(Math.Max(0f, policeLeft));
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
            float power = 1f;
            foreach (var thief in crew) power = Math.Max(power, Services.Crew.GetSkill(thief));
            float seconds = room.difficulty * Context.DifficultyMult(room.obstacle) / power;
            roomProgress = Math.Min(1f, roomProgress + dt / Math.Max(0.5f, seconds));
            GameEvents.RaiseRoomProgress(roomIndex, roomProgress);

            if (freezeTimer > 0f) freezeTimer -= dt;
            else AddAlert(room.alertPerSecond * Context.alertMult * dt);

            if (phase == HeistPhase.Working && roomProgress >= 1f) ClearRoom();
        }

        void ClearRoom()
        {
            var room = CurrentRoom;
            double loot = Math.Round(room.loot * Context.lootMult);
            bag += loot;
            roomsCleared++;
            S.activeHeist.bag = bag;

            foreach (var thief in crew)
                if (Services.Crew.AddXp(thief, Cfg.xpPerRoom) && !leveledUp.Contains(thief.id))
                    leveledUp.Add(thief.id);

            GameEvents.RaiseRoomCleared(roomIndex, room, loot);
            GameEvents.RaiseBagChanged(bag);

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
                hasSpecialist = HasSpecialist(next.obstacle),
                estimatedLoot = Math.Round(next.loot * Context.lootMult),
                currentBag = bag,
                sirenActive = siren
            };
            GameEvents.RaiseDoorDecisionRequested(PendingDoor);
        }

        bool HasSpecialist(ObstacleType obstacle)
        {
            foreach (var thief in crew)
            {
                var def = Services.Db.GetThiefType(thief.type);
                if (def != null && def.hasSpecialty && def.specialty == obstacle) return true;
            }
            return false;
        }

        // ---------------- Decisões ----------------

        public void ChooseDoor(bool goToNextRoom)
        {
            if (phase != HeistPhase.AwaitingDoor) return;
            GameEvents.RaiseDoorDecisionMade(goToNextRoom);
            var next = PendingDoor != null ? PendingDoor.nextRoomIndex : roomIndex + 1;
            PendingDoor = null;
            if (goToNextRoom) EnterRoom(next);
            else Flee();
        }

        void TriggerEvent(HeistEventDef def)
        {
            var instance = new HeistEventInstance { def = def };
            foreach (var choice in def.choices)
            {
                var view = new EventChoiceView { choice = choice, available = true };
                if (choice.requiresType && !crew.Exists(t => t.type == choice.requiredType))
                {
                    view.available = false;
                    var typeDef = Services.Db.GetThiefType(choice.requiredType);
                    view.unavailableReason = "Precisa de um " + (typeDef != null ? typeDef.displayName : choice.requiredType.ToString()) + ".";
                }
                view.successChance = Math.Min(1f, Math.Max(0f, choice.baseChance + choice.chancePerStatPoint * BestStat(choice.check)));
                instance.choices.Add(view);
            }

            PendingEvent = instance;
            phase = HeistPhase.AwaitingEvent;
            GameEvents.RaiseHeistEventTriggered(instance);
        }

        float BestStat(CheckStat stat)
        {
            float best = 0f;
            foreach (var thief in crew)
            {
                float value = stat == CheckStat.Skill ? Services.Crew.GetSkill(thief)
                    : stat == CheckStat.Stealth ? Services.Crew.GetStealth(thief)
                    : stat == CheckStat.Nerves ? Services.Crew.GetNerves(thief) : 0f;
                best = Math.Max(best, value);
            }
            return best;
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
            GameEvents.RaiseHeistEventResolved(resolution);
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
            GameEvents.RaiseAbilityUsed(thief, ability);
            if (ability.sfx != null) Services.Audio.PlaySfx(ability.sfx);
            ability.Use(this);
        }

        // ---------------- IHeistControl ----------------

        public void AddAlert(float delta)
        {
            if (!IsRunning || delta == 0f) return;
            alert = Math.Min(100f, Math.Max(0f, alert + delta));
            GameEvents.RaiseAlertChanged(alert);
            if (alert >= 100f && !siren) StartSiren();
        }

        void StartSiren()
        {
            siren = true;
            policeLeft = Cfg.policeCountdownSeconds + Context.policeCountdownDelta
                         + Services.Economy.GetUpgradeTotal(UpgradeEffect.PoliceRadioSeconds) + DriverBonus(d => d.policeCountdownBonus);
            policeLeft = Math.Max(3f, policeLeft);
            GameEvents.RaiseSirenStarted(policeLeft);
        }

        float DriverBonus(Func<ThiefTypeDef, float> pick)
        {
            float best = 0f;
            foreach (var thief in crew)
            {
                var def = Services.Db.GetThiefType(thief.type);
                if (def != null) best = Math.Max(best, pick(def));
            }
            return best;
        }

        public void FreezeAlert(float seconds) => freezeTimer = Math.Max(freezeTimer, seconds);

        public void CompleteCurrentRoom()
        {
            if (phase != HeistPhase.Working) return;
            roomProgress = 1f;
            GameEvents.RaiseRoomProgress(roomIndex, roomProgress);
            ClearRoom();
        }

        public void PerfectEscape()
        {
            if (IsRunning) Finish(HeistOutcome.PerfectEscape, false);
        }

        public void AddBagPercent(float percent)
        {
            bag = Math.Max(0, Math.Round(bag * (1 + percent / 100.0)));
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
            float escapeBonus = DriverBonus(d => d.escapeLootBonus);
            float keep = outcome == HeistOutcome.HastyEscape ? Cfg.hastyEscapeLootKeep + escapeBonus
                : outcome == HeistOutcome.ForcedEscape ? Cfg.forcedEscapeLootKeep + escapeBonus
                : 1f;
            keep = Math.Min(1f, keep);

            var arrested = new List<ThiefState>();
            if (outcome != HeistOutcome.PerfectEscape)
            {
                arrested.AddRange(pendingArrests);
                if (outcome == HeistOutcome.ForcedEscape)
                {
                    float chance = Math.Max(0f, Cfg.baseArrestChance - DriverBonus(d => d.arrestChanceReduction));
                    foreach (var thief in crew)
                        if (!arrested.Contains(thief) && rng.NextDouble() < chance) arrested.Add(thief);
                }
            }

            double money = Math.Round(bag * keep * (1 + Services.Economy.GetUpgradeTotal(UpgradeEffect.LaundryPercent) / 100.0));
            Services.Economy.Add(money, "golpe " + CurrentHeist.id);
            S.cycleLoot += money;
            S.lifetimeLoot += money;

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
                xpPerThief = roomsCleared * Cfg.xpPerRoom
            };
            result.leveledUpThiefIds.AddRange(leveledUp);

            foreach (var thief in crew)
            {
                if (arrested.Contains(thief))
                {
                    Services.Crew.Jail(thief, CurrentHeist.jailSeconds);
                    result.arrestedThiefIds.Add(thief.id);
                }
                else Services.Crew.SetStatus(thief, ThiefStatus.Available);
            }

            if (completed) Services.Progression.MarkCompleted(CurrentHeist);

            PendingDoor = null;
            PendingEvent = null;
            phase = HeistPhase.Finished;
            LastResult = result;
            S.hasActiveHeist = false;
            Services.SaveNow();
            GameEvents.RaiseHeistFinished(result);
        }

        public HeistResult RecoverInterruptedHeist()
        {
            if (!S.hasActiveHeist) return null;
            var record = S.activeHeist;
            var heist = Services.Db.GetHeist(record.heistId);

            Services.Economy.Add(record.bag, "golpe interrompido");
            S.cycleLoot += record.bag;
            S.lifetimeLoot += record.bag;
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
                moneyGained = record.bag
            };
            S.hasActiveHeist = false;
            S.activeHeist = new ActiveHeistRecord();
            LastResult = result;
            Services.SaveNow();
            GameEvents.RaiseNotice("O jogo fechou no meio do golpe. A crew meteu o pé e trouxe " + Formulas.Money(record.bag) + ".");
            return result;
        }
    }
}
