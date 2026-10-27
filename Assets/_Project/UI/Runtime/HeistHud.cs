using System.Collections.Generic;
using System.Linq;
using AFirma.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace AFirma.UI
{
    /// <summary>
    /// HUD do golpe (prefab UI/HeistHUD, na cena Heist): sala, alerta, sacola, velocidade, pausa (Esc),
    /// a crew com o botão de habilidade e Meter o pé. No meio abre o popup de porta, o cartão de evento
    /// e a contagem da polícia; no fim, o jornal. Lê o Services.Heist todo frame: o popup abre sozinho
    /// quando o golpe para esperando uma decisão.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HeistHud : MonoBehaviour
    {
        public static readonly string[] ObstacleNames = { "Fechadura", "Sistema", "Pessoa" };

        VisualElement root, popup, paused, crewBar, roomFill, alertFill;
        Label heistName, room, alertText, bag, countdown;
        Button speed;
        readonly List<(ThiefState thief, Button button)> abilities = new List<(ThiefState, Button)>();
        object shownDecision;
        Label hint;

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            popup = root.Q("popup");
            paused = root.Q("paused");
            crewBar = root.Q("crew");
            roomFill = root.Q("room-fill");
            alertFill = root.Q("alert-fill");
            heistName = root.Q<Label>("heist-name");
            room = root.Q<Label>("room");
            alertText = root.Q<Label>("alert-text");
            bag = root.Q<Label>("bag");
            countdown = root.Q<Label>("countdown");

            speed = root.OnClick("speed", () =>
                Services.Heist.Speed = Services.Heist.Speed > 1f ? 1f : Services.Config.speedFast);
            root.OnClick("pause", () => SetPaused(true));
            root.OnClick("resume", () => SetPaused(false));
            root.OnClick("options", () => MenuPanels.Options(root));
            root.OnClick("give-up", () => { SetPaused(false); if (Services.Heist.IsRunning) Services.Heist.Flee(); });
            root.OnClick("flee", () => { if (Services.Heist.IsRunning) Services.Heist.Flee(); });

            GameEvents.HeistStarted += OnStarted;
            GameEvents.HeistEventResolved += OnEventResolved;
            GameEvents.HeistFinished += OnFinished;
            Notices.Attach(root);
            if (Services.Heist.IsRunning) OnStarted(Services.Heist.CurrentHeist);
        }

        void OnDisable()
        {
            GameEvents.HeistStarted -= OnStarted;
            GameEvents.HeistEventResolved -= OnEventResolved;
            GameEvents.HeistFinished -= OnFinished;
            HideHint();
            if (GameClock.Paused) GameClock.SetPaused(false);
        }

        /// <summary>Usa e avisa o que mudou (sala aberta, alerta antes → depois); sem número pra mostrar, diz o que a habilidade faz.</summary>
        void UseAbility(ThiefState thief, ThiefAbilityDef ability)
        {
            var h = Services.Heist;
            float alert = h.Alert;
            int roomIndex = h.RoomIndex;
            bool working = h.Phase == HeistPhase.Working;
            h.UseAbility(thief);
            if (!h.IsRunning) return; // fuga: o jornal conta o resto

            var parts = new List<string>();
            if (working && (h.RoomIndex != roomIndex || h.Phase != HeistPhase.Working)) parts.Add("sala aberta");
            if (Mathf.RoundToInt(alert) != Mathf.RoundToInt(h.Alert)) parts.Add($"alerta {alert:0} → {h.Alert:0}");
            Ui.Toast(root, $"{ability.displayName}: " + (parts.Count > 0 ? string.Join(", ", parts) : ability.description));
        }

        /// <summary>Descrição da habilidade enquanto o mouse está em cima do botão (tooltip não aparece em runtime).</summary>
        void ShowHint(string text)
        {
            HideHint();
            var toasts = root.Q("toasts");
            if (toasts == null || string.IsNullOrEmpty(text)) return;
            hint = Ui.Text(text, "dark toast");
            toasts.Add(hint);
        }

        void HideHint()
        {
            hint?.RemoveFromHierarchy();
            hint = null;
        }

        void SetPaused(bool value)
        {
            GameClock.SetPaused(value);
            paused.SetShown(value);
        }

        /// <summary>Um cartão por ladrão: rosto (recorte do retrato 3x4) e o botão da habilidade.</summary>
        void OnStarted(HeistDef heist)
        {
            HideHint();
            crewBar.Clear();
            abilities.Clear();
            foreach (var thief in Services.Heist.CurrentCrew)
            {
                var type = Services.Db.GetThiefType(thief.type);
                var ability = type != null ? type.ability : null;
                var button = Ui.Btn(ability != null ? ability.displayName : "—", () =>
                {
                    HideHint();
                    if (Services.Heist.CanUseAbility(thief, out var reason)) UseAbility(thief, ability);
                    else Ui.Toast(root, reason);
                });
                if (ability != null)
                {
                    button.RegisterCallback<PointerEnterEvent>(_ => ShowHint(ability.description));
                    button.RegisterCallback<PointerLeaveEvent>(_ => HideHint());
                }
                abilities.Add((thief, button));
                crewBar.Add(Ui.El("dark row hud-thief",
                    Ui.Image(type != null ? type.portrait : null, "hud-thief__face"),
                    Ui.El("grow", Ui.Text(thief.firstName, "light"), button)));
            }
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && Services.Heist.IsRunning)
                SetPaused(!GameClock.Paused);

            var h = Services.Heist;
            if (!h.IsRunning) return;
            heistName.text = h.CurrentHeist.displayName;
            room.text = $"Sala {h.RoomIndex + 1}/{h.CurrentHeist.rooms.Count} · {h.CurrentRoom?.name}";
            roomFill.style.width = Length.Percent(h.RoomProgress01 * 100f);
            alertFill.style.width = Length.Percent(h.Alert);
            alertFill.EnableInClassList("alert--mid", h.Alert >= 50f && h.Alert < 80f);
            alertFill.EnableInClassList("alert--high", h.Alert >= 80f || h.SirenActive);
            alertText.text = h.SirenActive ? "SIRENE!" : $"Alerta {h.Alert:0}";
            bag.text = Formulas.Money(h.Bag);
            speed.text = h.Speed > 1f ? $"{h.Speed:0}x" : "1x";
            countdown.SetShown(h.SirenActive);
            countdown.text = $"POLÍCIA EM {Mathf.CeilToInt(h.PoliceSecondsLeft)}s";
            foreach (var (thief, button) in abilities)
            {
                button.SetAvailable(h.CanUseAbility(thief, out _));
                if (h.HasUsedAbility(thief)) button.text = "Usada";
            }

            object decision = h.Phase == HeistPhase.AwaitingDoor ? h.PendingDoor
                : h.Phase == HeistPhase.AwaitingEvent ? h.PendingEvent : (object)null;
            if (decision != shownDecision) ShowDecision(decision);
        }

        void ShowDecision(object decision)
        {
            shownDecision = decision;
            popup.Clear();
            popup.SetShown(decision != null);
            if (decision != null) UiSounds.Paper();
            if (decision is DoorDecision door) DoorPopup(door);
            else if (decision is HeistEventInstance heistEvent) EventCard(heistEvent);
        }

        void DoorPopup(DoorDecision door)
        {
            var next = door.nextRoom;
            popup.Add(Ui.Text(door.sirenActive ? "A polícia tá chegando!" : "Próxima sala?", "h2"));
            popup.Add(Ui.El("row", Ui.El("icon icon--" + (int)next.obstacle),
                Ui.Text($" {next.name} · {ObstacleNames[(int)next.obstacle]}")));
            popup.Add(Ui.Text($"Loot estimado: {Formulas.Money(door.estimatedLoot)} · Na sacola: {Formulas.Money(door.currentBag)}"));
            if (!door.hasSpecialist)
                popup.Add(Ui.El("row", Ui.El("icon icon--nospec"), Ui.Text(" Sem especialista: fica lenta e o alerta dobra.", "danger")));
            popup.Add(Ui.El("row dialog__buttons",
                Ui.Btn("Meter o pé", () => Services.Heist.ChooseDoor(false), "btn--red"),
                Named(Ui.Btn("Próxima sala", () => Services.Heist.ChooseDoor(true), "btn--green"), "next-room")));
        }

        void EventCard(HeistEventInstance heistEvent)
        {
            popup.Add(Ui.Text(heistEvent.def.displayName, "h2"));
            if (heistEvent.def.art != null) popup.Add(Ui.Image(heistEvent.def.art, "event__art"));
            popup.Add(Ui.Text(heistEvent.def.body, "dialog__body"));
            for (int i = 0; i < heistEvent.choices.Count; i++)
            {
                var view = heistEvent.choices[i];
                int index = i;
                var label = !view.available ? $"{view.choice.label} ({view.unavailableReason})"
                    : view.successChance < 1f ? $"{view.choice.label} ({Mathf.RoundToInt(view.successChance * 100f)}%)"
                    : view.choice.label;
                var button = Ui.Btn(label, () =>
                {
                    if (view.available) Services.Heist.ChooseEventOption(index);
                    else Ui.Toast(root, view.unavailableReason);
                });
                button.name = "choice-" + i;
                button.SetAvailable(view.available);
                popup.Add(button);
            }
        }

        void OnEventResolved(EventResolution resolution)
        {
            var text = resolution.effect != null ? resolution.effect.resultText : null;
            Ui.Toast(root, string.IsNullOrEmpty(text) ? (resolution.success ? "Deu certo." : "Deu ruim.") : text);
        }

        /// <summary>Capa do "O Guanabarense": manchete, dinheiro, salas, XP de cada um e quem foi preso.</summary>
        void OnFinished(HeistResult result)
        {
            HideHint();
            ShowDecision(null);
            countdown.SetShown(false);
            if (GameClock.Paused) SetPaused(false);
            var heist = Services.Db.GetHeist(result.heistId);
            var headline = Services.Db.headlines != null
                ? Services.Db.headlines.Pick(result, heist != null ? heist.displayName : "", new System.Random())
                : heist?.displayName;

            var crew = Ui.El("news__col");
            foreach (var id in result.xpGains.Select(g => g.thiefId).Union(result.arrestedThiefIds))
            {
                var thief = Services.Crew.Crew.FirstOrDefault(t => t.id == id);
                if (thief == null) continue;
                var gain = result.xpGains.FirstOrDefault(g => g.thiefId == id) ?? new ThiefXpGain();
                var type = Services.Db.GetThiefType(thief.type);
                var line = Ui.El("row news__thief",
                    Ui.Image(type != null ? type.portrait : null, "hud-thief__face"),
                    Ui.El("grow", Ui.Text(thief.DisplayName),
                        Ui.Text(gain.leveledUp ? $"+{gain.xp} XP · subiu pro nível {gain.levelAfter}!" : $"+{gain.xp} XP", gain.leveledUp ? "money" : "muted")));
                if (result.arrestedThiefIds.Contains(thief.id)) line.Add(Ui.Text("PRESO", "stamp"));
                crew.Add(line);
            }

            var news = Ui.El("news news--folded",
                Ui.Text("O GUANABARENSE", "h1 news__mast"),
                Ui.Text("Nova Guanabara, 1992 · Edição extra · Cr$ 500", "muted news__mast"),
                Ui.El("news__rule"),
                Ui.Text(headline, "h2 news__headline"),
                Ui.El("row",
                    Ui.El("news__col",
                        Ui.Text($"Salas limpas: {result.roomsCleared}/{result.totalRooms}"),
                        Ui.Text($"Na sacola: {Formulas.Money(result.bag)}"),
                        Ui.Text($"Pro caixa: +{Formulas.Money(result.moneyGained)}", "h2 money")),
                    crew),
                Ui.El("news__rule"),
                Named(Ui.Btn("Voltar pro esconderijo", SceneFlow.GoToHideout, "btn--green"), "back"));
            news.name = "news";
            root.Add(Ui.El("scrim", news));
            news.schedule.Execute(() => news.RemoveFromClassList("news--folded")).StartingIn(50);
            UiSounds.Play(UiSounds.Get?.typewriter);
        }

        static Button Named(Button button, string name)
        {
            button.name = name;
            return button;
        }
    }
}
