using AFirma.Core;
using AFirma.Heist;
using NUnit.Framework;
using static AFirma.Tests.HeistTestSupport;

namespace AFirma.Tests
{
    /// <summary>Cenários estranhos: aba em segundo plano, pausa, velocidade e clique repetido (ESCOPO 10).</summary>
    public class HeistRobustnessTests
    {
        HeistRunner runner;

        [SetUp]
        public void SetUp() => runner = InitWithRunner();

        [TearDown]
        public void TearDown() => Teardown();

        // ---------------- Aba em segundo plano ----------------

        [Test]
        public void BackgroundTab_AHugeFrame_DoesNotBringThePoliceAtOnce()
        {
            StartAtTheDoorWithBag100(AddThief(ThiefType.Locksmith, 50));
            runner.AddAlert(100f);

            runner.Tick(120f); // a aba voltou depois de 2 min: o Unity entrega um frame gigante

            Assert.IsTrue(runner.IsRunning, "A crew ainda tem tempo de reagir.");
            Assert.AreEqual(10f - HeistRunner.MaxTickSeconds, runner.PoliceSecondsLeft, 0.001f);
        }

        [Test]
        public void BackgroundTab_AHugeFrame_DoesNotClearTheRoomAtOnce()
        {
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 100)), Crew(AddThief(ThiefType.Locksmith, 50))); // 2 s de sala

            runner.Tick(120f);

            Assert.AreEqual(HeistRunner.MaxTickSeconds / 2f, runner.RoomProgress01, 0.001f);
        }

        // ---------------- Pausa ----------------

        [Test]
        public void Pause_FreezesTheCountdownTheAlertAndTheRoom()
        {
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 100000, alertPerSecond: 10)), Crew(AddThief(ThiefType.Locksmith, 50)));
            runner.AddAlert(100f);
            GameClock.SetPaused(true);

            Tick(5f);

            Assert.AreEqual(10f, runner.PoliceSecondsLeft, 0.001f);
            Assert.AreEqual(0f, runner.RoomProgress01);

            GameClock.SetPaused(false);
            Tick(1f);
            Assert.AreEqual(9f, runner.PoliceSecondsLeft, 0.001f);
        }

        [Test]
        public void Pause_DuringAnEvent_TheEventKeepsWaiting()
        {
            OnlyTheseEvents(MakeEvent("guarda", SafeChoice()));
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock)), Crew(AddThief(ThiefType.Locksmith, 50)));
            GameClock.SetPaused(true);

            Tick(30f);
            GameClock.SetPaused(false);

            Assert.AreEqual(HeistPhase.AwaitingEvent, runner.Phase);
            Assert.IsNotNull(runner.PendingEvent);
        }

        // ---------------- Velocidade ----------------

        [Test]
        public void Speed2x_DuringTheSiren_TheCountdownAlsoRunsTwiceAsFast()
        {
            StartAtTheDoorWithBag100(AddThief(ThiefType.Locksmith, 50));
            runner.AddAlert(100f);

            runner.Speed = 2f;
            Tick(1f);

            Assert.AreEqual(8f, runner.PoliceSecondsLeft, 0.001f);
        }

        [Test]
        public void Speed_ZeroOrNegative_NeverFreezesNorRewindsTheHeist()
        {
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 100)), Crew(AddThief(ThiefType.Locksmith, 50)));

            runner.Speed = 0f;
            Tick(1f);
            float afterZero = runner.RoomProgress01;
            runner.Speed = -5f;
            Tick(1f);

            Assert.Greater(afterZero, 0f);
            Assert.Greater(runner.RoomProgress01, afterZero);
        }

        // ---------------- Clique repetido ----------------

        [Test]
        public void DoubleClick_OnTheDoor_EntersOnlyOneRoom()
        {
            int entered = 0;
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock), Room(ObstacleType.Lock), Room(ObstacleType.Lock)), Crew(AddThief(ThiefType.Locksmith, 50)));
            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);
            GameEvents.RoomEntered += (i, room) => entered++;

            runner.ChooseDoor(true);
            runner.ChooseDoor(true);
            runner.ChooseDoor(false);

            Assert.AreEqual(1, entered);
            Assert.AreEqual(1, runner.RoomIndex);
            Assert.IsTrue(runner.IsRunning, "O \"meter o pé\" atrasado não vale: a crew já entrou na sala.");
        }

        [Test]
        public void DoubleClick_OnAnEventChoice_ResolvesOnlyOnce()
        {
            OnlyTheseEvents(MakeEvent("barulho", SafeChoice(new EventEffect { resultText = "Barulhão.", alertDelta = 20 })));
            int resolved = 0;
            GameEvents.HeistEventResolved += r => resolved++;
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 100000)), Crew(AddThief(ThiefType.Locksmith, 50)));

            runner.ChooseEventOption(0);
            runner.ChooseEventOption(0);

            Assert.AreEqual(1, resolved);
            Assert.AreEqual(20f, runner.Alert, 0.001f);
        }

        [Test]
        public void DoubleClick_OnAnAbility_UsesItOnce()
        {
            int used = 0;
            GameEvents.AbilityUsed += (t, a) => used++;
            var labia = AddThief(ThiefType.ConArtist, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Person, difficulty: 100000)), Crew(labia));
            runner.AddAlert(60f);

            runner.UseAbility(labia);
            runner.UseAbility(labia);

            Assert.AreEqual(1, used);
            Assert.AreEqual(35f, runner.Alert, 0.001f);
        }

        [Test]
        public void ChoosingDuringTheWrongPhase_IsIgnored()
        {
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock), Room(ObstacleType.Lock)), Crew(AddThief(ThiefType.Locksmith, 50)));

            runner.ChooseEventOption(0);  // não tem evento
            runner.ChooseEventOption(-1);
            runner.ChooseEventOption(99);

            Assert.AreEqual(HeistPhase.Working, runner.Phase);
            Assert.IsTrue(runner.IsRunning);
        }

        // ---------------- Apoio ----------------

        void StartAtTheDoorWithBag100(params ThiefState[] thieves)
        {
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, loot: 100), Room(ObstacleType.Lock, difficulty: 100000)), Crew(thieves));
            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);
        }
    }
}
