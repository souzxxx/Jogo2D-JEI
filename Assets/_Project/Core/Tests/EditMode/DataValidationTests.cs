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

        static readonly string[] ThiefAnimations = { "idle", "andar", "trabalhar", "comemorar" };

        /// <summary>Quem já tem arte segue o padrão do ASSETS.md. Ladrão sem arte ainda usa o placeholder da cena.</summary>
        [Test]
        public void ThiefArt_WhenFilled_HasPortrait64AndTheFourAnimations()
        {
            foreach (var t in db.thiefTypes)
            {
                if (t.portrait != null)
                {
                    Assert.AreEqual(64f, t.portrait.rect.width, t.displayName + ": retrato é 64x64.");
                    Assert.AreEqual(64f, t.portrait.rect.height, t.displayName + ": retrato é 64x64.");
                }
                if (t.animator == null) continue;
                var controller = t.animator as UnityEditor.Animations.AnimatorController;
                Assert.IsNotNull(controller, t.displayName + ": o animator tem que ser um Animator Controller.");
                var states = controller.layers[0].stateMachine.states;
                foreach (var anim in ThiefAnimations)
                {
                    var state = System.Array.Find(states, s => s.state.name == anim);
                    Assert.IsNotNull(state.state, t.displayName + ": falta o estado '" + anim + "'.");
                    var clip = state.state.motion as AnimationClip;
                    Assert.IsNotNull(clip, t.displayName + "/" + anim + " sem clipe.");
                    var curves = UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(clip);
                    Assert.AreEqual(1, curves.Length, t.displayName + "/" + anim + ": o clipe troca o sprite do SpriteRenderer.");
                    foreach (var key in UnityEditor.AnimationUtility.GetObjectReferenceCurve(clip, curves[0]))
                        Assert.IsNotNull(key.value as Sprite, t.displayName + "/" + anim + ": frame sem sprite.");
                }
                Assert.AreEqual("idle", controller.layers[0].stateMachine.defaultState.name, t.displayName);
            }
        }

        [Test]
        public void ConArtist_HasArt()
        {
            var conArtist = db.thiefTypes.Find(t => t.type == ThiefType.ConArtist);
            Assert.IsNotNull(conArtist.portrait, "171 sem retrato.");
            Assert.IsNotNull(conArtist.animator, "171 sem animator.");
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
        public void Events_HaveAtMostThreeChoices_EachWithLabelAndResultText()
        {
            foreach (var e in db.events)
            {
                Assert.LessOrEqual(e.choices.Count, 3, e.displayName + ": o cartão cabe 2 ou 3 escolhas.");
                foreach (var c in e.choices)
                {
                    Assert.IsNotEmpty(c.label, e.displayName + ": escolha sem texto.");
                    Assert.IsNotEmpty(c.success.resultText, e.displayName + " / " + c.label + ": falta o texto do sucesso.");
                    // Com atributo baixo a chance fica no baseChance: abaixo de 100% pode dar errado.
                    if (c.baseChance < 1f)
                        Assert.IsNotEmpty(c.failure.resultText, e.displayName + " / " + c.label + ": pode dar errado, falta o texto da falha.");
                }
            }
        }

        [Test]
        public void Headlines_FillPlaceholders()
        {
            var result = new HeistResult { outcome = HeistOutcome.CleanEscape, moneyGained = 1500 };
            for (int wanted = 0; wanted <= 4; wanted++)
                for (int seed = 0; seed < 20; seed++)
                {
                    var line = db.headlines.Pick(result, "Lotérica do Seu Zé", new System.Random(seed), wanted);
                    StringAssert.DoesNotContain("{", line, "Placeholder sem preencher.");
                }

            result.arrestedThiefIds.Add("x");
            CollectionAssert.AreEqual(db.headlines.withArrests, db.headlines.PoolFor(result), "Prisão tem prioridade.");
        }

        [Test]
        public void Headlines_UseOnlyKnownPlaceholders_AndHaveAtLeastThreePerResult()
        {
            var known = new[] { "{golpe}", "{valor}", "{bastos}", "{fala}" };
            var h = db.headlines;
            var pools = new[] { h.clean, h.hasty, h.forced, h.perfect, h.withArrests, h.interrupted };
            var all = new System.Collections.Generic.List<string>();
            foreach (var pool in pools)
            {
                Assert.GreaterOrEqual(pool.Length, 3, "Cada resultado precisa de pelo menos 3 manchetes.");
                all.AddRange(pool);
            }
            foreach (var own in h.byHeist)
            {
                Assert.IsNotNull(own.heist, "Manchete própria sem golpe.");
                all.AddRange(own.completed);
            }
            foreach (var rank in h.bastos)
            {
                Assert.IsNotEmpty(rank.title, "Cargo do Bastos sem nome.");
                Assert.IsNotEmpty(rank.quotes, rank.title + " sem fala.");
            }

            foreach (var line in all)
            {
                var rest = line;
                foreach (var placeholder in known) rest = rest.Replace(placeholder, "");
                StringAssert.DoesNotContain("{", rest, "Placeholder desconhecido em: " + line);
            }
        }

        [Test]
        public void ExtraHeists_BranchOffTheLadder_AndNeverReplaceTheTop()
        {
            var extras = db.heists.FindAll(h => h != null && h.extra);
            Assert.AreEqual(3, extras.Count, "Os 3 golpes extras (ESCOPO 6.9).");

            HeistDef top = null;
            foreach (var h in db.heists)
                if (h != null && !h.extra && (top == null || h.tier > top.tier)) top = h;
            Assert.AreEqual("banco-central", top.id, "O topo da escada (prestígio) é o Banco Central.");

            foreach (var h in extras)
            {
                Assert.IsFalse(h.unlockedByDefault, h.displayName + ": extra libera com um golpe da escada.");
                Assert.IsNotNull(h.unlockedBy, h.displayName + " nunca é liberado.");
                Assert.IsFalse(h.unlockedBy.extra, h.displayName + ": extra sai de um golpe da escada, não de outro extra.");
                Assert.IsNotEmpty(h.briefing, h.displayName + " sem briefing.");
                Assert.IsTrue(db.headlines.byHeist.Exists(x => x.heist == h), h.displayName + " sem manchete própria.");
            }
        }

        [Test]
        public void FirstHeist_IsNeverLocked()
        {
            var first = db.heists.Find(h => h != null && !h.extra && h.tier == 1);
            Assert.IsNotNull(first, "Falta o golpe de tier 1.");
            Assert.IsTrue(first.unlockedByDefault, first.displayName + " nunca pode ficar bloqueado (anti dead-end).");
        }

        [Test]
        public void Traits_AreEight_AndTheDescriptionShowsEveryEffect()
        {
            Assert.AreEqual(8, db.traits.Count);
            foreach (var trait in db.traits)
            {
                Assert.IsNotEmpty(trait.description, trait.displayName);
                var m = trait.modifiers;
                foreach (var mult in new[] { m.skillMult, m.stealthMult, m.nervesMult, m.lootMult, m.personRoomLootMult, m.alertMult, m.xpMult, m.bailMult, m.badEventChanceMult })
                {
                    if (Mathf.Approximately(mult, 1f)) continue;
                    int percent = Mathf.RoundToInt((mult - 1f) * 100f);
                    string shown = (percent > 0 ? "+" : "-") + Mathf.Abs(percent) + "%";
                    StringAssert.Contains(shown, trait.description, trait.displayName + ": a ficha tem que mostrar o número real.");
                }
            }
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
