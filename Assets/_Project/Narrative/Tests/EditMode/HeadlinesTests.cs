using System.Collections.Generic;
using AFirma.Core;
using NUnit.Framework;
using UnityEngine;

namespace AFirma.Tests
{
    /// <summary>Manchetes do jornal (ESCOPO 6.5): o Bastos sobe de cargo a cada prestígio e cada golpe tem as suas manchetes.</summary>
    public class HeadlinesTests
    {
        HeadlineSetDef headlines;
        HeistDef loterica;

        [SetUp]
        public void SetUp()
        {
            TestSupport.InitServicesWithMocks();
            headlines = ScriptableObject.CreateInstance<HeadlineSetDef>();
            headlines.clean = new[] { "limpo" };
            headlines.hasty = new[] { "apressada" };
            headlines.withArrests = new[] { "preso" };
            headlines.bastos = new List<BastosRank>
            {
                new BastosRank { title = "DELEGADO BASTOS", quotes = new[] { "É QUESTÃO DE DIAS" } },
                new BastosRank { title = "SUPERINTENDENTE BASTOS", quotes = new[] { "PROMOÇÃO NÃO ME FEZ ESQUECER" } }
            };
            loterica = ScriptableObject.CreateInstance<HeistDef>();
            loterica.id = "loterica";
            headlines.byHeist.Add(new HeistHeadlines { heist = loterica, completed = new[] { "seu zé" } });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(loterica);
            Object.DestroyImmediate(headlines);
            TestSupport.Teardown();
        }

        static HeistResult Result(HeistOutcome outcome, bool completed = false, string heistId = "loterica") =>
            new HeistResult { heistId = heistId, outcome = outcome, completed = completed, moneyGained = 1500 };

        [Test]
        public void Pick_BastosTitleAndQuote_FollowTheWantedLevel()
        {
            headlines.clean = new[] { "{bastos}: \"{fala}\"" };
            var result = Result(HeistOutcome.CleanEscape);

            Assert.AreEqual("DELEGADO BASTOS: \"É QUESTÃO DE DIAS\"", headlines.Pick(result, "Lotérica", new System.Random(1), 0));
            Assert.AreEqual("SUPERINTENDENTE BASTOS: \"PROMOÇÃO NÃO ME FEZ ESQUECER\"", headlines.Pick(result, "Lotérica", new System.Random(1), 1));
            Assert.AreEqual("SUPERINTENDENTE BASTOS: \"PROMOÇÃO NÃO ME FEZ ESQUECER\"", headlines.Pick(result, "Lotérica", new System.Random(1), 7),
                "Passou do último cargo, fica o último.");
        }

        [Test]
        public void Pick_WithoutWantedLevel_UsesTheSave()
        {
            headlines.clean = new[] { "{bastos}" };
            Services.State.wantedLevel = 1;

            Assert.AreEqual("SUPERINTENDENTE BASTOS", headlines.Pick(Result(HeistOutcome.CleanEscape), "Lotérica", new System.Random(1)));
        }

        [Test]
        public void Pick_WithoutRanks_StillFillsEverything()
        {
            headlines.bastos.Clear();
            headlines.clean = new[] { "{bastos}{fala} {golpe} {valor}" };

            Assert.AreEqual("BASTOS LOTÉRICA Cr$ 1.500", headlines.Pick(Result(HeistOutcome.CleanEscape), "Lotérica", new System.Random(1), 0));
        }

        [Test]
        public void PoolFor_VaultClearedClean_AddsTheHeistsOwnHeadlines()
        {
            CollectionAssert.AreEquivalent(new[] { "limpo", "seu zé" }, headlines.PoolFor(Result(HeistOutcome.CleanEscape, completed: true)));
            CollectionAssert.AreEquivalent(new[] { "limpo" }, headlines.PoolFor(Result(HeistOutcome.CleanEscape, completed: false)),
                "Meteu o pé antes do cofre: só as gerais.");
            CollectionAssert.AreEquivalent(new[] { "apressada" }, headlines.PoolFor(Result(HeistOutcome.HastyEscape, completed: true)),
                "Cofre limpo com sirene fica com as da fuga.");
            CollectionAssert.AreEquivalent(new[] { "limpo" }, headlines.PoolFor(Result(HeistOutcome.CleanEscape, completed: true, heistId: "joalheria")));
        }

        [Test]
        public void PoolFor_Arrests_ComeFirst()
        {
            var result = Result(HeistOutcome.CleanEscape, completed: true);
            result.arrestedThiefIds.Add("ze");

            CollectionAssert.AreEqual(new[] { "preso" }, headlines.PoolFor(result));
        }

        [Test]
        public void PoolFor_EmptyOutcome_FallsBackToClean()
        {
            CollectionAssert.AreEqual(new[] { "limpo" }, headlines.PoolFor(Result(HeistOutcome.ForcedEscape)));
            Assert.IsEmpty(headlines.PoolFor(null));
        }
    }
}
