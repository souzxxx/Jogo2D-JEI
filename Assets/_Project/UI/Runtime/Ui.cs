using System;
using UnityEngine.UIElements;

namespace AFirma.UI
{
    /// <summary>
    /// Atalhos pra montar e ligar as telas (UI Toolkit). Os estilos ficam em Styles/Firma.uss.
    /// Regra de UX (ESCOPO 7.2): botão indisponível continua clicável e diz o motivo; ação irreversível pede confirmação.
    /// </summary>
    public static class Ui
    {
        /// <summary>Toca o som de clique e chama a ação. Hover também tem som.</summary>
        public static Button OnClick(this Button button, Action action)
        {
            button.clicked += () =>
            {
                UiSounds.Click();
                action();
            };
            button.RegisterCallback<PointerEnterEvent>(_ => UiSounds.Hover());
            return button;
        }

        public static Button OnClick(this VisualElement root, string name, Action action) => root.Q<Button>(name).OnClick(action);

        /// <summary>Marca o botão como indisponível (cinza) sem desabilitar: o clique explica o motivo.</summary>
        public static void SetAvailable(this Button button, bool available) => button.EnableInClassList("btn--off", !available);

        public static void SetShown(this VisualElement element, bool shown) => element.EnableInClassList("hidden", !shown);

        public static VisualElement El(string classes = null, params VisualElement[] children)
        {
            var element = new VisualElement();
            AddClasses(element, classes);
            foreach (var child in children) element.Add(child);
            return element;
        }

        public static Label Text(string text, string classes = null)
        {
            var label = new Label(text);
            AddClasses(label, classes);
            return label;
        }

        public static Button Btn(string text, Action onClick, string classes = null)
        {
            var button = new Button { text = text };
            AddClasses(button, "btn " + classes);
            return button.OnClick(onClick);
        }

        /// <summary>
        /// Popup de confirmação por cima da tela (screen = rootVisualElement do UIDocument).
        /// Com twice = true pergunta de novo antes de fazer.
        /// </summary>
        public static void Confirm(VisualElement screen, string title, string body, string yes, Action onYes, bool twice = false)
        {
            VisualElement scrim = null;
            scrim = El("scrim",
                El("panel dialog",
                    Text(title, "h2"),
                    Text(body, "dialog__body"),
                    El("row dialog__buttons",
                        Btn("Cancelar", () => scrim.RemoveFromHierarchy()),
                        Btn(yes, () =>
                        {
                            scrim.RemoveFromHierarchy();
                            if (twice) Confirm(screen, "Certeza mesmo?", "Não tem volta.", yes, onYes);
                            else onYes();
                        }, "btn--red"))));
            screen.Add(scrim);
        }

        static void AddClasses(VisualElement element, string classes)
        {
            if (string.IsNullOrEmpty(classes)) return;
            foreach (var c in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries)) element.AddToClassList(c);
        }
    }
}
