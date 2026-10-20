using UnityEngine;

namespace AFirma.Core
{
    /// <summary>
    /// Objeto persistente criado automaticamente antes da primeira cena.
    /// Inicializa os serviços, chama Tick neles todo frame e cuida do autosave.
    /// </summary>
    public class GameLoop : MonoBehaviour
    {
        static GameLoop instance;
        float autosaveTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Services.ResetAll();
            GameEvents.ClearAll();
            GameClock.Reset();
            SceneFlow.Reset();
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            Services.Initialize();
            if (instance != null) return;
            var go = new GameObject("[GameLoop]");
            instance = go.AddComponent<GameLoop>();
            DontDestroyOnLoad(go);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            go.AddComponent<DebugOverlay>();
#endif
        }

        void Start()
        {
            // Golpe que ficou aberto quando o jogo fechou? Resolve agora (conta como "meteu o pé").
            if (Services.State != null && Services.State.hasActiveHeist && !Services.Heist.IsRunning)
                Services.Heist.RecoverInterruptedHeist();
        }

        void Update()
        {
            if (!Services.IsInitialized) return;

            float dt = Time.unscaledDeltaTime;
            if (!GameClock.Paused)
                foreach (var tickable in Services.Tickables())
                    tickable.Tick(dt);

            autosaveTimer += dt;
            float interval = Services.Config != null ? Services.Config.autosaveSeconds : 10f;
            if (autosaveTimer >= interval)
            {
                autosaveTimer = 0f;
                Services.SaveNow();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Services.SaveNow();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) Services.SaveNow();
        }

        void OnApplicationQuit() => Services.SaveNow();
    }
}
