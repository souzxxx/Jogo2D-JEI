using System.Collections.Generic;
using AFirma.Core;
using AFirma.Heist;
using NUnit.Framework;
using UnityEngine;
using static AFirma.Tests.HeistTestSupport;

namespace AFirma.Tests
{
    /// <summary>Habilidades ativas: uso único por golpe e as 4 do jogo (ESCOPO 6.2).</summary>
    public class HeistAbilityTests
    {
        HeistRunner runner;

        [SetUp]
        public void SetUp() => runner = InitWithRunner();

        [TearDown]
        public void TearDown() => Teardown();

        // ---------------- Cavalo de pau (Pé-na-Tábua) ----------------

        [Test]
        public void CavaloDePau_AtTheDoor_KeepsTheWholeBag()
        {
            var driver = AddThief(ThiefType.Driver, 50);
            StartAtTheDoorWithBag100(AddThief(ThiefType.Locksmith, 50), driver);

            runner.UseAbility(driver);

            Assert.AreEqual(HeistOutcome.PerfectEscape, runner.LastResult.outcome);
            Assert.AreEqual(100.0, runner.LastResult.moneyGained);
        }

        [Test]
        public void CavaloDePau_SavesEvenWhoAnEventWasGoingToArrest()
        {
            OnlyTheseEvents(MakeEvent("flagra", SafeChoice(new EventEffect { resultText = "Pegaram alguém.", jailRandomThief = true })));
            var driver = AddThief(ThiefType.Driver, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock), Room(ObstacleType.Lock)), Crew(AddThief(ThiefType.Locksmith, 50), driver));
            runner.ChooseEventOption(0);

            runner.UseAbility(driver);

            Assert.IsEmpty(runner.LastResult.arrestedThiefIds);
        }

        [Test]
        public void CavaloDePau_NotDuringAnEvent_SaysWhy()
        {
            OnlyTheseEvents(MakeEvent("guarda", SafeChoice()));
            var driver = AddThief(ThiefType.Driver, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock)), Crew(AddThief(ThiefType.Locksmith, 50), driver));

            Assert.IsFalse(runner.CanUseAbility(driver, out var reason));
            Assert.AreEqual("Resolve o evento primeiro.", reason);
        }

        // ---------------- Pé-de-cabra (Gazua) ----------------

        [Test]
        public void PeDeCabra_ClearsTheLockRoomNow_ButMakesNoise()
        {
            var gazua = WithStealth(AddThief(ThiefType.Locksmith, 50), 0);
            double cleared = -1;
            GameEvents.RoomCleared += (i, room, loot) => cleared = loot;
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 100000, loot: 100), Room(ObstacleType.Lock)), Crew(gazua));

            runner.UseAbility(gazua);

            Assert.AreEqual(100.0, cleared);
            Assert.AreEqual(100.0, runner.Bag);
            Assert.AreEqual(HeistPhase.AwaitingDoor, runner.Phase);
            Assert.AreEqual(15f, runner.Alert, 0.001f);
        }

        [Test]
        public void PeDeCabra_OnlyInALockRoom_SaysWhy()
        {
            var gazua = AddThief(ThiefType.Locksmith, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Person)), Crew(gazua));

            Assert.IsFalse(runner.CanUseAbility(gazua, out var reason));
            Assert.IsNotEmpty(reason);
            runner.UseAbility(gazua);
            Assert.IsFalse(runner.HasUsedAbility(gazua), "Não gasta a habilidade quando não dá.");
        }

        [Test]
        public void PeDeCabra_NotAtTheDoor()
        {
            var gazua = AddThief(ThiefType.Locksmith, 50);
            StartAtTheDoorWithBag100(gazua);

            Assert.IsFalse(runner.CanUseAbility(gazua, out var reason));
            Assert.IsNotEmpty(reason);
        }

        // ---------------- Loop na câmera (Disquete) ----------------

        [Test]
        public void LoopNaCamera_FreezesTheAlertFor15Seconds()
        {
            var disquete = AddThief(ThiefType.Hacker, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.System, difficulty: 100000, alertPerSecond: 10)), Crew(WithStealth(disquete, 0)));

            runner.UseAbility(disquete);
            for (int i = 0; i < 15; i++) Tick(1f);
            Assert.AreEqual(0f, runner.Alert, "15 s congelado.");

            Tick(1f);
            Assert.AreEqual(10f, runner.Alert, 0.01f);
        }

        // ---------------- Lábia (171) ----------------

        [Test]
        public void Labia_TakesOff25OfAlert()
        {
            var labia = AddThief(ThiefType.ConArtist, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Person, difficulty: 100000)), Crew(labia));
            runner.AddAlert(40f);

            runner.UseAbility(labia);

            Assert.AreEqual(15f, runner.Alert, 0.001f);
        }

        [Test]
        public void Labia_WithNoAlert_SaysWhy_AndIsNotWasted()
        {
            var labia = AddThief(ThiefType.ConArtist, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Person, difficulty: 100000)), Crew(labia));

            Assert.IsFalse(runner.CanUseAbility(labia, out var reason));
            Assert.AreEqual("Ninguém desconfia de nada ainda.", reason);
            runner.UseAbility(labia);
            Assert.IsFalse(runner.HasUsedAbility(labia));
        }

        // ---------------- Regras gerais ----------------

        [Test]
        public void Ability_OncePerHeist_AndBackOnTheNextHeist()
        {
            var labia = AddThief(ThiefType.ConArtist, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Person, loot: 100)), Crew(labia));
            runner.AddAlert(40f);
            runner.UseAbility(labia);

            Assert.IsTrue(runner.HasUsedAbility(labia));
            Assert.IsFalse(runner.CanUseAbility(labia, out var reason));
            Assert.AreEqual("Já usou neste golpe.", reason);

            TickUntil(() => !runner.IsRunning);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Person, difficulty: 100000)), Crew(labia));
            runner.AddAlert(40f);
            Assert.IsTrue(runner.CanUseAbility(labia, out reason), reason);
        }

        [Test]
        public void Ability_RaisesAbilityUsed_AndPlaysItsSound()
        {
            var clip = AudioClip.Create("teste", 10, 1, 44100, false);
            var ability = ScriptableObject.CreateInstance<SimpleAbilityDef>();
            ability.alertDelta = -5;
            ability.sfx = clip;
            GiveAbility(ThiefType.ConArtist, ability);
            var audio = new RecordingAudio();
            Services.Register((IAudioService)audio);
            var used = new List<ThiefAbilityDef>();
            GameEvents.AbilityUsed += (t, a) => used.Add(a);
            var thief = AddThief(ThiefType.ConArtist, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Person, difficulty: 100000)), Crew(thief));
            runner.AddAlert(40f);

            runner.UseAbility(thief);

            CollectionAssert.AreEqual(new[] { ability }, used);
            CollectionAssert.AreEqual(new[] { clip }, audio.Played);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void Ability_Refusals_AlwaysComeWithAReason()
        {
            var outsider = AddThief(ThiefType.ConArtist, 50);
            Assert.IsFalse(runner.CanUseAbility(outsider, out var reason));
            Assert.AreEqual("Nenhum golpe rolando.", reason);

            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 100000)), Crew(AddThief(ThiefType.Locksmith, 50)));
            Assert.IsFalse(runner.CanUseAbility(outsider, out reason));
            Assert.AreEqual("Não está neste golpe.", reason);
        }

        // ---------------- Apoio ----------------

        static ThiefState WithStealth(ThiefState thief, float stealth)
        {
            thief.stealth = stealth;
            return thief;
        }

        void StartAtTheDoorWithBag100(params ThiefState[] thieves)
        {
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, loot: 100), Room(ObstacleType.Lock, difficulty: 100000)), Crew(thieves));
            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);
        }

        class RecordingAudio : IAudioService
        {
            public readonly List<AudioClip> Played = new List<AudioClip>();
            public void PlaySfx(AudioClip clip, float volume = 1f) => Played.Add(clip);
            public void PlayMusic(MusicTrack track) { }
            public void SetHeistIntensity(float intensity01) { }
            public float GetVolume(string channel) => 1f;
            public void SetVolume(string channel, float volume01) { }
        }
    }
}
