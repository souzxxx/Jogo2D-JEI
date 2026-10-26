using AFirma.Core;
using AFirma.Thieves.Gazua;
using NUnit.Framework;
using UnityEngine;

namespace AFirma.Tests
{
    /// <summary>Pé-de-cabra (Gazua): só em sala de Fechadura, nunca na porta, e sempre faz barulho.</summary>
    public class PeDeCabraAbilityTests
    {
        class FakeHeist : IHeistControl
        {
            public HeistPhase Phase { get; set; } = HeistPhase.Working;
            public float Alert { get; set; }
            public bool SirenActive => false;
            public RoomData CurrentRoom { get; set; } = new RoomData { obstacle = ObstacleType.Lock };
            public double Bag => 0;
            public bool completed;

            public void AddAlert(float delta) => Alert += delta;
            public void FreezeAlert(float seconds) { }
            public void CompleteCurrentRoom() => completed = true;
            public void PerfectEscape() { }
            public void AddBagPercent(float percent) { }
            public void AddDelay(float seconds) { }
            public void JailRandomThief() { }
        }

        PeDeCabraAbility ability;

        [SetUp]
        public void SetUp() => ability = ScriptableObject.CreateInstance<PeDeCabraAbility>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(ability);

        [Test]
        public void InALockRoom_ClearsTheRoomAndMakesNoise()
        {
            var heist = new FakeHeist();
            Assert.IsTrue(ability.CanUse(heist, out _));

            ability.Use(heist);

            Assert.IsTrue(heist.completed);
            Assert.AreEqual(15f, heist.Alert);
        }

        [TestCase(ObstacleType.System)]
        [TestCase(ObstacleType.Person)]
        public void OutsideALockRoom_SaysWhy(ObstacleType obstacle)
        {
            var heist = new FakeHeist { CurrentRoom = new RoomData { obstacle = obstacle } };

            Assert.IsFalse(ability.CanUse(heist, out var reason));
            Assert.AreEqual(PeDeCabraAbility.OnlyLockRoom, reason);
        }

        [Test]
        public void AtTheDoor_WaitsForTheNextRoom()
        {
            var heist = new FakeHeist { Phase = HeistPhase.AwaitingDoor };

            Assert.IsFalse(ability.CanUse(heist, out var reason));
            Assert.AreEqual(PeDeCabraAbility.WaitForTheRoom, reason);
        }

        [Test]
        public void DuringAnEvent_SaysWhy()
        {
            var heist = new FakeHeist { Phase = HeistPhase.AwaitingEvent };

            Assert.IsFalse(ability.CanUse(heist, out var reason));
            Assert.IsNotEmpty(reason);
        }
    }
}
