using System;
using System.Collections.Generic;
using AFirma.Core;

namespace AFirma.Heist
{
    /// <summary>
    /// Golpes no automático (Leonardo, ESCOPO 6.11). Substitui o MockAutoHeistService.
    ///
    /// Cada golpe é um HeistRunner sem cena, tickado pelo GameLoop e jogado pelo AutoPilot, junto com o golpe na mão.
    /// Rende autoHeistLootMult do mesmo golpe na mão, prende gente na fuga forçada e não libera golpe nem dá bip de marco.
    /// Com Repetir a crew emenda a próxima volta e para sozinha se alguém for preso ou sumir da firma.
    ///
    /// Save (ESCOPO 10): cada um vira um AutoHeistRecord. Ao carregar, o golpe do meio conta como "meteu o pé"; com Repetir,
    /// o tempo fora roda sem cena (até offlineCapHours, a offlineRate e sem prisão), entra no popup offline e a crew volta a rodar.
    /// </summary>
    public class AutoHeistService : IAutoHeistService, ITickable
    {
        /// <summary>Teto de voltas simuladas por golpe no offline (só segurança: 4 h dão bem menos).</summary>
        public const int MaxOfflineRuns = 5000;

        class Slot
        {
            public AutoHeistRun view;
            public AutoHeistRecord record;
            public HeistRunner runner; // null entre uma volta e outra
        }

        readonly Random rng;
        readonly List<Slot> slots = new List<Slot>();
        readonly List<AutoHeistRun> running = new List<AutoHeistRun>();
        bool restoring; // carregando o save: nada de salvar ainda

        static GameState S => Services.State;
        static GameDatabase Db => Services.Db;
        static GameConfig Cfg => Services.Config;

        public AutoHeistService(Random rng = null)
        {
            this.rng = rng ?? new Random();
            GameEvents.GameLoaded += RestoreFromSave;
            GameEvents.NewGameStarted += DropAll;
            GameEvents.Prestiged += (fame, wanted) => DropAll(); // a crew antiga sumiu do mapa junto
        }

        public IReadOnlyList<AutoHeistRun> Running => running;

        public int MaxRunning =>
            Cfg.startingAutoHeistSlots + (int)Services.Economy.GetUpgradeTotal(UpgradeEffect.AutoHeistSlots);

        public bool IsAutoUnlocked(HeistDef heist) => Services.Progression.IsCompleted(heist);

        public bool CanSend(HeistDef heist, IReadOnlyList<ThiefState> crew, out string reason)
        {
            reason = null;
            if (heist == null || heist.rooms.Count == 0) reason = "Escolha um golpe.";
            else if (!IsAutoUnlocked(heist)) reason = "Limpa o cofre na mão primeiro.";
            else if (slots.Exists(s => s.view.heist == heist)) reason = "Esse golpe já está no automático.";
            else if (slots.Count >= MaxRunning) reason = MockAutoHeistService.FullReason(MaxRunning);
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

        public void Send(HeistDef heist, IReadOnlyList<ThiefState> crew, AutoHeistOrder order, bool repeat)
        {
            if (!CanSend(heist, crew, out var reason))
            {
                UnityEngine.Debug.LogWarning("[Automático] " + reason);
                return;
            }

            var record = new AutoHeistRecord { heistId = heist.id, order = order, repeat = repeat };
            foreach (var thief in crew) record.crewIds.Add(thief.id);
            S.autoHeists.Add(record);

            var slot = Track(record, heist);
            if (StartRun(slot)) Services.SaveNow();
        }

        public void SetRepeat(AutoHeistRun run, bool repeat)
        {
            var slot = Find(run);
            if (slot == null) return;
            slot.view.repeat = repeat;
            slot.record.repeat = repeat;
        }

        public void Recall(AutoHeistRun run)
        {
            var slot = Find(run);
            if (slot == null) return;
            SetRepeat(run, false);
            if (slot.runner != null && slot.runner.IsRunning)
            {
                slot.runner.Flee();
                Advance(slot);
            }
            else Stop(slot, null);
        }

        // ---------------- Loop ----------------

        public void Tick(float deltaSeconds)
        {
            if (GameClock.Paused) return;
            foreach (var slot in slots.ToArray())
            {
                if (slot.runner == null) continue;
                slot.runner.Tick(deltaSeconds);
                Advance(slot);
            }
        }

        /// <summary>Joga o passo do piloto, copia o estado pra faixa e trata o fim da volta.</summary>
        void Advance(Slot slot)
        {
            var view = slot.view;
            int room = view.roomIndex;
            bool siren = view.siren;
            double bag = view.bag;

            AutoPilot.Step(slot.runner, view.order, Cfg);
            Sync(slot);

            if (slot.runner.Phase == HeistPhase.Finished) OnRunFinished(slot);
            else if (room != view.roomIndex || siren != view.siren || bag != view.bag) GameEvents.RaiseAutoHeistProgress(view);
        }

        static void Sync(Slot slot)
        {
            var runner = slot.runner;
            var view = slot.view;
            view.roomIndex = runner.RoomIndex;
            view.roomProgress01 = runner.RoomProgress01;
            view.alert = runner.Alert;
            view.bag = runner.Bag;
            view.siren = runner.SirenActive;
            view.policeSecondsLeft = runner.PoliceSecondsLeft;
            slot.record.bag = runner.Phase == HeistPhase.Finished ? 0 : runner.Bag;
        }

        bool StartRun(Slot slot)
        {
            var view = slot.view;
            var runner = HeistRunner.Headless(rng, Cfg.autoHeistLootMult);
            if (!runner.CanStart(view.heist, view.crew, out var reason))
            {
                Stop(slot, $"{view.heist.displayName} saiu do automático: {reason}");
                return false;
            }

            runner.StartHeist(view.heist, view.crew);
            slot.runner = runner;
            Sync(slot);
            GameEvents.RaiseAutoHeistStarted(view); // evento na primeira sala o piloto resolve no próximo Tick
            return true;
        }

        void OnRunFinished(Slot slot)
        {
            var view = slot.view;
            var result = slot.runner.LastResult;
            slot.runner = null;
            view.runsDone++;
            view.moneyEarned += result.moneyGained;
            view.lastResult = result;
            GameEvents.RaiseAutoHeistFinished(view, result);

            if (!view.repeat) Stop(slot, null);
            else if (!CanContinue(slot, out var reason)) Stop(slot, reason);
            else StartRun(slot);
            if (!restoring) Services.SaveNow();
        }

        /// <summary>Repetir só emenda se a crew inteira ainda está na firma e fora da cadeia.</summary>
        bool CanContinue(Slot slot, out string reason)
        {
            reason = null;
            var heist = slot.view.heist;
            if (heist == null)
            {
                reason = "Um golpe do automático não existe mais. A crew voltou pro esconderijo.";
                return false;
            }
            if (!IsAutoUnlocked(heist))
            {
                reason = $"{heist.displayName} saiu do automático: limpa o cofre na mão de novo.";
                return false;
            }
            foreach (var id in slot.record.crewIds)
            {
                var thief = S.FindThief(id);
                if (thief == null) reason = $"Alguém da crew saiu da firma. {heist.displayName} saiu do automático.";
                else if (thief.status == ThiefStatus.Jailed) reason = $"{thief.DisplayName} foi em cana. {heist.displayName} saiu do automático.";
                if (reason != null) return false;
            }
            return true;
        }

        /// <summary>Tira da faixa. Com motivo (algo deu errado) também avisa o jogador.</summary>
        void Stop(Slot slot, string reason)
        {
            slots.Remove(slot);
            running.Remove(slot.view);
            S.autoHeists.Remove(slot.record);
            foreach (var thief in slot.view.crew)
                if (thief.status == ThiefStatus.OnHeist && S.crew.Contains(thief))
                    Services.Crew.SetStatus(thief, ThiefStatus.Available);
            GameEvents.RaiseAutoHeistStopped(slot.view, reason);
            if (reason != null) GameEvents.RaiseNotice(reason);
        }

        /// <summary>Jogo novo ou prestígio: a crew antiga não existe mais, então some tudo sem pagar.</summary>
        void DropAll()
        {
            var views = running.ToArray();
            slots.Clear();
            running.Clear();
            S?.autoHeists.Clear();
            foreach (var view in views) GameEvents.RaiseAutoHeistStopped(view, null);
        }

        Slot Track(AutoHeistRecord record, HeistDef heist)
        {
            var slot = new Slot
            {
                record = record,
                view = new AutoHeistRun { heist = heist, order = record.order, repeat = record.repeat }
            };
            foreach (var id in record.crewIds)
            {
                var thief = S.FindThief(id);
                if (thief != null) slot.view.crew.Add(thief);
            }
            slots.Add(slot);
            running.Add(slot.view);
            return slot;
        }

        Slot Find(AutoHeistRun run) => slots.Find(s => s.view == run);

        // ---------------- Save e offline (ESCOPO 10) ----------------

        /// <summary>
        /// Ao carregar: o golpe do meio conta como "meteu o pé" (leva a sacola, ninguém preso). Com Repetir, o tempo fora
        /// roda sem cena e a crew volta a rodar; sem Repetir, a crew volta pro esconderijo.
        /// Não salva aqui: a economia ainda vai ler o lastSaveUnix pro offline dos bicos.
        /// </summary>
        public void RestoreFromSave()
        {
            slots.Clear();
            running.Clear();
            if (S == null || S.autoHeists.Count == 0) return;

            double secondsAway = GameClock.NowUnix - S.lastSaveUnix;
            double brought = 0, offline = 0;
            restoring = true;
            foreach (var record in S.autoHeists.ToArray())
            {
                var slot = Track(record, Db.GetHeist(record.heistId));
                if (record.bag > 0) brought += HeistRunner.Pay(record.bag, 1f, "golpe no automático interrompido");
                record.bag = 0;
                foreach (var thief in slot.view.crew)
                    if (thief.status == ThiefStatus.OnHeist) Services.Crew.SetStatus(thief, ThiefStatus.Available);

                if (!record.repeat) Stop(slot, null);
                else if (!CanContinue(slot, out var reason)) Stop(slot, reason);
                else
                {
                    offline += RunOffline(slot, secondsAway);
                    StartRun(slot);
                }
            }
            restoring = false;

            if (brought > 0) GameEvents.RaiseNotice("O jogo fechou com golpe no automático no meio. A crew meteu o pé e trouxe " + Formulas.Money(brought) + ".");
            if (offline > 0) GameEvents.RaiseOfflineEarnings(offline, secondsAway);
        }

        /// <summary>
        /// Repetir com o jogo fechado: a crew emenda golpes sem cena pelo tempo fora (até offlineCapHours), cada um rendendo
        /// offlineRate do automático e sem ninguém preso (fuga forçada só perde sacola). O tempo acabou no meio? Mete o pé.
        /// </summary>
        double RunOffline(Slot slot, double secondsAway)
        {
            if (double.IsNaN(secondsAway) || secondsAway < Math.Max(0f, Cfg.offlineMinSeconds)) return 0;
            double seconds = Math.Min(secondsAway, Math.Max(0f, Cfg.offlineCapHours) * 3600.0);
            var view = slot.view;
            double total = 0;

            for (int runs = 0; seconds > 0 && runs < MaxOfflineRuns; runs++)
            {
                var runner = HeistRunner.Headless(rng, Cfg.autoHeistLootMult * Cfg.offlineRate, arrests: false);
                if (!runner.CanStart(view.heist, view.crew, out _)) break;
                runner.StartHeist(view.heist, view.crew);
                AutoPilot.Step(runner, view.order, Cfg);
                while (runner.IsRunning && seconds > 0)
                {
                    runner.Tick(HeistRunner.MaxTickSeconds);
                    seconds -= HeistRunner.MaxTickSeconds;
                    AutoPilot.Step(runner, view.order, Cfg);
                }
                if (runner.IsRunning) runner.Flee();

                total += runner.LastResult.moneyGained;
                view.runsDone++;
                view.moneyEarned += runner.LastResult.moneyGained;
                view.lastResult = runner.LastResult;
            }
            return total;
        }
    }
}
