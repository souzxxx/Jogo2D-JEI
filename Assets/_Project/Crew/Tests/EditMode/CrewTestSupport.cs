using AFirma.Core;
using AFirma.Crew;

namespace AFirma.Tests
{
    /// <summary>Mocks de Economia/Progressão/Golpe + CrewService real, com relógio falso.</summary>
    public static class CrewTestSupport
    {
        public static long Now = 1000;

        public static CrewService InitWithCrew(int seed = 1)
        {
            TestSupport.InitServicesWithMocks();
            Now = 1000;
            GameClock.UnixNowProvider = () => Now;
            var crew = new CrewService(new System.Random(seed));
            Services.Register((ICrewService)crew);
            return crew;
        }

        public static void Teardown() => TestSupport.Teardown();

        /// <summary>Ladrão novo na crew, disponível, com Perícia 50, Discrição 40 e Nervos 30.</summary>
        public static ThiefState Thief(ThiefType type, string traitId = null, int level = 1)
        {
            var thief = new ThiefState
            {
                id = "teste-" + Services.State.crew.Count,
                firstName = "Zé",
                nickname = type.ToString(),
                type = type,
                level = level,
                skill = 50f,
                stealth = 40f,
                nerves = 30f,
                traitId = traitId,
                status = ThiefStatus.Available
            };
            Services.State.crew.Add(thief);
            return thief;
        }
    }
}
