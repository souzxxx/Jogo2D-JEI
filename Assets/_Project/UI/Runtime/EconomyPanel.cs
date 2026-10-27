using System;
using System.Collections.Generic;
using System.Linq;
using AFirma.Core;
using UnityEngine.UIElements;

namespace AFirma.UI
{
    /// <summary>Abas Bicos (quem fica ganhando um troco) e Upgrades (melhorias do esconderijo).</summary>
    static class EconomyPanel
    {
        static IEconomyService Economy => Services.Economy;

        public static void BuildJobs(VisualElement content)
        {
            Tutorial.Tip("bicos", "Bico é dinheiro entrando parado. Quem tá de bobeira no esconderijo pode ir pro bico.");
            content.Add(Ui.Live(() => string.Join(",", Services.Crew.Crew.Select(t => $"{t.id}{t.status}{t.sideJobId}")), (panel, updates) =>
            {
                var total = Ui.Text("", "h2");
                updates.Add(() => total.text = $"Bicos · +{Formulas.Money(Economy.IncomePerSecond)}/s");
                panel.Add(total);
                var list = new ScrollView();
                list.AddToClassList("grow");
                foreach (var job in Services.Db.sideJobs.Where(j => j != null)) list.Add(JobCard(job, panel));
                panel.Add(list);
            }));
        }

        static VisualElement JobCard(SideJobDef job, VisualElement panel)
        {
            var info = Ui.El("grow", Ui.Text(job.displayName), Ui.Text(job.description, "muted"));
            if (!job.requiresThief)
            {
                info.Add(Ui.Text($"Sempre rolando, sem ladrão: +{Formulas.Money(job.incomePerSecond)}/s", "money"));
                return Ui.El("card row", Ui.Image(job.icon, "icon32"), info);
            }

            var match = job.hasPreferredType ? Services.Db.GetThiefType(job.preferredType) : null;
            info.Add(Ui.Text($"+{Formulas.Money(job.incomePerSecond)}/s" + (match != null ? $" · rende {job.matchMultiplier:0.#}x com {match.displayName}" : "")));

            foreach (var worker in Services.Crew.Crew.Where(t => t.sideJobId == job.id))
            {
                var leave = Ui.Btn("Tirar", () => Economy.RemoveFromSideJob(worker));
                leave.name = "leave-" + worker.id;
                info.Add(Ui.El("row", Ui.Text($"{worker.DisplayName} +{Formulas.Money(Economy.GetSideJobIncome(worker, job))}/s", "money grow"), leave));
            }

            var send = Ui.El("row wrap");
            foreach (var thief in Services.Crew.Crew.Where(t => t.status == ThiefStatus.Available))
            {
                bool right = match != null && thief.type == job.preferredType;
                var button = Ui.Btn($"{thief.nickname} +{Formulas.Money(Economy.GetSideJobIncome(thief, job))}/s", () =>
                {
                    if (Economy.CanAssignSideJob(thief, job, out var reason)) Economy.AssignSideJob(thief, job);
                    else Ui.Toast(panel, reason);
                }, right ? "btn--green" : null);
                button.name = $"job-{job.id}-{thief.id}";
                button.SetAvailable(Economy.CanAssignSideJob(thief, job, out _));
                send.Add(button);
            }
            if (send.childCount > 0) info.Add(send);
            return Ui.El("card row", Ui.Image(job.icon, "icon32"), info);
        }

        public static void BuildUpgrades(VisualElement content)
        {
            var upgrades = Services.Db.upgrades.Where(u => u != null).ToList();
            content.Add(Ui.Live(() => string.Join(",", upgrades.Select(Economy.GetUpgradeLevel)), (panel, updates) =>
            {
                panel.Add(Ui.Text("Upgrades do esconderijo", "h2"));
                var list = new ScrollView();
                list.AddToClassList("grow");
                foreach (var upgrade in upgrades) list.Add(UpgradeCard(upgrade, panel, updates));
                panel.Add(list);
            }));
        }

        static VisualElement UpgradeCard(UpgradeDef upgrade, VisualElement panel, List<Action> updates)
        {
            int level = Economy.GetUpgradeLevel(upgrade);
            bool max = level >= upgrade.maxLevel;
            var info = Ui.El("grow",
                Ui.Text($"{upgrade.displayName} · nível {level}/{upgrade.maxLevel}"),
                Ui.Text(upgrade.description, "muted"),
                Ui.Text(max ? $"Agora {Effect(upgrade, level)} (máximo)" : $"Agora {Effect(upgrade, level)} → {Effect(upgrade, level + 1)}"));
            var card = Ui.El("card row", Ui.Image(upgrade.icon, "icon32"), info);
            if (max) return card;

            var buy = Ui.Btn($"Comprar {Formulas.Money(Economy.GetUpgradeCost(upgrade))}", () =>
            {
                if (!Economy.CanBuyUpgrade(upgrade, out var reason)) { Ui.Toast(panel, reason); UiSounds.Error(); return; }
                Economy.BuyUpgrade(upgrade);
                UiSounds.Buy();
                Ui.Toast(panel, $"{upgrade.displayName}: nível {level + 1}!");
            }, "btn--green");
            buy.name = "buy-" + upgrade.id;
            updates.Add(() => buy.SetAvailable(Economy.CanBuyUpgrade(upgrade, out _)));
            card.Add(Ui.El("crew__actions", buy));
            return card;
        }

        /// <summary>Efeito somado até o nível, com a unidade do upgrade ("+10%", "+2 s", "+1").</summary>
        static string Effect(UpgradeDef upgrade, int level)
        {
            float value = upgrade.valuePerLevel * level;
            switch (upgrade.effect)
            {
                case UpgradeEffect.LawyerPercent: return $"−{value:0}%";
                case UpgradeEffect.PoliceRadioSeconds: return $"+{value:0} s";
                case UpgradeEffect.HeistSlots:
                case UpgradeEffect.CrewSlots:
                case UpgradeEffect.ContactsLevel: return $"+{value:0}";
                default: return $"+{value:0}%";
            }
        }
    }
}
