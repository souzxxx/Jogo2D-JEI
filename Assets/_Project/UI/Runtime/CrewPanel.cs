using System;
using System.Collections.Generic;
using System.Linq;
using AFirma.Core;
using UnityEngine.UIElements;

namespace AFirma.UI
{
    /// <summary>Abas Crew (ficha de cada ladrão, demitir, fiança) e Contatos (recrutamento) do esconderijo.</summary>
    static class CrewPanel
    {
        static ICrewService Crew => Services.Crew;

        public static void BuildCrew(VisualElement content)
        {
            content.Add(Ui.Live(() => $"{Crew.MaxCrewSize}|" + string.Join(",", Crew.Crew.Select(t => $"{t.id}{t.status}{t.level}{t.sideJobId}")), (panel, updates) =>
            {
                panel.Add(Ui.Text($"Crew ({Crew.Crew.Count}/{Crew.MaxCrewSize})", "h2"));
                var list = new ScrollView();
                list.AddToClassList("grow");
                foreach (var thief in Crew.Crew) list.Add(ThiefCard(thief, updates));
                panel.Add(list);
            }));
        }

        static VisualElement ThiefCard(ThiefState thief, List<Action> updates)
        {
            var info = Info(thief);
            int toNext = Crew.XpToNextLevel(thief);
            info.Insert(2, Ui.Text(toNext > 0 ? $"Nível {thief.level} · XP {thief.xp}/{toNext}" : $"Nível {thief.level} · MÁX"));

            var status = Ui.Text("");
            info.Add(status);
            var actions = Ui.El("crew__actions");
            if (thief.status == ThiefStatus.Jailed)
            {
                var bail = Ui.Btn("", () =>
                {
                    if (Crew.CanPayBail(thief, out var reason)) { Crew.PayBail(thief); UiSounds.Buy(); }
                    else Ui.Toast(info, reason);
                }, "btn--green");
                bail.name = "bail-" + thief.id;
                actions.Add(bail);
                updates.Add(() =>
                {
                    status.text = $"Preso: sai em {Clock(Crew.SecondsLeftInJail(thief))}";
                    bail.text = $"Fiança {Formulas.Money(Crew.GetBailCost(thief))}";
                    bail.SetAvailable(Crew.CanPayBail(thief, out _));
                });
            }
            else status.text = StatusText(thief);

            var fire = Ui.Btn("Demitir", () =>
            {
                if (Crew.CanFire(thief, out var reason))
                    Ui.Confirm(info, $"Demitir {thief.DisplayName}?", "Sai da firma e não volta. Quem tava no bico larga o bico.", "Demitir", () => Crew.Fire(thief));
                else Ui.Toast(info, reason);
            }, "btn--red");
            fire.name = "fire-" + thief.id;
            fire.SetAvailable(Crew.CanFire(thief, out _));
            actions.Add(fire);

            return Ui.El("card row", Portrait(thief.type), info, actions);
        }

        public static void BuildContacts(VisualElement content)
        {
            content.Add(Ui.Live(() => $"{Crew.Crew.Count}|" + string.Join(",", Crew.Candidates.Select(c => c.thief.id)), (panel, updates) =>
            {
                var timer = Ui.Text("", "muted grow");
                var refresh = Ui.Btn("", () =>
                {
                    if (Services.Economy.CanAfford(Crew.PaidRefreshCost)) Crew.RefreshCandidates(true);
                    else Ui.Toast(panel, $"Falta {Formulas.Money(Crew.PaidRefreshCost - Services.Economy.Money)} pra trocar agora.");
                });
                refresh.name = "refresh";
                updates.Add(() =>
                {
                    timer.text = $"A Dona Cida manda gente nova em {Clock(Crew.SecondsToCandidatesRefresh)}";
                    refresh.text = $"Trocar agora ({Formulas.Money(Crew.PaidRefreshCost)})";
                    refresh.SetAvailable(Services.Economy.CanAfford(Crew.PaidRefreshCost));
                });
                panel.Add(Ui.Text($"Contatos · crew {Crew.Crew.Count}/{Crew.MaxCrewSize}", "h2"));
                panel.Add(Ui.El("row", timer, refresh));

                var list = new ScrollView();
                list.AddToClassList("grow");
                foreach (var candidate in Crew.Candidates)
                {
                    var info = Info(candidate.thief);
                    info.Insert(2, Ui.Text($"Nível {candidate.thief.level}"));
                    var hire = Ui.Btn($"Recrutar {Formulas.Money(candidate.cost)}", () =>
                    {
                        if (Crew.CanRecruit(candidate, out var reason)) { Crew.Recruit(candidate); UiSounds.Buy(); }
                        else Ui.Toast(panel, reason);
                    }, "btn--green");
                    hire.name = "hire-" + candidate.thief.id;
                    updates.Add(() => hire.SetAvailable(Crew.CanRecruit(candidate, out _)));
                    list.Add(Ui.El("card row", Portrait(candidate.thief.type), info, Ui.El("crew__actions", hire)));
                }
                panel.Add(list);
            }));
        }

        /// <summary>Nome, tipo, atributos efetivos (com manha e upgrades) e a manha com o que ela faz.</summary>
        static VisualElement Info(ThiefState thief)
        {
            var type = Services.Db.GetThiefType(thief.type);
            var trait = Services.Db.GetTrait(thief.traitId);
            var info = Ui.El("grow",
                Ui.Text(thief.DisplayName),
                Ui.Text(type != null ? type.displayName : thief.type.ToString(), "muted"),
                Ui.Text($"Perícia {Crew.GetSkill(thief):0} · Discrição {Crew.GetStealth(thief):0} · Nervos {Crew.GetNerves(thief):0}"));
            if (trait != null) info.Add(Ui.Text($"Manha: {trait.displayName}. {trait.description}", "muted"));
            return info;
        }

        static VisualElement Portrait(ThiefType type) => Ui.Image(Services.Db.GetThiefType(type)?.portrait, "portrait");

        static string StatusText(ThiefState thief)
        {
            switch (thief.status)
            {
                case ThiefStatus.OnHeist: return "Num golpe";
                case ThiefStatus.OnSideJob:
                    var job = Services.Db.GetSideJob(thief.sideJobId);
                    return $"No bico: {job?.displayName} (+{Formulas.Money(Services.Economy.GetSideJobIncome(thief, job))}/s)";
                default: return "Disponível";
            }
        }

        public static string Clock(float seconds) => $"{(int)seconds / 60}:{(int)seconds % 60:00}";
    }
}
