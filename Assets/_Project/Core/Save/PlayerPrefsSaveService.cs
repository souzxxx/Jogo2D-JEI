using System;
using UnityEngine;

namespace AFirma.Core
{
    /// <summary>
    /// Save em JSON no PlayerPrefs (funciona no WebGL: vira IndexedDB no navegador).
    /// Sempre mantém o último save válido como backup.
    /// </summary>
    public class PlayerPrefsSaveService : ISaveService
    {
        readonly string mainKey;
        readonly string backupKey;

        public PlayerPrefsSaveService(string keyPrefix = "afirma.save")
        {
            mainKey = keyPrefix;
            backupKey = keyPrefix + ".backup";
        }

        public bool HasSave => PlayerPrefs.HasKey(mainKey);

        public GameState Load(out string notice)
        {
            notice = null;
            if (!PlayerPrefs.HasKey(mainKey) && !PlayerPrefs.HasKey(backupKey)) return null;

            var main = Parse(PlayerPrefs.GetString(mainKey, null));
            if (main != null) return main;

            var backup = Parse(PlayerPrefs.GetString(backupKey, null));
            if (backup != null)
            {
                notice = "O save deu problema, então carregamos o backup mais recente.";
                Debug.LogWarning("[A Firma] Save principal inválido. Backup carregado.");
                return backup;
            }

            notice = "Não deu pra ler o save. Começamos um jogo novo.";
            Debug.LogWarning("[A Firma] Save e backup inválidos. Jogo novo.");
            return null;
        }

        public void Save(GameState state)
        {
            if (state == null) return;
            var current = PlayerPrefs.GetString(mainKey, null);
            if (Parse(current) != null) PlayerPrefs.SetString(backupKey, current);

            state.version = GameState.CurrentVersion;
            PlayerPrefs.SetString(mainKey, JsonUtility.ToJson(state));
            PlayerPrefs.Save();
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(mainKey);
            PlayerPrefs.DeleteKey(backupKey);
            PlayerPrefs.Save();
        }

        static GameState Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var state = JsonUtility.FromJson<GameState>(json);
                if (state == null || state.version <= 0 || state.crew == null) return null;
                return Migrate(state);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[A Firma] Falha ao ler save: {e.Message}");
                return null;
            }
        }

        static GameState Migrate(GameState state)
        {
            // Quando o GameState.CurrentVersion subir, converta saves antigos aqui.
            if (state.version > GameState.CurrentVersion) return null;
            return state;
        }
    }
}
