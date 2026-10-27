using System;
using AFirma.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace AFirma.UI
{
    /// <summary>
    /// Esconderijo (hub, cena Hideout): caixa com contador rolando, renda por segundo, abas da pasta
    /// (Mapa, Crew, Contatos, Bicos, Upgrades, Prestígio) e os avisos do jogo. Cada aba monta o seu painel em #content.
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
            Tab("crew", c => Soon(c, "Crew", "A ficha de cada ladrão, demitir e pagar fiança."));
            Tab("contacts", c => Soon(c, "Contatos", "Os candidatos que a Dona Cida indica."));
            Tab("jobs", c => Soon(c, "Bicos", "Quem fica ganhando um troco enquanto não tem golpe."));
            Tab("upgrades", c => Soon(c, "Upgrades", "Van, quartos, ferramentas e o resto do esconderijo."));
            Tab("prestige", c => Soon(c, "Prestígio", "Sumir do mapa e voltar mais famoso."));
            root.Q<Button>("prestige").SetShown(Services.Progression.Fame > 0 || Services.Progression.CanPrestige(out _));
            root.OnClick("menu", SceneFlow.GoToMainMenu);

            GameEvents.Notice += OnNotice;
            Services.Audio.PlayMusic(MusicTrack.Hideout);
        }

        void OnDisable() => GameEvents.Notice -= OnNotice;

        void OnNotice(string text) => Ui.Toast(root, text);

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

        // os painéis da Sprint 2 (#52, #53, #55) entram no lugar deste aviso
        static void Soon(VisualElement content, string title, string what)
        {
            content.Add(Ui.Text(title, "h2"));
            content.Add(Ui.Text(what, "muted"));
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
