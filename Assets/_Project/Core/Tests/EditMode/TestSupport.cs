using AFirma.Core;
using UnityEngine;

namespace AFirma.Tests
{
    /// <summary>Save em memória pros testes não mexerem no PlayerPrefs.</summary>
    public class MemorySaveService : ISaveService
    {
        string json;
        public int SaveCount { get; private set; }
        public bool HasSave => json != null;

        public GameState Load(out string notice)
        {
            notice = null;
            return json == null ? null : JsonUtility.FromJson<GameState>(json);
        }

        public void Save(GameState state)
        {
            json = JsonUtility.ToJson(state);
            SaveCount++;
        }

        public void DeleteSave() => json = null;
    }

    public static class TestSupport
    {
        /// <summary>Sobe os serviços com mocks, save em memória e o GameDatabase real.</summary>
        public static MemorySaveService InitServicesWithMocks()
        {
            GameEvents.ClearAll();
            GameClock.Reset();
            Services.ResetAll();
            var save = new MemorySaveService();
            Services.Register(save);
            Services.Initialize();
            return save;
        }

        public static void Teardown()
        {
            GameEvents.ClearAll();
            GameClock.Reset();
            Services.ResetAll();
        }
    }
}
