using System.Collections.Generic;
using System.Linq;
using AFirma.Core;
using UnityEngine.UIElements;

namespace AFirma.UI
{
    /// <summary>
    /// Aba Mapa do esconderijo: o mapa de Nova Guanabara com um pino por golpe (bloqueado, liberado, concluído)
    /// e o modificador sorteado na visita. Escolher um golpe e Planejar abre o planejamento: crew, especialistas e Ir.
    /// </summary>
    static class MapPanel
    {
        static readonly string[] ObstacleNames = { "Fechadura", "Sistema", "Pessoa" };

        public static void Build(VisualElement content)
        {
            var progression = Services.Progression;
            progression.RollModifiers();
            var map = Ui.El("map");
            var info = Ui.El("paper row map__info");
            content.Add(map);
            content.Add(info);

            HeistDef first = null;
            foreach (var heist in progression.AllHeists.Where(h => h != null))
            {
                map.Add(Pin(heist, () => Select(content, info, heist)));
                if (first == null && progression.IsUnlocked(heist) && !progression.IsCompleted(heist)) first = heist;
            }
            Select(content, info, first ?? progression.AllHeists.FirstOrDefault());
        }

        /// <summary>mapPosition vai de x −6..6 (Zona Norte → Porto) e y 0..1,5; o mapa tem 400x230 e o mar fica embaixo.</summary>
        static Button Pin(HeistDef heist, System.Action onClick)
        {
            var progression = Services.Progression;
            bool unlocked = progression.IsUnlocked(heist);
            var pin = new Button { name = heist.id, userData = heist };
            pin.AddToClassList("pin");
            pin.AddToClassList(!unlocked ? "pin--locked" : progression.IsCompleted(heist) ? "pin--done" : "pin--open");
            pin.Add(Ui.El("pin__dot"));
            pin.Add(Ui.Text(heist.displayName, "pin__name"));
            var modifier = progression.GetModifier(heist);
            if (unlocked && modifier != null) pin.Add(Ui.Text(modifier.displayName, "pin__mod"));
            pin.style.left = 200 + heist.mapPosition.x * 24;
            pin.style.top = 230 * (0.58f - heist.mapPosition.y * 0.16f);
            return pin.OnClick(onClick);
        }

        static void Select(VisualElement content, VisualElement info, HeistDef heist)
        {
            if (heist == null) return;
            content.Query<Button>(className: "pin").ForEach(p => p.EnableInClassList("pin--on", p.userData == heist));
            info.Clear();
            var progression = Services.Progression;
            var modifier = progression.GetModifier(heist);
            var text = Ui.El("grow",
                Ui.Text(heist.displayName, "h2"),
                Ui.Text($"{heist.rooms.Count} salas · {Obstacles(heist)} · até {Formulas.Money(heist.TotalLoot * progression.GetContext(heist).lootMult)}"));
            if (modifier != null) text.Add(Ui.Text($"{modifier.displayName}: {modifier.description}", "danger"));
            info.Add(text);

            bool unlocked = progression.IsUnlocked(heist);
            var plan = Ui.Btn("Planejar", () =>
            {
                if (unlocked) Planning(content, heist);
                else Ui.Toast(info, heist.unlockedBy != null ? $"Libera depois de limpar o cofre: {heist.unlockedBy.displayName}." : "Golpe ainda bloqueado.");
            }, "btn--green");
            plan.name = "plan";
            plan.SetAvailable(unlocked);
            info.Add(plan);
            if (unlocked) Tutorial.Step("plano", plan, "Escolhe o golpe no mapa e clica em Planejar pra ver quem vai.");
        }

        static string Obstacles(HeistDef heist) => string.Join(" · ", heist.rooms.GroupBy(r => r.obstacle).OrderBy(g => g.Key)
            .Select(g => $"{ObstacleNames[(int)g.Key]} ×{g.Count()}"));

        /// <summary>Planejamento: escolher até HeistSlots ladrões, ver quem falta e ir.</summary>
        static void Planning(VisualElement content, HeistDef heist)
        {
            content.Clear();
            var crew = Services.Crew;
            var chosen = crew.GetAvailable().Take(crew.HeistSlots).ToList();
            var list = new ScrollView();
            list.AddToClassList("grow");
            var warnings = Ui.El();
            var go = Ui.Btn("Ir!", () =>
            {
                if (Services.Heist.CanStart(heist, chosen, out var reason)) SceneFlow.GoToHeist(heist, chosen);
                else Ui.Toast(content, reason);
            }, "btn--green plan__go");
            go.name = "go";
            Tutorial.Step("ir", go, "Especialista na sala certa trabalha mais rápido. Escolheu a crew? Manda ver.");

            void Refresh()
            {
                foreach (var card in list.Query<Button>(className: "card").ToList())
                    card.EnableInClassList("card--on", chosen.Contains((ThiefState)card.userData));
                warnings.Clear();
                foreach (var obstacle in heist.rooms.Select(r => r.obstacle).Distinct().OrderBy(o => o))
                    if (!chosen.Any(t => IsSpecialist(t, obstacle)))
                        warnings.Add(Ui.Text($"Sem {SpecialistName(obstacle)}: {ObstacleNames[(int)obstacle]} fica lenta e o alerta dobra.", "danger"));
                bool can = Services.Heist.CanStart(heist, chosen, out var reason);
                go.text = can ? $"Ir! ({chosen.Count}/{crew.HeistSlots})" : reason;
                go.SetAvailable(can);
            }

            foreach (var thief in crew.Crew)
            {
                var type = Services.Db.GetThiefType(thief.type);
                bool free = thief.status == ThiefStatus.Available || thief.status == ThiefStatus.OnSideJob;
                var status = thief.status == ThiefStatus.Jailed ? $"Preso: sai em {Clock(crew.SecondsLeftInJail(thief))}"
                    : thief.status == ThiefStatus.OnSideJob ? "No bico (sai do bico se for)" : $"Nível {thief.level}";
                var card = new Button { userData = thief };
                card.AddToClassList("card");
                card.AddToClassList("row");
                card.Add(Ui.Image(type != null ? type.portrait : null, "portrait"));
                card.Add(Ui.El("grow",
                    Ui.Text(thief.DisplayName),
                    Ui.Text(type != null ? type.displayName : thief.type.ToString(), "muted"),
                    Ui.Text(status, free ? null : "danger")));
                card.OnClick(() =>
                {
                    if (chosen.Remove(thief)) { Refresh(); return; }
                    if (!free) Ui.Toast(content, status);
                    else if (chosen.Count >= crew.HeistSlots) Ui.Toast(content, $"Só cabem {crew.HeistSlots} na van. Tire alguém antes.");
                    else chosen.Add(thief);
                    Refresh();
                });
                list.Add(card);
            }

            content.Add(Ui.El("row",
                Ui.Text(heist.displayName, "h2 grow"),
                Ui.Btn("Voltar ao mapa", () => { content.Clear(); Build(content); })));
            content.Add(Ui.El("row plan",
                Ui.El("paper plan__brief", Ui.Text(heist.briefing), Ui.Text(Obstacles(heist), "muted"), warnings),
                Ui.El("plan__crew", Ui.Text($"Crew (cabem {crew.HeistSlots} na van)", "muted"), list)));
            content.Add(go);
            Refresh();
        }

        static bool IsSpecialist(ThiefState thief, ObstacleType obstacle)
        {
            var type = Services.Db.GetThiefType(thief.type);
            return type != null && type.hasSpecialty && type.specialty == obstacle;
        }

        static string SpecialistName(ObstacleType obstacle) =>
            Services.Db.thiefTypes.FirstOrDefault(t => t != null && t.hasSpecialty && t.specialty == obstacle)?.displayName ?? "especialista";

        static string Clock(float seconds) => $"{(int)seconds / 60}:{(int)seconds % 60:00}";
    }
}
