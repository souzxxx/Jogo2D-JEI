using AFirma.Core;
using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>
    /// HUD provisória (OnGUI) numa faixa embaixo da tela, pra cena do golpe ser jogável enquanto o prefab da HUD
    /// do Enzo não entra. Quando a HUD real estiver na cena, apague o GameObject "HUD provisória".
    /// </summary>
    public class HeistSceneFallbackHud : MonoBehaviour
    {
        HeistResult result;

        void OnEnable() => GameEvents.HeistFinished += OnFinished;
        void OnDisable() => GameEvents.HeistFinished -= OnFinished;
        void OnFinished(HeistResult r) => result = r;

        void OnGUI()
        {
            if (!Services.IsInitialized) return;
            var h = Services.Heist;
            const float height = 118f;
            var area = new Rect(8, Screen.height - height - 8, Screen.width - 16, height);
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 8, area.y + 6, area.width - 16, area.height - 12));

            if (h.IsRunning)
            {
                GUILayout.Label($"{h.CurrentHeist.displayName} · sala {h.RoomIndex + 1}/{h.CurrentHeist.rooms.Count}: {h.CurrentRoom?.name}" +
                                $" · alerta {h.Alert:0} · sacola {Formulas.Money(h.Bag)}" +
                                (h.SirenActive ? $" · POLÍCIA EM {h.PoliceSecondsLeft:0}s" : ""));
                GUILayout.BeginHorizontal();
                if (h.Phase == HeistPhase.AwaitingDoor && h.PendingDoor != null)
                {
                    var d = h.PendingDoor;
                    GUILayout.Label($"Próxima: {d.nextRoom.name} (~{Formulas.Money(d.estimatedLoot)})" + (d.hasSpecialist ? "" : " · sem especialista!"), GUILayout.Width(330));
                    if (GUILayout.Button("Próxima sala", GUILayout.Height(28))) h.ChooseDoor(true);
                    if (GUILayout.Button("Meter o pé", GUILayout.Height(28))) h.ChooseDoor(false);
                }
                else if (h.Phase == HeistPhase.AwaitingEvent && h.PendingEvent != null)
                {
                    GUILayout.Label(h.PendingEvent.def.displayName + ": " + h.PendingEvent.def.body, GUILayout.Width(330));
                    for (int i = 0; i < h.PendingEvent.choices.Count; i++)
                    {
                        var view = h.PendingEvent.choices[i];
                        GUI.enabled = view.available;
                        var label = view.choice.label + (view.available ? $" ({view.successChance:P0})" : $" ({view.unavailableReason})");
                        if (GUILayout.Button(label, GUILayout.Height(28))) h.ChooseEventOption(i);
                        GUI.enabled = true;
                    }
                }
                else if (GUILayout.Button("Meter o pé agora", GUILayout.Height(28), GUILayout.Width(160))) h.Flee();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                foreach (var thief in h.CurrentCrew)
                {
                    var def = Services.Db.GetThiefType(thief.type);
                    bool can = h.CanUseAbility(thief, out var why);
                    GUI.enabled = can;
                    var label = def != null && def.ability != null ? def.ability.displayName : "Habilidade";
                    if (GUILayout.Button(new GUIContent($"{label} ({thief.DisplayName})", can ? null : why), GUILayout.Height(26))) h.UseAbility(thief);
                    GUI.enabled = true;
                }
                if (GUILayout.Button("Velocidade " + h.Speed + "x", GUILayout.Height(26), GUILayout.Width(120)))
                    h.Speed = h.Speed > 1f ? 1f : Services.Config.speedFast;
                if (GUILayout.Button(GameClock.Paused ? "Continuar" : "Pausa", GUILayout.Height(26), GUILayout.Width(90)))
                    GameClock.SetPaused(!GameClock.Paused);
                GUILayout.EndHorizontal();
                GUILayout.Label(GUI.tooltip);
            }
            else
            {
                if (result != null)
                    GUILayout.Label($"O GUANABARENSE · {result.outcome} · {result.roomsCleared}/{result.totalRooms} salas · {Formulas.Money(result.moneyGained)}" +
                                    (result.arrestedThiefIds.Count > 0 ? $" · presos: {result.arrestedThiefIds.Count}" : ""));
                if (GUILayout.Button("Voltar pro esconderijo", GUILayout.Height(32), GUILayout.Width(220))) SceneFlow.GoToHideout();
            }
            GUILayout.EndArea();
        }
    }
}
