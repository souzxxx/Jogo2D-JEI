using System.Collections.Generic;
using AFirma.Core;
using AFirma.Heist;
using NUnit.Framework;
using static AFirma.Tests.HeistTestSupport;

namespace AFirma.Tests
{
    /// <summary>Alerta, sirene, contagem da polícia, as 4 saídas e prisão (ESCOPO 6.4).</summary>
    public class HeistAlertTests
    {
        HeistRunner runner;

        [SetUp]
        public void SetUp() => runner = InitWithRunner();

        [TearDown]
        public void TearDown() => Teardown();

        // ---------------- Taxa de alerta ----------------

        [Test]
        public void AlertPerSecond_AverageStealthLowersTheRoomRate()
        {
            var crew = Crew(WithStealth(AddThief(ThiefType.Locksmith, 50), 30), WithStealth(AddThief(ThiefType.ConArtist, 50), 50));
            Assert.AreEqual(1.6f, Rate(Room(ObstacleType.Lock, alertPerSecond: 2), crew), 0.001f); // 2 × (1 − 0,5 × 40/100)
        }

        [Test]
        public void AlertPerSecond_DoublesWithoutTheSpecialist()
        {
            var crew = Crew(WithStealth(AddThief(ThiefType.Locksmith, 50), 0));
            Assert.AreEqual(4f, Rate(Room(ObstacleType.System, alertPerSecond: 2), crew), 0.001f);
        }

        [Test]
        public void AlertPerSecond_UsesEffectiveStealth()
        {
            var crew = Crew(WithStealth(AddThief(ThiefType.Locksmith, 50, traitId: "fominha"), 40)); // Discrição ×0,85 = 34
            Assert.AreEqual(1.66f, Rate(Room(ObstacleType.Lock, alertPerSecond: 2), crew), 0.001f);
        }

        [Test]
        public void AlertPerSecond_BocaAbertaRaisesIt()
        {
            var crew = Crew(WithStealth(AddThief(ThiefType.Locksmith, 50, traitId: "boca-aberta"), 0)); // alerta ×1,2
            Assert.AreEqual(2.4f, Rate(Room(ObstacleType.Lock, alertPerSecond: 2), crew), 0.001f);
        }

        [Test]
        public void AlertPerSecond_AppliesTheContext_AndTheFirstRoomsOnlyInTheFirstRooms()
        {
            var ctx = new HeistContext { alertMult = 1.2f, firstRoomsAlertMult = 0.8f, firstRoomsCount = 2 };
            var crew = Crew(WithStealth(AddThief(ThiefType.Locksmith, 50), 0));
            var room = Room(ObstacleType.Lock, alertPerSecond: 2);

            Assert.AreEqual(1.92f, HeistRules.AlertPerSecond(room, 1, ctx, crew, Services.Crew, Services.Db, 0f), 0.001f);
            Assert.AreEqual(2.4f, HeistRules.AlertPerSecond(room, 2, ctx, crew, Services.Crew, Services.Db, 0f), 0.001f);
        }

        [Test]
        public void AlertPerSecond_FameSangueFrioLowersIt()
        {
            var crew = Crew(WithStealth(AddThief(ThiefType.Locksmith, 50), 0));
            Assert.AreEqual(1.6f, Rate(Room(ObstacleType.Lock, alertPerSecond: 2), crew, fame: 20f), 0.001f);
        }

        [Test]
        public void AlertPerSecond_NeverNegative_EvenWithHugeStealthAndFame()
        {
            var crew = Crew(WithStealth(AddThief(ThiefType.Locksmith, 50), 300));
            Assert.AreEqual(1f, Rate(Room(ObstacleType.Lock, alertPerSecond: 2), crew), 0.001f, "Discrição acima de 100 conta como 100.");
            Assert.AreEqual(0f, Rate(Room(ObstacleType.Lock, alertPerSecond: 2), crew, fame: 150f), 0.001f);
        }

        // ---------------- Alerta no golpe ----------------

        [Test]
        public void Alert_RisesAtTheRuleRateWhileWorking()
        {
            GameState.SetLevel(Services.State.famePerks, "sangue-frio-da-firma", 4); // −20%
            var gazua = WithStealth(AddThief(ThiefType.Locksmith, 50), 40);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 100000, alertPerSecond: 10)), Crew(gazua));

            Tick(1f);

            Assert.AreEqual(6.4f, runner.Alert, 0.01f); // 10 × (1 − 0,5 × 0,4) × 0,8
        }

        [Test]
        public void Alert_DoesNotRiseAtTheDoor()
        {
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, alertPerSecond: 10), Room(ObstacleType.Lock)),
                              Crew(AddThief(ThiefType.Locksmith, 50)));
            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);
            float atTheDoor = runner.Alert;

            Tick(5f);

            Assert.Greater(atTheDoor, 0f);
            Assert.AreEqual(atTheDoor, runner.Alert);
        }

        [Test]
        public void FreezeAlert_HoldsTheAlertForThatLong()
        {
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 100000, alertPerSecond: 10)),
                              Crew(WithStealth(AddThief(ThiefType.Locksmith, 50), 0)));
            runner.FreezeAlert(2f);

            Tick(1f);
            Tick(1f);
            Assert.AreEqual(0f, runner.Alert, "Congelado.");

            Tick(1f);
            Assert.AreEqual(10f, runner.Alert, 0.01f);
        }

        // ---------------- Sirene e contagem ----------------

        [Test]
        public void Siren_StartsAt100_WithTheBaseCountdown()
        {
            float started = -1f;
            GameEvents.SirenStarted += seconds => started = seconds;
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 100000, alertPerSecond: 50)),
                              Crew(WithStealth(AddThief(ThiefType.Locksmith, 50), 0)));

            Tick(1f);
            Assert.IsFalse(runner.SirenActive, "Alerta 50 ainda não é sirene.");
            Tick(1f);

            Assert.AreEqual(100f, runner.Alert);
            Assert.IsTrue(runner.SirenActive);
            Assert.AreEqual(10f, started, 0.001f);
            Assert.AreEqual(10f, runner.PoliceSecondsLeft, 0.001f);
        }

        [Test]
        public void PoliceCountdown_AddsDriverRadioAndModifier()
        {
            var ctx = new HeistContext { policeCountdownDelta = -3f }; // Blitz na área
            var withDriver = Crew(AddThief(ThiefType.Locksmith, 50), AddThief(ThiefType.Driver, 50)); // Pé-na-Tábua: +4 s
            var withoutDriver = Crew(AddThief(ThiefType.Locksmith, 50));

            Assert.AreEqual(15f, HeistRules.PoliceCountdown(ctx, withDriver, Services.Db, 4f), 0.001f); // 10 − 3 + 4 + 4
            Assert.AreEqual(11f, HeistRules.PoliceCountdown(ctx, withoutDriver, Services.Db, 4f), 0.001f);
        }

        [Test]
        public void PoliceCountdown_NeverBelowTheMinimum()
        {
            var ctx = new HeistContext { policeCountdownDelta = -20f };
            Assert.AreEqual(3f, HeistRules.PoliceCountdown(ctx, Crew(AddThief(ThiefType.Locksmith, 50)), Services.Db, 0f), 0.001f);
        }

        [Test]
        public void Siren_InTheHeist_CountsTheDriverAndTheRadio()
        {
            GameState.SetLevel(Services.State.upgrades, "radio-da-policia", 2); // +4 s
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock), Room(ObstacleType.Lock)),
                              Crew(AddThief(ThiefType.Locksmith, 50), AddThief(ThiefType.Driver, 50)));

            runner.AddAlert(100f);

            Assert.AreEqual(18f, runner.PoliceSecondsLeft, 0.001f); // 10 + 4 (Rádio) + 4 (Pé-na-Tábua)
        }

        [Test]
        public void Countdown_KeepsRunningAtTheDoor()
        {
            var ticks = new List<float>();
            GameEvents.PoliceCountdownTick += left => ticks.Add(left);
            StartAtTheDoorWithBag100(AddThief(ThiefType.Locksmith, 50));
            runner.AddAlert(100f);

            Tick(1f);
            Tick(1f);
            Tick(1f);

            Assert.AreEqual(HeistPhase.AwaitingDoor, runner.Phase);
            Assert.AreEqual(7f, runner.PoliceSecondsLeft, 0.001f);
            CollectionAssert.AreEqual(new[] { 9.5f, 9f, 8.5f, 8f, 7.5f, 7f }, ticks); // um aviso por frame de 0,5 s
        }

        [Test]
        public void Countdown_PausesDuringAnEvent()
        {
            StartAtTheDoorWithBag100(AddThief(ThiefType.Locksmith, 50));
            runner.AddAlert(100f);
            Services.Config.eventChancePerRoom = 1f;

            runner.ChooseDoor(true);
            Assert.AreEqual(HeistPhase.AwaitingEvent, runner.Phase, "Precisa de um evento que caiba numa sala de Fechadura.");
            Tick(5f);

            Assert.AreEqual(10f, runner.PoliceSecondsLeft, 0.001f);
        }

        // ---------------- As 4 saídas ----------------

        [Test]
        public void Exit_FleeBeforeTheSiren_KeepsTheWholeBag_AndNobodyIsArrested()
        {
            MakeArrestCertain();
            StartAtTheDoorWithBag100(AddThief(ThiefType.Locksmith, 50));

            runner.ChooseDoor(false);

            AssertExit(HeistOutcome.CleanEscape, 100.0, arrested: 0);
        }

        [Test]
        public void Exit_FleeDuringTheCountdown_Keeps80_AndNobodyIsArrested()
        {
            MakeArrestCertain();
            StartAtTheDoorWithBag100(AddThief(ThiefType.Locksmith, 50));
            runner.AddAlert(100f);

            runner.ChooseDoor(false);

            AssertExit(HeistOutcome.HastyEscape, 80.0, arrested: 0);
        }

        [Test]
        public void Exit_CountdownRunsOut_Keeps50_AndArrests()
        {
            MakeArrestCertain();
            var gazua = AddThief(ThiefType.Locksmith, 50);
            StartAtTheDoorWithBag100(gazua);
            runner.AddAlert(100f);

            TickUntil(() => !runner.IsRunning, step: 0.25f, maxSeconds: 20f);

            AssertExit(HeistOutcome.ForcedEscape, 50.0, arrested: 1);
            Assert.AreEqual(ThiefStatus.Jailed, gazua.status);
        }

        [Test]
        public void Exit_CavaloDePau_DuringTheCountdown_KeepsTheWholeBag_AndNobodyIsArrested()
        {
            MakeArrestCertain();
            var driver = AddThief(ThiefType.Driver, 50);
            StartAtTheDoorWithBag100(AddThief(ThiefType.Locksmith, 50), driver);
            runner.AddAlert(100f);
            Tick(9f);
            Assert.IsTrue(runner.IsRunning, "A polícia ainda não chegou (10 + 4 s).");

            runner.UseAbility(driver);

            AssertExit(HeistOutcome.PerfectEscape, 100.0, arrested: 0);
        }

        [Test]
        public void Exit_ClearingTheVaultDuringTheCountdown_IsAHastyEscape()
        {
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, loot: 100)), Crew(AddThief(ThiefType.Locksmith, 50)));
            runner.AddAlert(100f);

            TickUntil(() => !runner.IsRunning);

            AssertExit(HeistOutcome.HastyEscape, 80.0, arrested: 0);
            Assert.IsTrue(runner.LastResult.completed);
        }

        [Test]
        public void LootKeep_DriverAddsToTheHastyAndForcedEscapes()
        {
            var withDriver = Crew(AddThief(ThiefType.Locksmith, 50), AddThief(ThiefType.Driver, 50)); // +10%

            Assert.AreEqual(0.9f, HeistRules.LootKeep(HeistOutcome.HastyEscape, withDriver, Services.Db), 0.001f);
            Assert.AreEqual(0.6f, HeistRules.LootKeep(HeistOutcome.ForcedEscape, withDriver, Services.Db), 0.001f);
            Assert.AreEqual(1f, HeistRules.LootKeep(HeistOutcome.CleanEscape, withDriver, Services.Db), 0.001f);
            Assert.AreEqual(1f, HeistRules.LootKeep(HeistOutcome.PerfectEscape, withDriver, Services.Db), 0.001f);
        }

        [Test]
        public void Exit_WithTheDriver_TheHastyEscapeKeeps90()
        {
            StartAtTheDoorWithBag100(AddThief(ThiefType.Locksmith, 50), AddThief(ThiefType.Driver, 50));
            runner.AddAlert(100f);

            runner.ChooseDoor(false);

            AssertExit(HeistOutcome.HastyEscape, 90.0, arrested: 0);
        }

        // ---------------- Prisão ----------------

        [Test]
        public void ArrestChance_DriverAndOwnStealthLowerIt()
        {
            var gazua = WithStealth(AddThief(ThiefType.Locksmith, 50), 0);
            Assert.AreEqual(0.4f, HeistRules.ArrestChance(gazua, Crew(gazua), Services.Crew, Services.Db), 0.001f);

            gazua.stealth = 50;
            Assert.AreEqual(0.3f, HeistRules.ArrestChance(gazua, Crew(gazua), Services.Crew, Services.Db), 0.001f); // 0,4 × (1 − 0,5 × 0,5)

            var withDriver = Crew(gazua, AddThief(ThiefType.Driver, 50));
            Assert.AreEqual(0.1875f, HeistRules.ArrestChance(gazua, withDriver, Services.Crew, Services.Db), 0.001f); // (0,4 − 0,15) × 0,75
        }

        [Test]
        public void ArrestChance_NeverNegative()
        {
            Services.Config.baseArrestChance = 0.1f;
            var driver = AddThief(ThiefType.Driver, 50);
            Assert.AreEqual(0f, HeistRules.ArrestChance(driver, Crew(driver), Services.Crew, Services.Db), 0.001f);
        }

        [Test]
        public void ForcedEscape_WithZeroChance_ArrestsNobody()
        {
            Services.Config.baseArrestChance = 0f;
            var gazua = AddThief(ThiefType.Locksmith, 50);
            StartAtTheDoorWithBag100(gazua);
            runner.AddAlert(100f);

            TickUntil(() => !runner.IsRunning, step: 0.25f, maxSeconds: 20f);

            AssertExit(HeistOutcome.ForcedEscape, 50.0, arrested: 0);
            Assert.AreEqual(ThiefStatus.Available, gazua.status);
        }

        // ---------------- Apoio ----------------

        static ThiefState WithStealth(ThiefState thief, float stealth)
        {
            thief.stealth = stealth;
            return thief;
        }

        static float Rate(RoomData room, List<ThiefState> crew, float fame = 0f) =>
            HeistRules.AlertPerSecond(room, 0, new HeistContext(), crew, Services.Crew, Services.Db, fame);

        /// <summary>Golpe de 2 salas parado na porta, com Cr$ 100 na sacola. A 2ª sala é enorme pra ninguém terminar sem querer.</summary>
        void StartAtTheDoorWithBag100(params ThiefState[] thieves)
        {
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, loot: 100), Room(ObstacleType.Lock, difficulty: 100000)), Crew(thieves));
            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);
        }

        static void MakeArrestCertain()
        {
            Services.Config.baseArrestChance = 1f;
            Services.Config.stealthArrestFactor = 0f;
        }

        void AssertExit(HeistOutcome outcome, double money, int arrested)
        {
            Assert.IsFalse(runner.IsRunning);
            Assert.AreEqual(outcome, runner.LastResult.outcome);
            Assert.AreEqual(money, runner.LastResult.moneyGained);
            Assert.AreEqual(money, Services.Economy.Money);
            Assert.AreEqual(arrested, runner.LastResult.arrestedThiefIds.Count);
        }
    }
}
