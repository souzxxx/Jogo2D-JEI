using System.Linq;
using AFirma.Core;
using UnityEngine.UIElements;

namespace AFirma.UI
{
    /// <summary>Aba Prestígio: "Sumir do mapa" (o que reseta, o que fica, quanta Fama vem) e a loja de bônus de Fama.</summary>
    static class PrestigePanel
    {
        static IProgressionService Progression => Services.Progression;

        public static void Build(VisualElement content)
        {
            var perks = Services.Db.famePerks.Where(p => p != null).ToList();
            content.Add(Ui.Live(() => $"{Progression.Fame}|{Progression.WantedLevel}|" + string.Join(",", perks.Select(Progression.GetFamePerkLevel)), (panel, updates) =>
            {
                panel.Add(Ui.Text($"Fama {Progression.Fame} · Procurado nível {Progression.WantedLevel}", "h2"));

                var vanish = Ui.Btn("", () =>
                {
                    if (!Progression.CanPrestige(out var reason)) { Ui.Toast(panel, reason); return; }
                    Ui.Confirm(panel, "Sumir do mapa?", $"Volta tudo do zero, menos a Fama e os bônus. Você ganha {Progression.FameOnPrestige()} de Fama.", "Sumir", () =>
                    {
                        Progression.Prestige();
                        SceneFlow.GoToHideout();
                    }, twice: true);
                }, "btn--red");
                vanish.name = "vanish";
                updates.Add(() =>
                {
                    vanish.text = $"Sumir do mapa (+{Progression.FameOnPrestige()} de Fama)";
                    vanish.SetAvailable(Progression.CanPrestige(out _));
                });
                panel.Add(Ui.El("paper",
                    Ui.Text("Reseta: Cr$, upgrades, golpes liberados e a crew (a Dona Cida manda 2 novatos)."),
                    Ui.Text("Fica: a Fama e os bônus de Fama."),
                    Ui.Text($"Procurado {Progression.WantedLevel} → {Progression.WantedLevel + 1}: golpes mais difíceis, loot maior.", "muted"),
                    vanish));

                panel.Add(Ui.Text("Bônus de Fama", "h2"));
                var list = new ScrollView();
                list.AddToClassList("grow");
                foreach (var perk in perks)
                {
                    int level = Progression.GetFamePerkLevel(perk);
                    var card = Ui.El("card row", Ui.Image(perk.icon, "icon32"), Ui.El("grow",
                        Ui.Text($"{perk.displayName} · nível {level}/{perk.maxLevel}"),
                        Ui.Text(perk.description, "muted")));
                    if (level < perk.maxLevel)
                    {
                        var buy = Ui.Btn($"Comprar {Progression.GetFamePerkCost(perk)} de Fama", () =>
                        {
                            if (!Progression.CanBuyFamePerk(perk, out var reason)) { Ui.Toast(panel, reason); UiSounds.Error(); return; }
                            Progression.BuyFamePerk(perk);
                            UiSounds.Buy();
                        }, "btn--green");
                        buy.name = "perk-" + perk.id;
                        updates.Add(() => buy.SetAvailable(Progression.CanBuyFamePerk(perk, out _)));
                        card.Add(Ui.El("crew__actions", buy));
                    }
                    list.Add(card);
                }
                panel.Add(list);
            }));
        }
    }
}
