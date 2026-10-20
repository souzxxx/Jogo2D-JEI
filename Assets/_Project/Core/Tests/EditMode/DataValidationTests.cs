using System.Collections.Generic;
using AFirma.Core;
using NUnit.Framework;
using UnityEngine;

namespace AFirma.Tests
{
    /// <summary>Pega erro de conteúdo antes de virar bug no jogo. Rode depois de mexer em qualquer Def.</summary>
    public class DataValidationTests
    {
        GameDatabase db;

        [SetUp]
        public void SetUp() => db = Resources.Load<GameDatabase>(GameDatabase.ResourcePath);

        [Test]
        public void Database_ExistsWithConfig()
        {
            Assert.IsNotNull(db, "GameDatabase não está em Resources.");
            Assert.IsNotNull(db.config);
            Assert.IsNotNull(db.names);
            Assert.IsNotNull(db.headlines);
        }

        [Test]
        public void ThiefTypes_AreTheFourWithAbilityAndSideJob()
        {
            Assert.AreEqual(4, db.thiefTypes.Count);
            var seen = new HashSet<ThiefType>();
            foreach (var t in db.thiefTypes)
            {
                Assert.IsNotNull(t);
                Assert.IsTrue(seen.Add(t.type), "Tipo repetido: " + t.type);
                Assert.IsNotNull(t.ability, t.displayName + " sem habilidade.");
                Assert.IsNotNull(t.signatureSideJob, t.displayName + " sem bico.");
            }
        }

        [Test]
        public void Heists_HaveRoomsAndEndInVault()
        {
            Assert.GreaterOrEqual(db.heists.Count, 5);
            foreach (var h in db.heists)
            {
                Assert.IsNotNull(h);
                Assert.GreaterOrEqual(h.rooms.Count, 3, h.displayName);
                Assert.AreEqual(RoomKind.Vault, h.rooms[h.rooms.Count - 1].kind, h.displayName + ": última sala tem que ser o cofre.");
                Assert.IsTrue(h.unlockedByDefault || h.unlockedBy != null, h.displayName + " nunca é liberado.");
                foreach (var room in h.rooms)
                {
                    Assert.Greater(room.difficulty, 0f, h.displayName + "/" + room.name);
                    Assert.Greater(room.loot, 0d, h.displayName + "/" + room.name);
                }
            }
        }

        [Test]
        public void Events_HaveAtLeastTwoChoices_AndOneTutorial()
        {
            int tutorials = 0;
            foreach (var e in db.events)
            {
                Assert.IsNotNull(e);
                Assert.GreaterOrEqual(e.choices.Count, 2, e.displayName);
                if (e.tutorialOnly) tutorials++;
            }
            Assert.AreEqual(1, tutorials);
        }

        [Test]
        public void Events_AlwaysHaveAChoiceWithoutRequirement()
        {
            foreach (var e in db.events)
                Assert.IsTrue(e.choices.Exists(c => !c.requiresType), e.displayName + ": toda crew precisa ter pelo menos uma saída.");
        }

        [Test]
        public void Headlines_FillPlaceholders()
        {
            var result = new HeistResult { outcome = HeistOutcome.CleanEscape, moneyGained = 1500 };
            var line = db.headlines.Pick(result, "Lotérica do Seu Zé", new System.Random(1));
            StringAssert.DoesNotContain("{golpe}", line);
            StringAssert.DoesNotContain("{valor}", line);

            result.arrestedThiefIds.Add("x");
            var arrested = db.headlines.Pick(result, "Lotérica", new System.Random(1));
            CollectionAssert.Contains(db.headlines.withArrests, arrested);
        }

        [Test]
        public void Ids_AreUniqueAndFilled()
        {
            AssertUnique(db.traits);
            AssertUnique(db.heists);
            AssertUnique(db.events);
            AssertUnique(db.upgrades);
            AssertUnique(db.sideJobs);
            AssertUnique(db.modifiers);
            AssertUnique(db.famePerks);
        }

        static void AssertUnique<T>(List<T> defs) where T : DefBase
        {
            var ids = new HashSet<string>();
            foreach (var def in defs)
            {
                Assert.IsNotNull(def, typeof(T).Name + " nulo na lista.");
                Assert.IsFalse(string.IsNullOrEmpty(def.id), def.name + " sem id.");
                Assert.IsTrue(ids.Add(def.id), "Id repetido: " + def.id);
            }
        }
    }
}
