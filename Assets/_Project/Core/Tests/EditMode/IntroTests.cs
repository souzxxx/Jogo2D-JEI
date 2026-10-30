using System.Collections.Generic;
using AFirma.Core;
using NUnit.Framework;
using UnityEngine;

namespace AFirma.Tests
{
    /// <summary>Abertura (ESCOPO 5): o golpe roteirizado dispara os eventos da cena na ordem da tabela e não mexe no save.</summary>
    public class IntroTests
    {
        IntroDef def;
        IntroHeistService intro;

        [SetUp]
        public void SetUp()
        {
            TestSupport.InitServicesWithMocks();
            def = Services.Db.intro;
            intro = new IntroHeistService(def, Services.Db, new System.Random(1));
        }

        [TearDown]
        public void TearDown() => TestSupport.Teardown();

        void PlayToTheEnd()
        {
            intro.StartHeist(def.heist, intro.Crew);
            for (int i = 0; i < 2000 && intro.IsRunning; i++) intro.Tick(0.05f);
        }

        [Test]
        public void Intro_IsTheTopOfTheLadder_WithTheOldFirm()
        {
            Assert.IsNotNull(def, "Falta o Data/Narrative/Intro.asset no GameDatabase.");
            Assert.IsNotNull(def.heist);
            Assert.IsFalse(def.heist.extra);
            foreach (var heist in Services.Db.heists)
                if (heist != null && !heist.extra) Assert.LessOrEqual(heist.tier, def.heist.tier, "A abertura é no topo da escada.");
            Assert.AreEqual(4, intro.Crew.Count, "Um de cada tipo.");
            Assert.AreEqual(4, new HashSet<ThiefType>(new List<ThiefState>(intro.Crew).ConvertAll(t => t.type)).Count);
            foreach (var thief in intro.Crew) Assert.IsNull(Services.State.FindThief(thief.id), "A firma antiga não entra na sua crew.");
        }

        [Test]
        public void Intro_PlaysTheTableInOrder()
        {
            var log = new List<string>();
            GameEvents.IntroCaption += text => log.Add(text == null ? "letreiro-some" : "letreiro");
            GameEvents.HeistStarted += _ => log.Add("começou");
            GameEvents.RoomEntered += (i, room) => log.Add("sala " + i);
            GameEvents.RoomCleared += (i, room, loot) => log.Add("limpou " + i);
            GameEvents.AbilityUsed += (thief, ability) => log.Add("habilidade " + thief.type);
            GameEvents.SirenStarted += seconds => log.Add("sirene");
            GameEvents.DoorDecisionRequested += door => log.Add("porta");
            GameEvents.DoorDecisionMade += next => log.Add(next ? "só mais uma" : "meteu o pé");
            HeistResult result = null;
            GameEvents.HeistFinished += r => { result = r; log.Add("jornal"); };

            PlayToTheEnd();

            CollectionAssert.AreEqual(new[]
            {
                "começou", "letreiro", "letreiro-some", "sala 0", "limpou 0", "sala 1", "habilidade Hacker", "limpou 1", "sala 2",
                "limpou 2", "sala 3", "habilidade ConArtist", "limpou 3", "sala 4", "limpou 4", "sirene", "porta", "só mais uma",
                "sala 5", "jornal",
            }, log);
            Assert.AreEqual(HeistOutcome.ForcedEscape, result.outcome);
            Assert.AreEqual(4, result.arrestedThiefIds.Count, "Todo mundo algemado.");
            Assert.AreEqual(def.headline, result.headline);
            Assert.AreEqual(0.0, result.moneyGained);
            Assert.AreEqual(IntroHeistService.ArrestAt, intro.Time, 0.1f);
        }

        [Test]
        public void Intro_CrewBarks_AndShoutsOneMore()
        {
            var lines = new List<string>();
            GameEvents.ThiefBark += (thief, line) => lines.Add(line);

            PlayToTheEnd();

            foreach (var bark in def.barks) CollectionAssert.Contains(lines, bark);
            CollectionAssert.Contains(lines, def.oneMoreShout);
        }

        [Test]
        public void Intro_DoesNotTouchTheSave()
        {
            var before = JsonUtility.ToJson(Services.State);
            int moneyEvents = 0, crewEvents = 0;
            GameEvents.MoneyChanged += (money, delta) => moneyEvents++;
            GameEvents.CrewChanged += () => crewEvents++;
            GameEvents.ThiefJailed += thief => crewEvents++;

            PlayToTheEnd();

            Assert.AreEqual(before, JsonUtility.ToJson(Services.State), "A abertura não paga, não prende e não muda ninguém.");
            Assert.AreEqual(0, moneyEvents);
            Assert.AreEqual(0, crewEvents);
        }

        [Test]
        public void Intro_IgnoresTheHudButtons()
        {
            intro.StartHeist(def.heist, intro.Crew);
            for (float t = 0f; t < IntroHeistService.DoorAt + 0.5f; t += 0.1f) intro.Tick(0.1f);
            Assert.AreEqual(HeistPhase.AwaitingDoor, intro.Phase);

            intro.ChooseDoor(false);
            intro.Flee();
            Assert.IsFalse(intro.CanUseAbility(intro.Crew[0], out var reason));
            Assert.AreEqual(IntroHeistService.NotNow, reason);
            Assert.IsTrue(intro.IsRunning, "Na abertura quem decide é a firma antiga.");
            Assert.AreEqual(HeistPhase.AwaitingDoor, intro.Phase);
        }

        [Test]
        public void Intro_AlertClimbs_DropsWithLabia_AndRingsAtTheSiren()
        {
            Assert.AreEqual(0f, IntroHeistService.AlertAt(1f));
            Assert.Less(IntroHeistService.AlertAt(15.6f), IntroHeistService.AlertAt(15.4f), "A Lábia derruba o alerta.");
            Assert.AreEqual(100f, IntroHeistService.AlertAt(IntroHeistService.SirenAt));
        }

        [Test]
        public void Intro_ClockKeepsGoingAfterTheNewspaper_ForTheBipAndTheEnd()
        {
            PlayToTheEnd();
            for (int i = 0; i < 400 && intro.Time < IntroHeistService.Length; i++) intro.Tick(0.05f);
            Assert.GreaterOrEqual(intro.Time, IntroHeistService.Length, "Sem o relógio o diretor nunca manda o bip nem encerra.");
            Assert.AreEqual(HeistPhase.Finished, intro.Phase);
        }

        [Test]
        public void Intro_SpeedFromTheHud_PlaysFaster()
        {
            intro.StartHeist(def.heist, intro.Crew);
            intro.Speed = 2f;
            intro.Tick(0.5f);
            Assert.AreEqual(1f, intro.Time, 0.001f);
        }
    }
}
