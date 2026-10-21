using System.Collections.Generic;
using AFirma.Core;
using AFirma.Crew;
using AFirma.Heist;
using NUnit.Framework;
using static AFirma.Tests.CrewTestSupport;

namespace AFirma.Tests
{
    /// <summary>As 8 manhas com efeito real, uma por teste, onde cada uma pesa (ESCOPO 6.3). Crew real + regras reais do golpe.</summary>
    public class TraitEffectsTests
    {
        CrewService crew;

        [SetUp]
        public void SetUp() => crew = InitWithCrew();

        [TearDown]
        public void TearDown() => Teardown();

        [Test]
        public void Fominha_MoreLoot_LessStealth()
        {
            var thief = Thief(ThiefType.Locksmith, "fominha");
            Assert.AreEqual(120.0, HeistRules.RoomLoot(Room(ObstacleType.Lock), new HeistContext(), Team(thief), Services.Db)); // +20%
            Assert.AreEqual(34f, crew.GetStealth(thief), 0.001f); // 40 × 0,85
        }

        [Test]
        public void PeFrio_MoreEvents()
        {
            float normal = EventRules.EventChance(Team(Thief(ThiefType.Locksmith)), Services.Db);
            float peFrio = EventRules.EventChance(Team(Thief(ThiefType.Locksmith, "pe-frio")), Services.Db);
            Assert.AreEqual(System.Math.Min(1f, normal * 1.5f), peFrio, 0.001f); // +50%
        }

        [Test]
        public void SangueFrio_MoreNerves()
        {
            Assert.AreEqual(39f, crew.GetNerves(Thief(ThiefType.Locksmith, "sangue-frio")), 0.001f); // 30 × 1,3
        }

        [Test]
        public void BocaAberta_MoreAlert()
        {
            var normal = Thief(ThiefType.Locksmith);
            var loud = Thief(ThiefType.Locksmith, "boca-aberta");
            var room = Room(ObstacleType.Lock);
            float normalRate = HeistRules.AlertPerSecond(room, 0, new HeistContext(), Team(normal), crew, Services.Db, 0f);
            float loudRate = HeistRules.AlertPerSecond(room, 0, new HeistContext(), Team(loud), crew, Services.Db, 0f);
            Assert.AreEqual(normalRate * 1.2f, loudRate, 0.001f); // +20%
        }

        [Test]
        public void PaoDuro_CheaperBail()
        {
            var thief = Thief(ThiefType.Locksmith, "pao-duro");
            crew.Jail(thief, 60f);
            Assert.AreEqual(72.0, crew.GetBailCost(thief)); // 60 s × Cr$ 2 × 0,6
        }

        [Test]
        public void CriaDaRua_MoreXp()
        {
            float normal = HeistRules.XpPerRoom(Thief(ThiefType.Locksmith), Services.Db);
            Assert.AreEqual(normal * 1.3f, HeistRules.XpPerRoom(Thief(ThiefType.Locksmith, "cria-da-rua"), Services.Db), 0.001f); // +30%
        }

        [Test]
        public void Supersticioso_MoreNerves_LessSkill()
        {
            var thief = Thief(ThiefType.Locksmith, "supersticioso");
            Assert.AreEqual(36f, crew.GetNerves(thief), 0.001f); // 30 × 1,2
            Assert.AreEqual(45f, crew.GetSkill(thief), 0.001f);  // 50 × 0,9
        }

        [Test]
        public void MaoBoba_MoreLootOnlyInPersonRooms()
        {
            var team = Team(Thief(ThiefType.ConArtist, "mao-boba"));
            Assert.AreEqual(130.0, HeistRules.RoomLoot(Room(ObstacleType.Person), new HeistContext(), team, Services.Db)); // +30%
            Assert.AreEqual(100.0, HeistRules.RoomLoot(Room(ObstacleType.Lock), new HeistContext(), team, Services.Db));
        }

        static List<ThiefState> Team(params ThiefState[] thieves) => new List<ThiefState>(thieves);

        static RoomData Room(ObstacleType obstacle) =>
            new RoomData { name = obstacle.ToString(), obstacle = obstacle, difficulty = 100, loot = 100, alertPerSecond = 2 };
    }
}
