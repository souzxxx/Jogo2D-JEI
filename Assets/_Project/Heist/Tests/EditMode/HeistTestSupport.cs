using System;
using System.Collections.Generic;
using AFirma.Core;
using AFirma.Heist;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AFirma.Tests
{
    /// <summary>Monta golpes e crews de teste com números conhecidos.</summary>
    public static class HeistTestSupport
    {
        static readonly List<Object> Created = new List<Object>();

        /// <summary>Mocks de Crew/Economia/Progressão + HeistRunner real. Sem evento sorteado e com o tutorial já feito.</summary>
        public static HeistRunner InitWithRunner(int seed = 1)
        {
            InitWithoutRandomEvents();
            var runner = new HeistRunner(new System.Random(seed));
            Services.Register((IHeistService)runner);
            return runner;
        }

        public static void InitWithoutRandomEvents()
        {
            TestSupport.InitServicesWithMocks();
            // Cópia do banco e da config pra mexer nos números sem tocar nos assets do projeto.
            var db = Track(Object.Instantiate(Services.Db));
            db.config = Track(Object.Instantiate(db.config));
            db.config.eventChancePerRoom = 0f;
            Services.UseDatabase(db);
            Services.State.tutorialDone = true;
        }

        public static void Teardown()
        {
            TestSupport.Teardown();
            foreach (var obj in Created)
                if (obj != null) Object.DestroyImmediate(obj);
            Created.Clear();
        }

        public static HeistDef MakeHeist(params RoomData[] rooms)
        {
            var heist = Track(ScriptableObject.CreateInstance<HeistDef>());
            heist.id = "teste";
            heist.displayName = "Golpe de teste";
            heist.tier = 1;
            heist.unlockedByDefault = true;
            heist.jailSeconds = 60f;
            heist.rooms.AddRange(rooms);
            return heist;
        }

        /// <summary>Sala com alerta baixo de propósito, pra sirene não tocar sem o teste pedir.</summary>
        public static RoomData Room(ObstacleType obstacle, float difficulty = 100f, double loot = 100, float alertPerSecond = 0.1f) =>
            new RoomData { name = obstacle.ToString(), obstacle = obstacle, difficulty = difficulty, loot = loot, alertPerSecond = alertPerSecond };

        /// <summary>Ladrão novo na crew, disponível. Sem manha, a não ser que o teste passe uma.</summary>
        public static ThiefState AddThief(ThiefType type, float skill, string traitId = null)
        {
            var thief = new ThiefState
            {
                id = "teste-" + Services.State.crew.Count,
                firstName = "Zé",
                nickname = type.ToString(),
                type = type,
                skill = skill,
                stealth = 30f,
                nerves = 30f,
                traitId = traitId,
                status = ThiefStatus.Available
            };
            Services.State.crew.Add(thief);
            return thief;
        }

        public static List<ThiefState> Crew(params ThiefState[] thieves) => new List<ThiefState>(thieves);

        /// <summary>Evento de teste que cabe em qualquer sala do tier 1.</summary>
        public static HeistEventDef MakeEvent(string id, params EventChoice[] choices)
        {
            var e = Track(ScriptableObject.CreateInstance<HeistEventDef>());
            e.id = id;
            e.displayName = id;
            e.body = "Evento de teste.";
            e.choices.AddRange(choices);
            return e;
        }

        /// <summary>Escolha que sempre dá certo, sem requisito.</summary>
        public static EventChoice SafeChoice(EventEffect success = null) =>
            new EventChoice { label = "Seguro", success = success ?? new EventEffect { resultText = "Deu certo." } };

        /// <summary>Troca a habilidade de um tipo de ladrão numa cópia do ThiefTypeDef (o asset do projeto fica intacto).</summary>
        public static void GiveAbility(ThiefType type, ThiefAbilityDef ability)
        {
            var types = Services.Db.thiefTypes;
            int index = types.FindIndex(t => t != null && t.type == type);
            var copy = Track(Object.Instantiate(types[index]));
            copy.ability = ability;
            types[index] = copy;
        }

        /// <summary>Só esses eventos entram no sorteio, e toda sala tem evento (chance 100%).</summary>
        public static void OnlyTheseEvents(params HeistEventDef[] events)
        {
            Services.Db.events.Clear();
            Services.Db.events.AddRange(events);
            Services.Config.eventChancePerRoom = 1f;
        }

        public static void Tick(float seconds) => ((ITickable)Services.Heist).Tick(seconds);

        /// <summary>Tica até a condição valer. Retorna quantos segundos de jogo passaram.</summary>
        public static float TickUntil(Func<bool> condition, float step = 0.05f, float maxSeconds = 300f)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < maxSeconds)
            {
                Tick(step);
                elapsed += step;
            }
            return elapsed;
        }

        /// <summary>Joga o golpe até o fim sempre entrando na próxima sala.</summary>
        public static void RunToTheEnd()
        {
            for (int i = 0; i < 20000 && Services.Heist.IsRunning; i++)
            {
                switch (Services.Heist.Phase)
                {
                    case HeistPhase.AwaitingDoor: Services.Heist.ChooseDoor(true); break;
                    case HeistPhase.AwaitingEvent: Services.Heist.ChooseEventOption(FirstAvailableChoice()); break;
                    default: Tick(0.25f); break;
                }
            }
        }

        public static int FirstAvailableChoice()
        {
            var choices = Services.Heist.PendingEvent.choices;
            for (int i = 0; i < choices.Count; i++)
                if (choices[i].available) return i;
            return 0;
        }

        static T Track<T>(T obj) where T : Object
        {
            Created.Add(obj);
            return obj;
        }
    }
}
