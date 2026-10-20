using System.Collections.Generic;
using UnityEngine;

namespace AFirma.Core
{
    /// <summary>
    /// Tela provisória feita com OnGUI pra o fluxo do jogo funcionar desde o dia 1.
    /// O dono da cena apaga este componente quando a versão de verdade estiver pronta.
    /// </summary>
    public class ScenePlaceholder : MonoBehaviour
    {
        public enum Mode { MainMenu, Hideout, Heist, Sandbox }

        public Mode mode = Mode.Sandbox;
        public string title = "Cena";
        [Tooltip("Quem é dono desta cena.")]
        public string owner = "";

        HeistResult shownResult;
        string notice;

        void OnEnable()
        {
            GameEvents.HeistFinished += OnHeistFinished;
            GameEvents.Notice += OnNotice;
        }

        void OnDisable()
        {
            GameEvents.HeistFinished -= OnHeistFinished;
            GameEvents.Notice -= OnNotice;
        }

        void OnHeistFinished(HeistResult result) => shownResult = result;
        void OnNotice(string message) => notice = message;

        void OnGUI()
        {
            var area = new Rect(Screen.width / 2f - 260, 40, 520, Screen.height - 80);
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label($"<size=20><b>{title}</b></size>", Rich());
            GUILayout.Label($"<i>Placeholder — dono: {owner}. Aperte F1 pro painel de debug.</i>", Rich());
            if (!string.IsNullOrEmpty(notice)) GUILayout.Label("Aviso: " + notice);
            GUILayout.Space(10);

            if (Services.IsInitialized)
            {
                switch (mode)
                {
                    case Mode.MainMenu: DrawMainMenu(); break;
                    case Mode.Hideout: DrawHideout(); break;
                    case Mode.Heist: DrawHeist(); break;
                }
            }
            GUILayout.EndArea();
        }

        void DrawMainMenu()
        {
            if (Services.Save.HasSave && GUILayout.Button("Continuar", GUILayout.Height(36))) SceneFlow.GoToHideout();
            if (GUILayout.Button("Novo jogo", GUILayout.Height(36)))
            {
                Services.StartNewGame();
                SceneFlow.GoToHideout();
            }
        }

        void DrawHideout()
        {
            GUILayout.Label($"Caixa: {Formulas.Money(Services.Economy.Money)} (+{Services.Economy.IncomePerSecond:0.#}/s)");
            GUILayout.Label("Crew:");
            foreach (var thief in Services.Crew.Crew)
                GUILayout.Label($"  {thief.DisplayName} · nível {thief.level} · {thief.status}");

            GUILayout.Space(8);
            GUILayout.Label("Golpes:");
            foreach (var heist in Services.Progression.AllHeists)
            {
                if (heist == null) continue;
                bool unlocked = Services.Progression.IsUnlocked(heist);
                GUI.enabled = unlocked;
                var label = $"{heist.displayName} ({heist.rooms.Count} salas, ~{Formulas.Money(heist.TotalLoot)})" + (unlocked ? "" : " — bloqueado");
                if (GUILayout.Button(label))
                {
                    var team = new List<ThiefState>();
                    foreach (var thief in Services.Crew.GetAvailable())
                        if (team.Count < Services.Crew.HeistSlots) team.Add(thief);
                    if (Services.Heist.CanStart(heist, team, out var reason)) SceneFlow.GoToHeist(heist, team);
                    else notice = reason;
                }
                GUI.enabled = true;
            }

            GUILayout.Space(8);
            if (GUILayout.Button("Menu principal")) SceneFlow.GoToMainMenu();
        }

        void DrawHeist()
        {
            var h = Services.Heist;
            if (h.IsRunning)
            {
                GUILayout.Label($"{h.CurrentHeist.displayName} · sala {h.RoomIndex + 1}/{h.CurrentHeist.rooms.Count}: {h.CurrentRoom?.name} ({h.CurrentRoom?.obstacle})");
                GUILayout.HorizontalSlider(h.RoomProgress01, 0f, 1f);
                GUILayout.Label($"Alerta {h.Alert:0}/100 · Sacola {Formulas.Money(h.Bag)}" + (h.SirenActive ? $" · POLÍCIA EM {h.PoliceSecondsLeft:0}s" : ""));

                if (h.Phase == HeistPhase.AwaitingDoor && h.PendingDoor != null)
                {
                    var d = h.PendingDoor;
                    GUILayout.Label($"Próxima: {d.nextRoom.name} ({d.nextRoom.obstacle}) ~{Formulas.Money(d.estimatedLoot)}" + (d.hasSpecialist ? "" : " — sem especialista!"));
                    if (GUILayout.Button("Próxima sala", GUILayout.Height(30))) h.ChooseDoor(true);
                    if (GUILayout.Button("Meter o pé", GUILayout.Height(30))) h.ChooseDoor(false);
                }
                else if (h.Phase == HeistPhase.AwaitingEvent && h.PendingEvent != null)
                {
                    GUILayout.Label($"<b>{h.PendingEvent.def.displayName}</b>", Rich());
                    GUILayout.Label(h.PendingEvent.def.body);
                    for (int i = 0; i < h.PendingEvent.choices.Count; i++)
                    {
                        var view = h.PendingEvent.choices[i];
                        GUI.enabled = view.available;
                        var suffix = view.available ? $" ({view.successChance:P0})" : $" — {view.unavailableReason}";
                        if (GUILayout.Button(view.choice.label + suffix)) h.ChooseEventOption(i);
                        GUI.enabled = true;
                    }
                }
                else if (GUILayout.Button("Meter o pé agora")) h.Flee();
                return;
            }

            if (shownResult != null)
            {
                GUILayout.Label("<b>O GUANABARENSE</b>", Rich());
                GUILayout.Label($"Resultado: {shownResult.outcome} · {shownResult.roomsCleared}/{shownResult.totalRooms} salas · {Formulas.Money(shownResult.moneyGained)}");
                if (shownResult.arrestedThiefIds.Count > 0) GUILayout.Label($"Presos: {shownResult.arrestedThiefIds.Count}");
            }
            if (GUILayout.Button("Voltar pro esconderijo", GUILayout.Height(32))) SceneFlow.GoToHideout();
        }

        static GUIStyle rich;
        static GUIStyle Rich()
        {
            if (rich == null) rich = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
            return rich;
        }
    }
}
