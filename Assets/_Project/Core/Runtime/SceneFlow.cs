using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace AFirma.Core
{
    /// <summary>Navegação entre as cenas principais. Ninguém chama SceneManager direto.</summary>
    public static class SceneFlow
    {
        public const string Boot = "Boot";
        public const string MainMenu = "MainMenu";
        public const string Hideout = "Hideout";
        public const string Heist = "Heist";

        static HeistDef pendingHeist;
        static List<ThiefState> pendingCrew;

        public static void GoToMainMenu() => Load(MainMenu);
        public static void GoToHideout() => Load(Hideout);

        /// <summary>Planejamento chama isto. O golpe começa quando a cena Heist termina de carregar (HeistSceneBootstrap).</summary>
        public static void GoToHeist(HeistDef heist, IEnumerable<ThiefState> crew)
        {
            pendingHeist = heist;
            pendingCrew = new List<ThiefState>(crew);
            Load(Heist);
        }

        public static bool TryConsumePendingHeist(out HeistDef heist, out List<ThiefState> crew)
        {
            heist = pendingHeist;
            crew = pendingCrew;
            pendingHeist = null;
            pendingCrew = null;
            return heist != null && crew != null;
        }

        public static void Reset()
        {
            pendingHeist = null;
            pendingCrew = null;
        }

        static void Load(string sceneName)
        {
            GameClock.SetPaused(false);
            Services.SaveNow();
            SceneManager.LoadScene(sceneName);
        }
    }
}
