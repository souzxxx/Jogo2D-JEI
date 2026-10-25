using System.Collections.Generic;
using AFirma.Core;
using AFirma.Narrative;
using NUnit.Framework;
using UnityEngine;

namespace AFirma.Tests
{
    /// <summary>Bips da Dona Cida nos marcos (ESCOPO 5): cada um uma vez por save, na ordem certa e esperando a pausa.</summary>
    public class DonaCidaBipsTests
    {
        DonaCidaBips bips;
        BipSetDef texts;
        readonly List<KeyValuePair<string, string>> sent = new List<KeyValuePair<string, string>>();

        HeistDef Loterica => Services.Db.GetHeist("loterica");
        HeistDef Joalheria => Services.Db.GetHeist("joalheria");
        HeistDef BancoCentral => Services.Db.GetHeist("banco-central");

        [SetUp]
        public void SetUp()
        {
            TestSupport.InitServicesWithMocks();
            texts = ScriptableObject.CreateInstance<BipSetDef>();
            texts.newGame = "novo";
            texts.firstHeistCompleted = "primeiro golpe";
            texts.firstHeistFled = "meteu o pé";
            texts.firstSiren = "sirene";
            texts.firstArrest = "pegaram o {nome}";
            texts.prestigeUnlocked = "prestígio";
            texts.heistUnlocked.Add(new HeistBip { heist = Joalheria, text = "joalheria" });
            bips = new DonaCidaBips(texts);
            sent.Clear();
            GameEvents.Bip += (sender, message) => sent.Add(new KeyValuePair<string, string>(sender, message));
        }

        [TearDown]
        public void TearDown()
        {
            bips.Dispose();
            Object.DestroyImmediate(texts);
            TestSupport.Teardown();
        }

        List<string> Messages()
        {
            bips.Flush();
            return sent.ConvertAll(b => b.Value);
        }

        static HeistResult Result(HeistDef heist, bool completed) => new HeistResult { heistId = heist.id, completed = completed };

        [Test]
        public void NewGame_BipsOnce_FromDonaCida()
        {
            GameEvents.RaiseNewGameStarted();
            GameEvents.RaiseGameLoaded();

            CollectionAssert.AreEqual(new[] { "novo" }, Messages());
            Assert.AreEqual(Services.Config.mentorName, sent[0].Key);
        }

        [Test]
        public void LoadedSave_WithHeistsDone_DoesNotWelcomeAgain()
        {
            Services.State.completedHeists.Add("loterica");
            GameEvents.RaiseGameLoaded();

            CollectionAssert.IsEmpty(Messages());
        }

        [Test]
        public void Bips_WaitInTheQueue_UntilFlush()
        {
            GameEvents.RaiseSirenStarted(10f);

            Assert.IsEmpty(sent, "Bip só sai no Flush, com a tela já ouvindo.");
            Assert.AreEqual(1, bips.Pending);
            CollectionAssert.AreEqual(new[] { "sirene" }, Messages());
        }

        [Test]
        public void Bips_WaitWhilePaused()
        {
            GameEvents.RaiseSirenStarted(10f);
            GameClock.SetPaused(true);
            bips.Flush();
            Assert.IsEmpty(sent);

            GameClock.SetPaused(false);
            CollectionAssert.AreEqual(new[] { "sirene" }, Messages());
        }

        [Test]
        public void FirstSirenAndFirstArrest_BipOnlyTheFirstTime()
        {
            var thief = Services.State.crew[0];
            GameEvents.RaiseSirenStarted(10f);
            GameEvents.RaiseSirenStarted(10f);
            GameEvents.RaiseThiefJailed(thief);
            GameEvents.RaiseThiefJailed(Services.State.crew[1]);

            CollectionAssert.AreEqual(new[] { "sirene", "pegaram o " + thief.DisplayName }, Messages());
        }

        [Test]
        public void FirstHeist_CompletedOrFled_HasItsOwnText_AndOnlyOnce()
        {
            GameEvents.RaiseHeistFinished(Result(Loterica, completed: false));
            GameEvents.RaiseHeistFinished(Result(Loterica, completed: true));

            CollectionAssert.AreEqual(new[] { "meteu o pé" }, Messages());
        }

        [Test]
        public void HeistUnlocked_DuringTheHeist_WaitsForTheResultBip()
        {
            Services.Heist.StartHeist(Loterica, Services.State.crew);
            Assume.That(Services.Heist.IsRunning);

            GameEvents.RaiseHeistUnlocked(Joalheria);
            bips.Flush();
            Assert.IsEmpty(sent, "Golpe liberado no meio do golpe espera o resultado.");

            GameEvents.RaiseHeistFinished(Result(Loterica, completed: true));
            CollectionAssert.AreEqual(new[] { "primeiro golpe", "joalheria" }, Messages());
        }

        [Test]
        public void HeistUnlocked_OutsideAHeist_BipsRightAway_OncePerHeist()
        {
            GameEvents.RaiseHeistUnlocked(Joalheria);
            GameEvents.RaiseHeistUnlocked(Joalheria);
            GameEvents.RaiseHeistUnlocked(Services.Db.GetHeist("cambio")); // sem texto: não vira bip

            CollectionAssert.AreEqual(new[] { "joalheria" }, Messages());
        }

        [Test]
        public void LastHeistCompleted_UnlocksPrestigeBip()
        {
            Services.State.completedHeists.Add("loterica");
            GameEvents.RaiseHeistFinished(Result(BancoCentral, completed: true));

            CollectionAssert.Contains(Messages(), "prestígio");
        }

        [Test]
        public void LastHeistFled_DoesNotUnlockPrestige()
        {
            GameEvents.RaiseHeistFinished(Result(BancoCentral, completed: false));

            CollectionAssert.DoesNotContain(Messages(), "prestígio");
        }

        [Test]
        public void SeenBips_AreSavedInTheState()
        {
            GameEvents.RaiseSirenStarted(10f);

            CollectionAssert.Contains(Services.State.seenTips, DonaCidaBips.SeenPrefix + "primeira-sirene");
        }

        [Test]
        public void NewGame_ClearsWhatWasQueuedForTheOldSave()
        {
            Services.State.completedHeists.Add("loterica");
            GameEvents.RaiseSirenStarted(10f);

            Services.StartNewGame(); // estado novo, fila do save antigo não vale mais

            CollectionAssert.AreEqual(new[] { "novo" }, Messages());
        }
    }
}
