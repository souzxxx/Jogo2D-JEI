using System.Collections.Generic;
using UnityEngine;

namespace AFirma.Core
{
    /// <summary>
    /// Registro central dos serviços e do estado do jogo.
    ///
    /// Como plugar o seu serviço real (ex.: Nicolas trocando o MockEconomyService):
    ///
    ///     static class EconomyInstaller
    ///     {
    ///         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    ///         static void Install() => Services.Register((IEconomyService)new EconomyService());
    ///     }
    ///
    /// Tudo que não foi registrado até o BeforeSceneLoad vira mock automaticamente,
    /// então qualquer cena dá Play sozinha.
    /// </summary>
    public static class Services
    {
        public static GameDatabase Db { get; private set; }
        public static GameState State { get; private set; }

        public static ISaveService Save { get; private set; }
        public static IEconomyService Economy { get; private set; }
        public static IProgressionService Progression { get; private set; }
        public static ICrewService Crew { get; private set; }
        public static IHeistService Heist { get; private set; }
        public static IAudioService Audio { get; private set; }

        public static bool IsInitialized { get; private set; }

        public static GameConfig Config => Db != null ? Db.config : null;

        public static void Register(ISaveService service) => Save = service;
        public static void Register(IEconomyService service) => Economy = service;
        public static void Register(IProgressionService service) => Progression = service;
        public static void Register(ICrewService service) => Crew = service;
        public static void Register(IHeistService service) => Heist = service;
        public static void Register(IAudioService service) => Audio = service;

        static readonly System.Random Rng = new System.Random();

        /// <summary>Carrega dados e save, completa com mocks o que faltar. Pode ser chamado várias vezes.</summary>
        public static void Initialize()
        {
            if (IsInitialized) return;

            if (Db == null) Db = Resources.Load<GameDatabase>(GameDatabase.ResourcePath);
            if (Db == null)
            {
                Debug.LogError("[A Firma] GameDatabase não encontrado em Resources. Rode 'A Firma > Setup > Criar dados de exemplo'.");
                Db = ScriptableObject.CreateInstance<GameDatabase>();
                Db.config = ScriptableObject.CreateInstance<GameConfig>();
            }

            Save ??= new PlayerPrefsSaveService();
            Economy ??= new MockEconomyService();
            Progression ??= new MockProgressionService();
            Crew ??= new MockCrewService();
            Heist ??= new MockHeistService();
            Audio ??= new MockAudioService();

            State = Save.Load(out var notice);
            if (State == null) State = GameState.CreateNew(Db, GameClock.NowUnix, Rng);

            IsInitialized = true;
            if (!string.IsNullOrEmpty(notice)) GameEvents.RaiseNotice(notice);
            GameEvents.RaiseGameLoaded();
        }

        /// <summary>Novo jogo: apaga o save atual e começa do zero.</summary>
        public static void StartNewGame()
        {
            State = GameState.CreateNew(Db, GameClock.NowUnix, Rng);
            SaveNow();
            GameEvents.RaiseNewGameStarted();
        }

        /// <summary>Troca o estado inteiro (usado pelo prestígio e pelos testes).</summary>
        public static void ReplaceState(GameState state) => State = state;

        /// <summary>Usado pelos testes pra rodar com um banco de dados próprio.</summary>
        public static void UseDatabase(GameDatabase db) => Db = db;

        public static void SaveNow()
        {
            if (State == null || Save == null) return;
            State.lastSaveUnix = GameClock.NowUnix;
            Save.Save(State);
            GameEvents.RaiseGameSaved();
        }

        public static IEnumerable<ITickable> Tickables()
        {
            if (Economy is ITickable e) yield return e;
            if (Progression is ITickable p) yield return p;
            if (Crew is ITickable c) yield return c;
            if (Heist is ITickable h) yield return h;
            if (Audio is ITickable a) yield return a;
        }

        /// <summary>Zera tudo (início do Play e testes).</summary>
        public static void ResetAll()
        {
            Db = null; State = null;
            Save = null; Economy = null; Progression = null; Crew = null; Heist = null; Audio = null;
            IsInitialized = false;
        }
    }
}
