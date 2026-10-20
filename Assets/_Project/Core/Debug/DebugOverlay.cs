using UnityEngine;

namespace AFirma.Core
{
    /// <summary>
    /// Painel de debug (F1). Só existe no editor e em Development Build.
    /// Dá dinheiro, libera golpes, solta presos, controla o golpe sem precisar de UI pronta.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        bool visible;
        Rect window = new Rect(10, 10, 380, 560);
        Vector2 scroll;

        void OnGUI()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F1)
            {
                visible = !visible;
                e.Use();
            }

            if (!visible)
            {
                if (GUI.Button(new Rect(Screen.width - 92, 6, 86, 22), "Debug (F1)")) visible = true;
                return;
            }

            window = GUILayout.Window(91827, window, DrawWindow, "A Firma · Debug");
        }

        void DrawWindow(int id)
        {
            if (!Services.IsInitialized)
            {
                GUILayout.Label("Serviços não inicializados.");
                GUI.DragWindow();
                return;
            }

            var s = Services.State;
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label($"{Formulas.Money(s.money)} · Fama {s.fame} · Procurado {s.wantedLevel} · Renda {Services.Economy.IncomePerSecond:0.#}/s");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+Cr$ 1 mil")) Services.Economy.Add(1000, "debug");
            if (GUILayout.Button("+Cr$ 100 mil")) Services.Economy.Add(100000, "debug");
            if (GUILayout.Button("+10 Fama")) s.fame += 10;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Liberar golpes"))
                foreach (var heist in Services.Db.heists)
                    if (heist != null && !s.unlockedHeists.Contains(heist.id)) s.unlockedHeists.Add(heist.id);
            if (GUILayout.Button("Soltar presos"))
                foreach (var thief in s.crew)
                    if (thief.status == ThiefStatus.Jailed) Services.Crew.SetStatus(thief, ThiefStatus.Available);
            if (GUILayout.Button("Pausa: " + (GameClock.Paused ? "ON" : "off"))) GameClock.SetPaused(!GameClock.Paused);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Salvar")) Services.SaveNow();
            if (GUILayout.Button("Novo jogo (apaga save)")) Services.StartNewGame();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Menu")) SceneFlow.GoToMainMenu();
            if (GUILayout.Button("Esconderijo")) SceneFlow.GoToHideout();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label($"<b>Crew</b> ({s.crew.Count}/{Services.Crew.MaxCrewSize}, vagas no golpe {Services.Crew.HeistSlots})", Rich());
            foreach (var thief in s.crew)
                GUILayout.Label($"• {thief.DisplayName} nv{thief.level} P{thief.skill:0} D{thief.stealth:0} N{thief.nerves:0} [{thief.status}]");

            GUILayout.Space(6);
            DrawHeist();

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        void DrawHeist()
        {
            var h = Services.Heist;
            GUILayout.Label("<b>Golpe</b>", Rich());

            if (!h.IsRunning)
            {
                if (h.LastResult != null)
                    GUILayout.Label($"Último: {h.LastResult.outcome}, {Formulas.Money(h.LastResult.moneyGained)}, presos {h.LastResult.arrestedThiefIds.Count}");

                foreach (var heist in Services.Db.heists)
                {
                    if (heist == null || !Services.Progression.IsUnlocked(heist)) continue;
                    if (GUILayout.Button("Começar: " + heist.displayName))
                    {
                        var team = new System.Collections.Generic.List<ThiefState>();
                        foreach (var thief in Services.Crew.GetAvailable())
                            if (team.Count < Services.Crew.HeistSlots) team.Add(thief);
                        if (h.CanStart(heist, team, out var reason)) h.StartHeist(heist, team);
                        else Debug.LogWarning("[Debug] " + reason);
                    }
                }
                return;
            }

            GUILayout.Label($"{h.CurrentHeist.displayName} · sala {h.RoomIndex + 1}/{h.CurrentHeist.rooms.Count} ({h.CurrentRoom?.obstacle}) {h.RoomProgress01:P0}");
            GUILayout.Label($"Alerta {h.Alert:0}/100 · Sacola {Formulas.Money(h.Bag)}" + (h.SirenActive ? $" · SIRENE {h.PoliceSecondsLeft:0.0}s" : ""));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+20 alerta")) h.AddAlert(20);
            if (GUILayout.Button("Velocidade " + h.Speed + "x")) h.Speed = h.Speed > 1f ? 1f : Services.Config.speedFast;
            if (GUILayout.Button("Meter o pé")) h.Flee();
            GUILayout.EndHorizontal();

            if (h.Phase == HeistPhase.AwaitingDoor)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Próxima sala")) h.ChooseDoor(true);
                if (GUILayout.Button("Meter o pé")) h.ChooseDoor(false);
                GUILayout.EndHorizontal();
            }

            if (h.Phase == HeistPhase.AwaitingEvent && h.PendingEvent != null)
            {
                GUILayout.Label("Evento: " + h.PendingEvent.def.displayName);
                for (int i = 0; i < h.PendingEvent.choices.Count; i++)
                {
                    var view = h.PendingEvent.choices[i];
                    GUI.enabled = view.available;
                    if (GUILayout.Button($"{view.choice.label} ({view.successChance:P0})")) h.ChooseEventOption(i);
                    GUI.enabled = true;
                }
            }

            foreach (var thief in h.CurrentCrew)
            {
                bool can = h.CanUseAbility(thief, out var why);
                GUI.enabled = can;
                if (GUILayout.Button($"Habilidade de {thief.DisplayName}" + (can ? "" : $" ({why})"))) h.UseAbility(thief);
                GUI.enabled = true;
            }
        }

        static GUIStyle rich;
        static GUIStyle Rich()
        {
            if (rich == null) rich = new GUIStyle(GUI.skin.label) { richText = true };
            return rich;
        }
    }
}
