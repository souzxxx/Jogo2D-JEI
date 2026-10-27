using System;
using AFirma.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace AFirma.UI
{
    /// <summary>
    /// Esconderijo (hub, cena Hideout): caixa com contador rolando, renda por segundo, abas da pasta
    /// (Mapa, Crew, Contatos, Bicos, Upgrades, Prestígio), o menu (Opções, Créditos) e os avisos do jogo. Cada aba monta o seu painel em #content.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HideoutScreen : MonoBehaviour
    {
        VisualElement root, content;
        Label money, income;
        double shownMoney;
        string open;

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            content = root.Q("content");
            money = root.Q<Label>("money");
            income = root.Q<Label>("income");
            shownMoney = Services.Economy.Money;

            Tab("map", MapPanel.Build);
            Tab("crew", CrewPanel.BuildCrew);
            Tab("contacts", CrewPanel.BuildContacts);
            Tab("jobs", EconomyPanel.BuildJobs);
            Tab("upgrades", EconomyPanel.BuildUpgrades);
            Tab("prestige", PrestigePanel.Build);
            root.Q<Button>("prestige").SetShown(Services.Progression.Fame > 0 || Services.Progression.CanPrestige(out _));
            root.OnClick("menu", () => Ui.Modal(root, "Menu", body => body.Add(Ui.El(null,
                Ui.Btn("Opções", () => MenuPanels.Options(root)),
                Ui.Btn("Créditos", () => MenuPanels.Credits(root)),
                Ui.Btn("Menu principal", SceneFlow.GoToMainMenu)))));

            Notices.Attach(root);
            Services.Audio.PlayMusic(MusicTrack.Hideout);
        }

        void Tab(string name, Action<VisualElement> build) => root.OnClick(name, () => Open(name, build));

        /// <summary>Abre o painel da aba (ou fecha, se ela já estava aberta).</summary>
        void Open(string name, Action<VisualElement> build)
        {
            root.Query<Button>(className: "tab").ForEach(t => t.RemoveFromClassList("tab--open"));
            content.Clear();
            open = open == name ? null : name;
            content.SetShown(open != null);
            if (open == null) return;
            root.Q<Button>(name).AddToClassList("tab--open");
            build(content);
        }

        void Update()
        {
            double target = Services.Economy.Money;
            shownMoney = Math.Abs(target - shownMoney) < 1 ? target : shownMoney + (target - shownMoney) * Math.Min(1, Time.unscaledDeltaTime * 6);
            money.text = Formulas.Money(shownMoney);
            income.text = $"+{Formulas.Money(Services.Economy.IncomePerSecond)}/s";
        }
    }
}
