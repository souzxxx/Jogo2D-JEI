using System.Collections.Generic;
using AFirma.Core;
using AFirma.Heist;
using NUnit.Framework;
using static AFirma.Tests.HeistTestSupport;

namespace AFirma.Tests
{
    /// <summary>Motor de eventos: sorteio, requisitos, testes de atributo e efeitos (ESCOPO 6.4).</summary>
    public class HeistEventTests
    {
        HeistRunner runner;

        [SetUp]
        public void SetUp() => runner = InitWithRunner();

        [TearDown]
        public void TearDown() => Teardown();

        // ---------------- Chance e sorteio ----------------

        [Test]
        public void EventChance_IsTheConfigChance_AndPeFrioRaisesIt()
        {
            Services.Config.eventChancePerRoom = 0.3f;
            var plain = AddThief(ThiefType.Locksmith, 50);
            var peFrio = AddThief(ThiefType.ConArtist, 50, traitId: "pe-frio"); // ×1,5
            var peFrio2 = AddThief(ThiefType.Hacker, 50, traitId: "pe-frio");

            Assert.AreEqual(0.3f, EventRules.EventChance(Crew(plain), Services.Db), 0.001f);
            Assert.AreEqual(0.45f, EventRules.EventChance(Crew(plain, peFrio), Services.Db), 0.001f);
            Assert.AreEqual(0.675f, EventRules.EventChance(Crew(peFrio, peFrio2), Services.Db), 0.001f);

            Services.Config.eventChancePerRoom = 0.8f;
            Assert.AreEqual(1f, EventRules.EventChance(Crew(peFrio), Services.Db), 0.001f, "Nunca passa de 100%.");
        }

        [Test]
        public void CanAppear_FiltersTutorialTierAndRoom()
        {
            var tier1 = MakeHeist(Room(ObstacleType.Lock));
            var lockRoom = Room(ObstacleType.Lock);
            var systemRoom = Room(ObstacleType.System);

            var any = MakeEvent("qualquer", SafeChoice());
            Assert.IsTrue(EventRules.CanAppear(any, tier1, lockRoom));

            var tutorial = MakeEvent("tutorial", SafeChoice());
            tutorial.tutorialOnly = true;
            Assert.IsFalse(EventRules.CanAppear(tutorial, tier1, lockRoom), "Tutorial não entra no sorteio.");

            var tier2 = MakeEvent("tier2", SafeChoice());
            tier2.minTier = 2;
            Assert.IsFalse(EventRules.CanAppear(tier2, tier1, lockRoom));

            var systemOnly = MakeEvent("sistema", SafeChoice());
            systemOnly.anyRoom = false;
            systemOnly.roomObstacle = ObstacleType.System;
            Assert.IsFalse(EventRules.CanAppear(systemOnly, tier1, lockRoom));
            Assert.IsTrue(EventRules.CanAppear(systemOnly, tier1, systemRoom));
        }

        [Test]
        public void PickWeighted_FollowsTheWeights()
        {
            var common = MakeEvent("comum", SafeChoice());
            common.weight = 3f;
            var rare = MakeEvent("raro", SafeChoice());
            rare.weight = 1f;
            var pool = new List<HeistEventDef> { common, rare };

            Assert.AreSame(common, EventRules.PickWeighted(pool, 0.0));
            Assert.AreSame(common, EventRules.PickWeighted(pool, 0.74));
            Assert.AreSame(rare, EventRules.PickWeighted(pool, 0.76));
            Assert.AreSame(rare, EventRules.PickWeighted(pool, 0.999));
        }

        [Test]
        public void EnteringARoom_TriggersOnlyAnEventThatFitsTheRoom()
        {
            var systemOnly = MakeEvent("sistema", SafeChoice());
            systemOnly.anyRoom = false;
            systemOnly.roomObstacle = ObstacleType.System;
            var lockOnly = MakeEvent("fechadura", SafeChoice());
            lockOnly.anyRoom = false;
            lockOnly.roomObstacle = ObstacleType.Lock;
            OnlyTheseEvents(systemOnly, lockOnly);
            HeistEventInstance triggered = null;
            GameEvents.HeistEventTriggered += e => triggered = e;

            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock)), Crew(AddThief(ThiefType.Locksmith, 50)));

            Assert.AreEqual(HeistPhase.AwaitingEvent, runner.Phase);
            Assert.AreSame(lockOnly, runner.PendingEvent.def);
            Assert.AreSame(runner.PendingEvent, triggered);
        }

        [Test]
        public void Tutorial_OnlyInTheFirstRoomOfATier1Heist_AndOnlyOnce()
        {
            Services.State.tutorialDone = false;
            var heist = MakeHeist(Room(ObstacleType.Lock), Room(ObstacleType.Lock));
            runner.StartHeist(heist, Crew(AddThief(ThiefType.Locksmith, 50), AddThief(ThiefType.ConArtist, 50)));
            Assert.IsTrue(runner.PendingEvent.def.tutorialOnly);

            runner.ChooseEventOption(FirstAvailableChoice());
            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);
            runner.ChooseDoor(true);

            Assert.IsTrue(Services.State.tutorialDone);
            Assert.AreEqual(HeistPhase.Working, runner.Phase, "Sem evento na 2ª sala (chance 0).");
        }

        [Test]
        public void Tutorial_DoesNotShowUpInAHigherTier()
        {
            Services.State.tutorialDone = false;
            var heist = MakeHeist(Room(ObstacleType.Lock));
            heist.tier = 2;

            runner.StartHeist(heist, Crew(AddThief(ThiefType.Locksmith, 50)));

            Assert.AreEqual(HeistPhase.Working, runner.Phase);
        }

        // ---------------- Escolhas ----------------

        [Test]
        public void Choice_RequiringAMissingType_IsUnavailableWithAReason_AndCannotBePicked()
        {
            var needsHacker = new EventChoice { label = "Hackear", requiresType = true, requiredType = ThiefType.Hacker, success = new EventEffect { alertDelta = -10 } };
            OnlyTheseEvents(MakeEvent("porta", needsHacker, SafeChoice()));
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock)), Crew(AddThief(ThiefType.Locksmith, 50)));

            var view = runner.PendingEvent.choices[0];
            Assert.IsFalse(view.available);
            Assert.AreEqual("Precisa de um Disquete.", view.unavailableReason);
            Assert.IsTrue(runner.PendingEvent.choices[1].available);

            runner.ChooseEventOption(0);
            Assert.AreEqual(HeistPhase.AwaitingEvent, runner.Phase, "Escolha indisponível é ignorada.");
        }

        [Test]
        public void Choice_RequiringATypeTheCrewHas_IsAvailable()
        {
            var needsHacker = new EventChoice { label = "Hackear", requiresType = true, requiredType = ThiefType.Hacker };
            var instance = EventRules.BuildInstance(MakeEvent("porta", needsHacker, SafeChoice()),
                                                    Crew(AddThief(ThiefType.Hacker, 50)), Services.Crew, Services.Db);

            Assert.IsTrue(instance.choices[0].available);
            Assert.IsNull(instance.choices[0].unavailableReason);
        }

        [Test]
        public void Choice_Chance_IsBasePlusPerPointTimesTheBestStatOfTheCrew()
        {
            var talk = new EventChoice { label = "Conversar", check = CheckStat.Nerves, baseChance = 0.2f, chancePerStatPoint = 0.01f };
            var calm = AddThief(ThiefType.ConArtist, 50);
            calm.nerves = 50;
            var crew = Crew(AddThief(ThiefType.Locksmith, 50), calm); // Nervos 30 e 50

            var instance = EventRules.BuildInstance(MakeEvent("guarda", talk, SafeChoice()), crew, Services.Crew, Services.Db);

            Assert.AreEqual(0.7f, instance.choices[0].successChance, 0.001f); // 0,2 + 0,01 × 50
            Assert.AreEqual(1f, instance.choices[1].successChance, 0.001f);
        }

        [Test]
        public void Choice_Chance_NeverAbove100()
        {
            var talk = new EventChoice { label = "Conversar", check = CheckStat.Nerves, baseChance = 0.9f, chancePerStatPoint = 0.01f };
            var instance = EventRules.BuildInstance(MakeEvent("guarda", talk, SafeChoice()),
                                                    Crew(AddThief(ThiefType.ConArtist, 50)), Services.Crew, Services.Db);

            Assert.AreEqual(1f, instance.choices[0].successChance, 0.001f);
        }

        [Test]
        public void Event_WithNoChoiceTheCrewCanMake_IsSkipped()
        {
            var needsHacker = new EventChoice { label = "Hackear", requiresType = true, requiredType = ThiefType.Hacker };
            OnlyTheseEvents(MakeEvent("beco-sem-saida", needsHacker));

            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock)), Crew(AddThief(ThiefType.Locksmith, 50)));

            Assert.AreEqual(HeistPhase.Working, runner.Phase, "Sem saída pra crew: o evento não abre e o golpe não trava.");
            Assert.IsNull(runner.PendingEvent);
        }

        // ---------------- Efeitos ----------------

        [Test]
        public void Effect_Alert()
        {
            OnlyTheseEvents(MakeEvent("barulho", SafeChoice(new EventEffect { resultText = "Barulhão.", alertDelta = 20 })));
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock)), Crew(AddThief(ThiefType.Locksmith, 50)));

            runner.ChooseEventOption(0);

            Assert.AreEqual(20f, runner.Alert, 0.001f);
        }

        [Test]
        public void Effect_BagPercent()
        {
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, loot: 100), Room(ObstacleType.Lock)), Crew(AddThief(ThiefType.Locksmith, 50)));
            TickUntil(() => runner.Phase == HeistPhase.AwaitingDoor);
            OnlyTheseEvents(MakeEvent("rasgou", SafeChoice(new EventEffect { resultText = "A sacola rasgou.", bagPercentDelta = -50 })));
            runner.ChooseDoor(true);

            runner.ChooseEventOption(0);

            Assert.AreEqual(50.0, runner.Bag);
            Assert.AreEqual(50.0, Services.State.activeHeist.bag);
        }

        [Test]
        public void Effect_Delay_HoldsTheRoom()
        {
            OnlyTheseEvents(MakeEvent("banheiro", SafeChoice(new EventEffect { resultText = "Escondidos.", delaySeconds = 5 })));
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, difficulty: 100)), Crew(AddThief(ThiefType.Locksmith, 50)));
            runner.ChooseEventOption(0);

            Tick(2f);
            Tick(2f);
            Assert.AreEqual(0f, runner.RoomProgress01, "Ainda escondidos.");
            Tick(2f);
            Tick(0.5f);
            Assert.Greater(runner.RoomProgress01, 0f);
        }

        [Test]
        public void Effect_Jail_ArrestsSomeoneWhenTheHeistEnds()
        {
            OnlyTheseEvents(MakeEvent("flagra", SafeChoice(new EventEffect { resultText = "Pegaram o Zé.", jailRandomThief = true })));
            var gazua = AddThief(ThiefType.Locksmith, 50);
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock, loot: 100)), Crew(gazua));
            runner.ChooseEventOption(0);

            TickUntil(() => !runner.IsRunning);

            CollectionAssert.AreEqual(new[] { gazua.id }, runner.LastResult.arrestedThiefIds);
            Assert.AreEqual(ThiefStatus.Jailed, gazua.status);
        }

        [Test]
        public void Effect_Failure_UsesTheFailureEffect_AndTheResolutionTellsWhatHappened()
        {
            var risky = new EventChoice
            {
                label = "Arriscar",
                baseChance = 0f,
                success = new EventEffect { resultText = "Deu bom." },
                failure = new EventEffect { resultText = "Deu ruim.", alertDelta = 30 }
            };
            OnlyTheseEvents(MakeEvent("risco", risky));
            EventResolution resolution = null;
            GameEvents.HeistEventResolved += r => resolution = r;
            runner.StartHeist(MakeHeist(Room(ObstacleType.Lock)), Crew(AddThief(ThiefType.Locksmith, 50)));

            runner.ChooseEventOption(0);

            Assert.IsFalse(resolution.success);
            Assert.AreEqual("Deu ruim.", resolution.effect.resultText);
            Assert.AreEqual(0, resolution.choiceIndex);
            Assert.AreEqual(30f, runner.Alert, 0.001f);
            Assert.AreEqual(HeistPhase.Working, runner.Phase);
        }
    }
}
