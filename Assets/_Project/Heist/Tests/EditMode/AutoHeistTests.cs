using System;
using System.Collections.Generic;
using AFirma.Core;
using AFirma.Heist;
using NUnit.Framework;
using UnityEngine;
using static AFirma.Tests.HeistTestSupport;
using Object = UnityEngine.Object;

namespace AFirma.Tests
{
    /// <summary>Golpes no automático (ESCOPO 6.11): limite, piloto, rendimento, Repetir, prisão, chamar de volta, save e offline.</summary>
    public class AutoHeistTests
    {
        HeistRunner hand;
        AutoHeistService auto;
        HeistDef heist;
        readonly List<Object> created = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            hand = InitWithRunner();
            auto = new AutoHeistService(new System.Random(1));
            Services.Register((IAutoHeistService)auto);
            heist = Cleared(MakeHeist(Room(ObstacleType.Lock, loot: 100), Room(ObstacleType.Lock, loot: 100)));
            Services.Db.heists.Add(heist); // o load acha o golpe pelo id
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
                if (obj != null) Object.DestroyImmediate(obj);
            created.Clear();
            Teardown();
        }

        /// <summary>Golpe que já teve o cofre limpo na mão (pode ir pro automático).</summary>
        static HeistDef Cleared(HeistDef def, string id = "teste")
        {
            def.id = id;
            Services.State.completedHeists.Add(id);
            return def;
        }

        /// <summary>Tica o golpe na mão e os automáticos juntos, como o GameLoop.</summary>
        void TickAll(float seconds)
        {
            while (seconds > 0f)
            {
                float step = Math.Min(seconds, HeistRunner.MaxTickSeconds);
                hand.Tick(step);
                auto.Tick(step);
                seconds -= step;
            }
        }

        void TickUntilStopped(float maxSeconds = 300f)
        {
            for (float t = 0f; t < maxSeconds && auto.Running.Count > 0; t += 0.25f) TickAll(0.25f);
        }

        static ThiefState MaxLevel(ThiefState thief)
        {
            thief.level = Services.Config.maxLevel; // sem subir de nível, toda volta é igual
            return thief;
        }

        // ---------------- Mandar ----------------

        [Test]
        public void CanSend_OnlyAfterTheVaultWasClearedByHand()
        {
            var fresh = MakeHeist(Room(ObstacleType.Lock));
            fresh.id = "nunca-feito";
            var crew = Crew(AddThief(ThiefType.Locksmith, 50));

            Assert.IsFalse(auto.CanSend(fresh, crew, out var reason));
            Assert.AreEqual("Limpa o cofre na mão primeiro.", reason);
            Assert.IsTrue(auto.CanSend(heist, crew, out reason), reason);
        }

        [Test]
        public void CanSend_RespectsTheOrelhao()
        {
            var second = Cleared(MakeHeist(Room(ObstacleType.Lock)), "outro");
            auto.Send(heist, Crew(AddThief(ThiefType.Locksmith, 50)), AutoHeistOrder.ToTheVault, false);
            var crew = Crew(AddThief(ThiefType.Locksmith, 50));

            Assert.AreEqual(1, auto.MaxRunning);
            Assert.IsFalse(auto.CanSend(second, crew, out var reason));
            StringAssert.Contains("Orelhão", reason);

            var orelhao = ScriptableObject.CreateInstance<UpgradeDef>();
            created.Add(orelhao);
            orelhao.id = "orelhao";
            orelhao.effect = UpgradeEffect.AutoHeistSlots;
            orelhao.valuePerLevel = 1f;
            Services.Db.upgrades.Add(orelhao);
            GameState.SetLevel(Services.State.upgrades, "orelhao", 1);

            Assert.AreEqual(2, auto.MaxRunning);
            Assert.IsTrue(auto.CanSend(second, crew, out reason), reason);
        }

        [Test]
        public void CanSend_TheSameHeistOnlyOnce_AndOnlyFreeThieves()
        {
            var thief = AddThief(ThiefType.Locksmith, 50);
            auto.Send(heist, Crew(thief), AutoHeistOrder.ToTheVault, false);

            Assert.AreEqual(ThiefStatus.OnHeist, thief.status);
            Assert.IsFalse(auto.CanSend(heist, Crew(AddThief(ThiefType.Locksmith, 50)), out var reason));
            Assert.AreEqual("Esse golpe já está no automático.", reason);
            Assert.IsFalse(hand.CanStart(MakeHeist(Room(ObstacleType.Lock)), Crew(thief), out _), "Quem está no automático não vai pro golpe na mão.");
        }

        // ---------------- Rodar ----------------

        [Test]
        public void Run_PaysTheAutoMultiplier_WithoutTheHandHeistEvents()
        {
            var thief = AddThief(ThiefType.Locksmith, 50);
            int handEvents = 0;
            GameEvents.HeistStarted += _ => handEvents++;
            GameEvents.RoomEntered += (_, __) => handEvents++;
            GameEvents.AlertChanged += _ => handEvents++;
            GameEvents.HeistFinished += _ => handEvents++;
            HeistResult finished = null;
            string stopReason = "não parou";
            GameEvents.AutoHeistFinished += (run, result) => finished = result;
            GameEvents.AutoHeistStopped += (run, reason) => stopReason = reason;
            double before = Services.Economy.Money;

            auto.Send(heist, Crew(thief), AutoHeistOrder.ToTheVault, false);
            TickUntilStopped();

            Assert.AreEqual(0, handEvents, "O automático não pode mexer na cena nem na HUD do golpe na mão.");
            Assert.IsNotNull(finished);
            Assert.IsTrue(finished.auto);
            Assert.IsTrue(finished.completed);
            Assert.AreEqual(120.0, finished.moneyGained); // (100 + 100) × 0,6
            Assert.AreEqual(before + 120.0, Services.Economy.Money);
            Assert.IsNull(stopReason);
            Assert.AreEqual(ThiefStatus.Available, thief.status);
            Assert.AreEqual(0, Services.State.autoHeists.Count);
        }

        [Test]
        public void HandAndAuto_RunTogether_AndBothPay()
        {
            var handHeist = MakeHeist(Room(ObstacleType.Lock, loot: 1000));
            handHeist.id = "na-mao";
            HeistResult handResult = null;
            GameEvents.HeistFinished += r => handResult = r;
            double before = Services.Economy.Money;

            auto.Send(heist, Crew(AddThief(ThiefType.Locksmith, 50)), AutoHeistOrder.ToTheVault, false);
            hand.StartHeist(handHeist, Crew(AddThief(ThiefType.Locksmith, 50)));
            Assert.IsTrue(hand.IsRunning);
            TickUntilStopped();
            for (int i = 0; i < 400 && hand.IsRunning; i++) TickAll(0.25f);

            Assert.IsNotNull(handResult);
            Assert.AreEqual("na-mao", handResult.heistId);
            Assert.IsFalse(handResult.auto);
            Assert.AreEqual(before + 1000.0 + 120.0, Services.Economy.Money);
        }

        [Test]
        public void ClearingTheVaultOnAuto_DoesNotUnlockTheNextHeist()
        {
            var next = MakeHeist(Room(ObstacleType.Lock));
            next.id = "proximo";
            next.unlockedByDefault = false;
            next.unlockedBy = heist;
            Services.State.completedHeists.Clear(); // só pra provar que o automático não chama o MarkCompleted
            Services.State.completedHeists.Add("teste");
            Services.Db.heists.Add(next);

            auto.Send(heist, Crew(AddThief(ThiefType.Locksmith, 50)), AutoHeistOrder.ToTheVault, false);
            TickUntilStopped();

            Assert.IsFalse(Services.State.unlockedHeists.Contains("proximo"));
        }

        // ---------------- Piloto ----------------

        /// <summary>Sala de 4 s que junta 68 de alerta (20/s × (1 − 0,5 × 30/100)).</summary>
        static RoomData Loud() => Room(ObstacleType.Lock, difficulty: 200f, loot: 100, alertPerSecond: 20f);

        [Test]
        public void Quiet_FleesBeforeTheNextRoomWouldRingTheSiren()
        {
            heist.rooms.Clear();
            heist.rooms.AddRange(new[] { Loud(), Loud(), Loud() });
            HeistResult result = null;
            GameEvents.AutoHeistFinished += (run, r) => result = r;

            auto.Send(heist, Crew(AddThief(ThiefType.Locksmith, 50)), AutoHeistOrder.Quiet, false);
            TickUntilStopped();

            Assert.AreEqual(HeistOutcome.CleanEscape, result.outcome);
            Assert.IsFalse(result.sirenHappened);
            Assert.AreEqual(1, result.roomsCleared);
            Assert.AreEqual(60.0, result.moneyGained);
        }

        [Test]
        public void ToTheVault_WithoutADriver_FleesAtTheSiren()
        {
            heist.rooms.Clear();
            heist.rooms.AddRange(new[] { Loud(), Loud(), Loud() });
            HeistResult result = null;
            GameEvents.AutoHeistFinished += (run, r) => result = r;

            auto.Send(heist, Crew(AddThief(ThiefType.Locksmith, 50)), AutoHeistOrder.ToTheVault, false);
            TickUntilStopped();

            Assert.AreEqual(HeistOutcome.HastyEscape, result.outcome);
            Assert.IsTrue(result.sirenHappened);
            Assert.IsFalse(result.completed);
            Assert.AreEqual(0, result.arrestedThiefIds.Count);
        }

        [Test]
        public void ToTheVault_WithADriver_KeepsWorkingAndUsesCavaloDePauWhenThePoliceArrives()
        {
            heist.rooms.Clear();
            heist.rooms.Add(Room(ObstacleType.Person, difficulty: 200f, loot: 100, alertPerSecond: 40f)); // sirene na 1ª sala, mesmo com a Lábia
            heist.rooms.Add(Room(ObstacleType.Person, difficulty: 5000f, loot: 100));                    // a polícia chega antes
            HeistResult result = null;
            GameEvents.AutoHeistFinished += (run, r) => result = r;

            auto.Send(heist, Crew(AddThief(ThiefType.ConArtist, 50), AddThief(ThiefType.Driver, 30)), AutoHeistOrder.ToTheVault, false);
            TickUntilStopped();

            Assert.AreEqual(HeistOutcome.PerfectEscape, result.outcome);
            Assert.AreEqual(1, result.roomsCleared);
            Assert.AreEqual(60.0, result.moneyGained, "Cavalo de pau leva a sacola inteira.");
        }

        [Test]
        public void Pilot_PicksTheAvailableChoiceWithTheBestChance()
        {
            var instance = new HeistEventInstance();
            instance.choices.Add(new EventChoiceView { available = true, successChance = 0.5f });
            instance.choices.Add(new EventChoiceView { available = false, successChance = 1f });
            instance.choices.Add(new EventChoiceView { available = true, successChance = 0.8f });
            instance.choices.Add(new EventChoiceView { available = true, successChance = 0.8f });

            Assert.AreEqual(2, AutoPilot.PickChoice(instance));
        }

        [Test]
        public void Pilot_ResolvesEvents_SoTheRunNeverGetsStuck()
        {
            OnlyTheseEvents(MakeEvent("teste-evento", SafeChoice(), SafeChoice()));
            HeistResult result = null;
            GameEvents.AutoHeistFinished += (run, r) => result = r;

            auto.Send(heist, Crew(AddThief(ThiefType.Locksmith, 50)), AutoHeistOrder.ToTheVault, false);
            TickUntilStopped(60f);

            Assert.IsNotNull(result, "O golpe travou esperando alguém escolher o evento.");
            Assert.IsTrue(result.completed);
        }

        [Test]
        public void Pilot_UsesLabiaWithHighAlert()
        {
            var runner = HeistRunner.Headless(new System.Random(1), 1f);
            var conArtist = AddThief(ThiefType.ConArtist, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Person, difficulty: 2000f, alertPerSecond: 10f)), Crew(conArtist));

            for (int i = 0; i < 200 && runner.Alert < Services.Config.autoAbilityAlert; i++)
            {
                runner.Tick(0.25f);
                if (runner.Alert < Services.Config.autoAbilityAlert) AutoPilot.Step(runner, AutoHeistOrder.ToTheVault, Services.Config);
            }
            Assert.IsFalse(runner.HasUsedAbility(conArtist), "Com alerta baixo a Lábia fica guardada.");
            float alert = runner.Alert;
            AutoPilot.Step(runner, AutoHeistOrder.ToTheVault, Services.Config);

            Assert.IsTrue(runner.HasUsedAbility(conArtist));
            Assert.AreEqual(alert - 25f, runner.Alert, 0.01f);
        }

        [Test]
        public void Pilot_UsesPeDeCabraInALockRoom()
        {
            var runner = HeistRunner.Headless(new System.Random(1), 1f);
            var locksmith = AddThief(ThiefType.Locksmith, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 5000f), Room(ObstacleType.Lock)), Crew(locksmith));

            AutoPilot.Step(runner, AutoHeistOrder.ToTheVault, Services.Config);

            Assert.IsTrue(runner.HasUsedAbility(locksmith));
            Assert.GreaterOrEqual(runner.RoomIndex, 1, "O pé-de-cabra limpa a sala na hora.");
        }

        [Test]
        public void ExtraHeists_RunOnAutoFromStartToEnd([Values("banca-do-bicho", "carro-forte", "museu")] string id)
        {
            var extra = Services.Db.GetHeist(id);
            Services.State.unlockedHeists.Add(id);
            Services.State.completedHeists.Add(id);
            Services.Config.eventChancePerRoom = 1f; // toda sala abre evento: os do golpe extra entram no sorteio
            var van = Services.Db.GetUpgrade(UpgradeEffect.HeistSlots);
            GameState.SetLevel(Services.State.upgrades, van.id, van.maxLevel);
            var rng = new System.Random(3);
            var crew = new List<ThiefState>();
            foreach (ThiefType type in Enum.GetValues(typeof(ThiefType)))
            {
                var thief = ThiefFactory.Create(Services.Db, type, Services.Config.maxLevel, rng, false);
                Services.State.crew.Add(thief);
                crew.Add(thief);
            }
            HeistResult result = null;
            GameEvents.AutoHeistFinished += (run, r) => result = r;

            auto.Send(extra, crew, AutoHeistOrder.ToTheVault, false);
            TickUntilStopped(600f);

            Assert.IsNotNull(result, extra.displayName + " travou no automático.");
            Assert.AreEqual(extra.rooms.Count, result.totalRooms);
            Assert.Greater(result.moneyGained, 0.0);
        }

        // ---------------- Repetir, prisão e chamar de volta ----------------

        [Test]
        public void Repeat_ChainsRunsUntilRecalled()
        {
            var thief = MaxLevel(AddThief(ThiefType.Locksmith, 50));
            int started = 0;
            GameEvents.AutoHeistStarted += _ => started++;
            double before = Services.Economy.Money;

            auto.Send(heist, Crew(thief), AutoHeistOrder.ToTheVault, true);
            var run = auto.Running[0];
            for (int i = 0; i < 2000 && run.runsDone < 3; i++) TickAll(0.25f);

            Assert.AreEqual(3, run.runsDone);
            Assert.AreEqual(4, started);
            Assert.AreEqual(1, auto.Running.Count);
            Assert.AreEqual(ThiefStatus.OnHeist, thief.status);
            Assert.AreEqual(before + 360.0, Services.Economy.Money);

            auto.Recall(run);

            Assert.AreEqual(0, auto.Running.Count);
            Assert.AreEqual(ThiefStatus.Available, thief.status);
            Assert.AreEqual(0, Services.State.autoHeists.Count);
        }

        [Test]
        public void Recall_FleesWithTheBag()
        {
            var thief = AddThief(ThiefType.Locksmith, 50);
            auto.Send(heist, Crew(thief), AutoHeistOrder.ToTheVault, true);
            var run = auto.Running[0];
            for (int i = 0; i < 200 && run.bag <= 0; i++) TickAll(0.25f);
            double before = Services.Economy.Money;

            auto.Recall(run);

            Assert.AreEqual(HeistOutcome.CleanEscape, run.lastResult.outcome);
            Assert.AreEqual(before + 60.0, Services.Economy.Money);
            Assert.AreEqual(ThiefStatus.Available, thief.status);
        }

        [Test]
        public void Repeat_StopsAndWarnsWhenSomeoneIsJailed()
        {
            var jail = new EventEffect { resultText = "Rodou.", jailRandomThief = true };
            OnlyTheseEvents(MakeEvent("cana", SafeChoice(jail), SafeChoice(jail)));
            var thief = AddThief(ThiefType.Locksmith, 50);
            string notice = null, stopReason = null;
            GameEvents.Notice += n => notice = n;
            GameEvents.AutoHeistStopped += (run, reason) => stopReason = reason;

            auto.Send(heist, Crew(thief), AutoHeistOrder.ToTheVault, true);
            TickUntilStopped();

            Assert.AreEqual(ThiefStatus.Jailed, thief.status, "Com o jogo aberto o automático também dá cana.");
            Assert.AreEqual(0, auto.Running.Count);
            StringAssert.Contains("foi em cana", stopReason);
            Assert.AreEqual(stopReason, notice);
        }

        [Test]
        public void PrestigeOrNewGame_DropsEverything()
        {
            auto.Send(heist, Crew(AddThief(ThiefType.Locksmith, 50)), AutoHeistOrder.ToTheVault, true);

            GameEvents.RaisePrestiged(1, 1);

            Assert.AreEqual(0, auto.Running.Count);
            Assert.AreEqual(0, Services.State.autoHeists.Count);
        }

        // ---------------- Save e offline ----------------

        /// <summary>Fecha e abre o jogo: estado pelo JSON do save e um AutoHeistService novo.</summary>
        AutoHeistService Reload(long secondsAway)
        {
            Services.State.lastSaveUnix = GameClock.NowUnix;
            var json = JsonUtility.ToJson(Services.State);
            long now = GameClock.NowUnix + secondsAway;
            GameClock.UnixNowProvider = () => now;
            Services.ReplaceState(JsonUtility.FromJson<GameState>(json));
            auto = new AutoHeistService(new System.Random(2));
            Services.Register((IAutoHeistService)auto);
            auto.RestoreFromSave();
            return auto;
        }

        [Test]
        public void Save_KeepsTheRecord_WithTheBag()
        {
            auto.Send(heist, Crew(AddThief(ThiefType.Locksmith, 50)), AutoHeistOrder.Quiet, true);
            var run = auto.Running[0];
            for (int i = 0; i < 200 && run.bag <= 0; i++) TickAll(0.25f);

            var record = Services.State.autoHeists[0];
            Assert.AreEqual("teste", record.heistId);
            Assert.AreEqual(AutoHeistOrder.Quiet, record.order);
            Assert.IsTrue(record.repeat);
            Assert.AreEqual(run.bag, record.bag);
        }

        [Test]
        public void Load_MidRunWithoutRepeat_CountsAsFled()
        {
            var id = AddThief(ThiefType.Locksmith, 50).id;
            auto.Send(heist, Crew(Services.State.FindThief(id)), AutoHeistOrder.ToTheVault, false);
            var run = auto.Running[0];
            for (int i = 0; i < 200 && run.bag <= 0; i++) TickAll(0.25f);
            double before = Services.Economy.Money;

            Reload(5);

            Assert.AreEqual(0, auto.Running.Count);
            Assert.AreEqual(0, Services.State.autoHeists.Count);
            Assert.AreEqual(before + 60.0, Services.Economy.Money, "O golpe do meio conta como meter o pé com a sacola.");
            Assert.AreEqual(ThiefStatus.Available, Services.State.FindThief(id).status);
        }

        [Test]
        public void Load_MidRunWithRepeat_PaysTheBagAndKeepsRunning()
        {
            var id = AddThief(ThiefType.Locksmith, 50).id;
            auto.Send(heist, Crew(Services.State.FindThief(id)), AutoHeistOrder.ToTheVault, true);
            var run = auto.Running[0];
            for (int i = 0; i < 200 && run.bag <= 0; i++) TickAll(0.25f);
            double before = Services.Economy.Money;

            Reload(5); // pouco tempo fora: sem offline

            Assert.AreEqual(1, auto.Running.Count);
            Assert.AreEqual(before + 60.0, Services.Economy.Money);
            Assert.AreEqual(ThiefStatus.OnHeist, Services.State.FindThief(id).status);
            Assert.AreSame(Services.State.FindThief(id), auto.Running[0].crew[0], "A faixa aponta pra crew do save novo.");
        }

        [Test]
        public void Load_WithRepeat_RunsTheTimeAwayAtTheOfflineRate_WithoutArrests()
        {
            var jail = new EventEffect { resultText = "Rodou.", jailRandomThief = true };
            OnlyTheseEvents(MakeEvent("cana", SafeChoice(jail), SafeChoice(jail)));
            var thief = MaxLevel(AddThief(ThiefType.Locksmith, 50));
            auto.Send(heist, Crew(thief), AutoHeistOrder.ToTheVault, true);
            double offline = 0;
            double before = Services.Economy.Money;

            GameEvents.OfflineEarnings += (money, seconds) => offline += money;
            Reload(3600);

            Assert.Greater(offline, 0.0);
            Assert.AreEqual(before + offline, Services.Economy.Money);
            Assert.IsFalse(Services.State.crew.Exists(t => t.status == ThiefStatus.Jailed), "Fora do jogo ninguém vai preso.");
            Assert.AreEqual(1, auto.Running.Count);
        }

        [Test]
        public void Offline_StopsAtTheCap_AndReloadingThePageIsNotOffline()
        {
            auto.Send(heist, Crew(MaxLevel(AddThief(ThiefType.Locksmith, 50))), AutoHeistOrder.ToTheVault, true);
            Services.State.lastSaveUnix = GameClock.NowUnix;
            var saved = JsonUtility.ToJson(Services.State);
            long cap = (long)(Services.Config.offlineCapHours * 3600f);

            double OfflineAfter(long secondsAway)
            {
                double offline = 0;
                Action<double, double> listen = (money, seconds) => offline += money;
                GameEvents.OfflineEarnings += listen;
                Services.ReplaceState(JsonUtility.FromJson<GameState>(saved));
                Reload(secondsAway);
                GameEvents.OfflineEarnings -= listen;
                return offline;
            }

            double atCap = OfflineAfter(cap);
            double wayMore = OfflineAfter(cap * 3);

            Assert.Greater(atCap, 0.0);
            Assert.AreEqual(atCap, wayMore, "Mais que offlineCapHours fora rende o mesmo.");
            Assert.AreEqual(0.0, OfflineAfter(10), "Recarregar a página não é offline.");
        }

        [Test]
        public void Load_RunWhoseHeistNoLongerExists_StopsAndWarns()
        {
            var id = AddThief(ThiefType.Locksmith, 50).id;
            auto.Send(heist, Crew(Services.State.FindThief(id)), AutoHeistOrder.ToTheVault, true);
            Services.State.autoHeists[0].heistId = "golpe-que-sumiu";
            string notice = null;
            GameEvents.Notice += n => notice = n;

            Reload(5);

            Assert.AreEqual(0, auto.Running.Count);
            Assert.AreEqual(0, Services.State.autoHeists.Count);
            StringAssert.Contains("não existe mais", notice);
            Assert.AreEqual(ThiefStatus.Available, Services.State.FindThief(id).status);
        }
    }
}
