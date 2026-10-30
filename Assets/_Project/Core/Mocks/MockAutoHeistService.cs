using System;
using System.Collections.Generic;

namespace AFirma.Core
{
    /// <summary>
    /// Golpes no automático de mentira: cada sala leva alguns segundos, o alerta sobe pela taxa da sala e a sacola
    /// enche com o loot × autoHeistLootMult. Dispara os mesmos AutoHeist* do motor real, pra faixa Golpes rolando
    /// (Enzo) e o balanceamento (Nicolas) funcionarem antes dele.
    ///
    /// Simplificações (o motor real resolve): sem especialista, eventos, habilidades nem prisão; não vai pro save.
    /// </summary>
    public class MockAutoHeistService : IAutoHeistService, ITickable
    {
        public const float RoomSeconds = 4f;

        readonly List<AutoHeistRun> running = new List<AutoHeistRun>();

        static GameConfig Cfg => Services.Config;

        public IReadOnlyList<AutoHeistRun> Running => running;

        public int MaxRunning =>
            (Cfg != null ? Cfg.startingAutoHeistSlots : 1) + (int)Services.Economy.GetUpgradeTotal(UpgradeEffect.AutoHeistSlots);

        public bool IsAutoUnlocked(HeistDef heist) => Services.Progression.IsCompleted(heist);

        public bool CanSend(HeistDef heist, IReadOnlyList<ThiefState> crew, out string reason)
        {
            reason = null;
            if (heist == null || heist.rooms.Count == 0) reason = "Escolha um golpe.";
            else if (!Services.Progression.IsUnlocked(heist)) reason = "Golpe ainda bloqueado.";
            else if (!IsAutoUnlocked(heist)) reason = "Limpa o cofre na mão primeiro.";
            else if (running.Exists(r => r.heist == heist)) reason = "Esse golpe já está no automático.";
            else if (running.Count >= MaxRunning) reason = FullReason(MaxRunning);
            else if (crew == null || crew.Count == 0) reason = "Escolha pelo menos 1 ladrão.";
            else if (crew.Count > Services.Crew.HeistSlots) reason = $"Só cabem {Services.Crew.HeistSlots} na van.";
            else
                foreach (var thief in crew)
                    if (thief.status != ThiefStatus.Available && thief.status != ThiefStatus.OnSideJob)
                    {
                        reason = $"{thief.DisplayName} não está disponível.";
                        break;
                    }
            return reason == null;
        }

        /// <summary>Motivo pra quando todas as vagas do automático estão ocupadas.</summary>
        public static string FullReason(int max) =>
            (max == 1 ? "Só cabe 1 golpe" : $"Só cabem {max} golpes") + " no automático. O Orelhão libera mais.";

        public void Send(HeistDef heist, IReadOnlyList<ThiefState> crew, AutoHeistOrder order, bool repeat)
        {
            if (!CanSend(heist, crew, out var reason))
            {
                UnityEngine.Debug.LogWarning("[Automático mock] " + reason);
                return;
            }
            var run = new AutoHeistRun { heist = heist, order = order, repeat = repeat };
            run.crew.AddRange(crew);
            running.Add(run);
            StartRun(run);
        }

        void StartRun(AutoHeistRun run)
        {
            run.roomIndex = 0; run.roomProgress01 = 0f; run.alert = 0f; run.bag = 0; run.siren = false; run.policeSecondsLeft = 0f;
            foreach (var thief in run.crew) Services.Crew.SetStatus(thief, ThiefStatus.OnHeist);
            GameEvents.RaiseAutoHeistStarted(run);
        }

        public void SetRepeat(AutoHeistRun run, bool repeat)
        {
            if (run != null) run.repeat = repeat;
        }

        public void Recall(AutoHeistRun run)
        {
            if (run == null || !running.Contains(run)) return;
            run.repeat = false;
            Finish(run, run.siren ? HeistOutcome.HastyEscape : HeistOutcome.CleanEscape, false);
        }

        public void Tick(float deltaSeconds)
        {
            if (GameClock.Paused) return;
            float dt = Math.Min(deltaSeconds, 0.5f);
            foreach (var run in running.ToArray()) TickRun(run, dt);
        }

        void TickRun(AutoHeistRun run, float dt)
        {
            var room = run.heist.rooms[run.roomIndex];
            run.alert = Math.Min(100f, run.alert + room.alertPerSecond * dt);
            run.roomProgress01 = Math.Min(1f, run.roomProgress01 + dt / RoomSeconds);

            if (run.siren)
            {
                run.policeSecondsLeft -= dt;
                if (run.policeSecondsLeft <= 0f) { Finish(run, HeistOutcome.ForcedEscape, false); return; }
            }
            else if (run.alert >= 100f)
            {
                run.siren = true;
                run.policeSecondsLeft = Cfg != null ? Cfg.policeCountdownSeconds : 10f;
                GameEvents.RaiseAutoHeistProgress(run);
                if (run.order == AutoHeistOrder.ToTheVault) { Finish(run, HeistOutcome.HastyEscape, false); return; }
            }

            float limit = Cfg != null ? Cfg.autoQuietAlertLimit : 70f;
            if (run.order == AutoHeistOrder.Quiet && run.alert >= limit) { Finish(run, run.siren ? HeistOutcome.HastyEscape : HeistOutcome.CleanEscape, false); return; }

            if (run.roomProgress01 < 1f) return;
            run.bag += Math.Round(room.loot * (Cfg != null ? Cfg.autoHeistLootMult : 0.6f));
            if (run.roomIndex >= run.RoomCount - 1) { Finish(run, run.siren ? HeistOutcome.HastyEscape : HeistOutcome.CleanEscape, true); return; }
            run.roomIndex++;
            run.roomProgress01 = 0f;
            GameEvents.RaiseAutoHeistProgress(run);
        }

        void Finish(AutoHeistRun run, HeistOutcome outcome, bool completed)
        {
            float keep = outcome == HeistOutcome.HastyEscape ? Cfg.hastyEscapeLootKeep
                : outcome == HeistOutcome.ForcedEscape ? Cfg.forcedEscapeLootKeep : 1f;
            double money = Math.Round(run.bag * keep);
            Services.Economy.Add(money, "golpe no automático " + run.heist.id);

            var result = new HeistResult
            {
                heistId = run.heist.id,
                outcome = outcome,
                roomsCleared = completed ? run.RoomCount : run.roomIndex,
                totalRooms = run.RoomCount,
                completed = completed,
                sirenHappened = run.siren,
                bag = run.bag,
                moneyGained = money,
                auto = true
            };
            run.runsDone++;
            run.moneyEarned += money;
            run.lastResult = result;
            foreach (var thief in run.crew) Services.Crew.SetStatus(thief, ThiefStatus.Available);
            GameEvents.RaiseAutoHeistFinished(run, result);

            if (run.repeat) StartRun(run);
            else
            {
                running.Remove(run);
                GameEvents.RaiseAutoHeistStopped(run, null);
            }
        }
    }
}
